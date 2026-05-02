#nullable enable
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Gateway.Events
{
    /// <summary>
    /// Represents a queued event for dispatching.
    /// </summary>
    internal record EventDispatchItem
    {
        public string EventName { get; init; } = string.Empty;
        public object? EventData { get; init; }
        public string? RawJson { get; init; }
        public Type? EventType { get; init; }
    }

    /// <summary>
    /// Bounded queue for event dispatching with automatic backpressure.
    /// Uses System.Threading.Channels for thread-safe async producer/consumer pattern.
    /// </summary>
    internal class EventDispatchQueue : IDisposable
    {
        private readonly Channel<EventDispatchItem> _channel;
        private readonly Task _processingTask;
        private readonly EventDispatcher _dispatcher;
        private readonly bool _enableParallelDispatch;
        private readonly int _maxDegreeOfParallelism;
        private readonly bool _disposed;
        private readonly Microsoft.Extensions.Logging.ILogger? _logger;

        public EventDispatchQueue(
            EventDispatcher dispatcher,
            int maxQueueSize,
            bool enableParallelDispatch = false,
            int maxDegreeOfParallelism = 4,
            Microsoft.Extensions.Logging.ILogger? logger = null)
        {
            _dispatcher = dispatcher;
            _enableParallelDispatch = enableParallelDispatch;
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _logger = logger;

            // Create channel for automatic backpressure
            if (maxQueueSize > 0)
            {
                var boundedOptions = new BoundedChannelOptions(maxQueueSize)
                {
                    FullMode = BoundedChannelFullMode.Wait, // Block producer when full
                    SingleReader = !enableParallelDispatch, // Single reader for sequential processing
                    SingleWriter = true
                };
                _channel = Channel.CreateBounded<EventDispatchItem>(boundedOptions);
            }
            else
            {
                var unboundedOptions = new UnboundedChannelOptions()
                {
                    SingleReader = !enableParallelDispatch,
                    SingleWriter = true
                };
                _channel = Channel.CreateUnbounded<EventDispatchItem>(unboundedOptions);
            }

            // Start background processing task
            _processingTask = ProcessQueueAsync();
        }

        /// <summary>
        /// Enqueues an event for dispatching. Will block if the queue is full (backpressure).
        /// </summary>
        public async ValueTask EnqueueAsync(EventDispatchItem item)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EventDispatchQueue));

            await _channel.Writer.WriteAsync(item);
        }

        /// <summary>
        /// Gets the current queue depth for monitoring.
        /// </summary>
        public int QueueDepth => _channel.Reader.Count;

        /// <summary>
        /// Background task that processes queued events.
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            if (_enableParallelDispatch)
            {
                await ProcessQueueParallelAsync();
            }
            else
            {
                await ProcessQueueSequentialAsync();
            }
        }

        /// <summary>
        /// Processes events sequentially (default behavior).
        /// </summary>
        private async Task ProcessQueueSequentialAsync()
        {
            await foreach (var item in _channel.Reader.ReadAllAsync())
            {
                try
                {
                    await DispatchItemAsync(item);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing queued event {Event}", item.EventName);
                }
            }
        }

        /// <summary>
        /// Processes events in parallel using Task.WhenAll with degree of parallelism control.
        /// </summary>
        private async Task ProcessQueueParallelAsync()
        {
            var semaphore = new System.Threading.SemaphoreSlim(_maxDegreeOfParallelism);
            var tasks = new System.Collections.Concurrent.ConcurrentBag<Task>();

            await foreach (var item in _channel.Reader.ReadAllAsync())
            {
                await semaphore.WaitAsync();
                
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await DispatchItemAsync(item);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing queued event {Event}", item.EventName);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
            }

            // Wait for all remaining tasks to complete
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Dispatches a single event item.
        /// </summary>
        private async Task DispatchItemAsync(EventDispatchItem item)
        {
            // For AOT compatibility, we avoid reflection by using the typed EventData directly.
            // EventData is already deserialized to the correct type when enqueued.
            if (item.EventData is GatewayEvent gatewayEvent)
            {
                // Use the non-generic typed dispatch method for AOT compatibility
                await _dispatcher.DispatchTypedAsync(item.EventName, gatewayEvent, item.RawJson);
            }
            else if (item.RawJson != null)
            {
                // Fallback to raw dispatch when typed data is not available
                await _dispatcher.DispatchRawAsync(item.EventName, item.RawJson);
            }
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
    }
}
