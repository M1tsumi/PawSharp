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
            // For AOT compatibility, we need to avoid reflection.
            // Since we can't use MakeGenericMethod, we'll use a different approach:
            // Call DispatchRawAsync for all events when using the queue.
            // This is a limitation of the current design - to support AOT with queuing,
            // we would need to register all event types at compile time.
            // For now, we'll use the raw JSON dispatch which works without reflection.
            if (item.RawJson != null)
            {
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
