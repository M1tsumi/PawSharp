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
    internal class EventDispatchQueue : IDisposable, IAsyncDisposable
    {
        private readonly Channel<EventDispatchItem> _channel;
        private readonly Task _processingTask;
        private readonly EventDispatcher _dispatcher;
        private readonly bool _enableParallelDispatch;
        private readonly int _maxDegreeOfParallelism;
        private bool _disposed;
        private readonly Microsoft.Extensions.Logging.ILogger? _logger;
        private Task? _disposeTask;

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

            await _channel.Writer.WriteAsync(item).ConfigureAwait(false);
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
                await ProcessQueueParallelAsync().ConfigureAwait(false);
            }
            else
            {
                await ProcessQueueSequentialAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Processes events sequentially (default behavior).
        /// </summary>
        private async Task ProcessQueueSequentialAsync()
        {
            await foreach (var item in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                try
                {
                    await DispatchItemAsync(item).ConfigureAwait(false);
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

            await foreach (var item in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await DispatchItemAsync(item).ConfigureAwait(false);
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
            await Task.WhenAll(tasks).ConfigureAwait(false);
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
                await _dispatcher.DispatchTypedAsync(item.EventName, gatewayEvent, item.RawJson).ConfigureAwait(false);
            }
            else if (item.RawJson != null)
            {
                // Fallback to raw dispatch when typed data is not available
                await _dispatcher.DispatchRawAsync(item.EventName, item.RawJson).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disposes the queue and begins draining pending events in a fire-and-forget manner.
        /// Dispose must remain synchronous per IDisposable contract, so callers that need
        /// a clean shutdown should await <see cref="WaitForDrainAsync"/> after disposal.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _channel.Writer.Complete();
            _disposed = true;
            // Fire-and-forget with timeout to avoid blocking the caller thread.
            _disposeTask = Task.Run(async () =>
            {
                try
                {
                    await _processingTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    _logger?.LogWarning("Event dispatch queue did not drain within 5 seconds during dispose");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger?.LogError(ex, "Error waiting for event dispatch queue to complete");
                }
            });
        }

        /// <summary>
        /// Waits for the disposal / drain operation to complete.
        /// Call this after <see cref="Dispose"/> during a graceful shutdown
        /// to ensure all queued events have been processed.
        /// </summary>
        /// <param name="timeout">Optional timeout for the drain wait. Defaults to 10 seconds.</param>
        public async Task WaitForDrainAsync(TimeSpan? timeout = null)
        {
            if (_disposeTask is not null)
            {
                timeout ??= TimeSpan.FromSeconds(10);
                try
                {
                    await _disposeTask.WaitAsync(timeout.Value).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    _logger?.LogWarning("Event dispatch queue drain did not complete within {Timeout}", timeout.Value);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            Dispose();
            if (_disposeTask is not null)
            {
                try
                {
                    await _disposeTask.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    _logger?.LogWarning("Event dispatch queue async drain did not complete within 10 seconds");
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
