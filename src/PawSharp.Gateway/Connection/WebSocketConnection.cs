using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Gateway.Connection
{
    /// <summary>
    /// Represents the result of a WebSocket receive operation, including close code information.
    /// </summary>
    public class WebSocketReceiveResult
    {
        /// <summary>The received message text, or null if the connection closed.</summary>
        public string? Message { get; set; }
        
        /// <summary>True if the WebSocket closed normally or with a close code.</summary>
        public bool IsClosed { get; set; }
        
        /// <summary>The WebSocket close status code, if applicable.</summary>
        public WebSocketCloseStatus? CloseStatus { get; set; }
        
        /// <summary>The close status description, if applicable.</summary>
        public string? CloseStatusDescription { get; set; }
        
        /// <summary>
        /// Gets the Discord gateway close code if applicable.
        /// See https://docs.discord.com/developers/topics/opcodes-and-status-codes
        /// </summary>
        public int? DiscordCloseCode => CloseStatus.HasValue ? (int)CloseStatus.Value : null;
    }

    public class WebSocketConnection : IDisposable, IAsyncDisposable
    {
        private ClientWebSocket _webSocket;
        private ZlibStreamCompression? _compression;
        private readonly bool _useCompression;
        private readonly bool _useArrayPooling;
        private readonly int _bufferSize;
        private bool _disposed;
        private Task? _disposeTask;
        private WebSocketCloseStatus? _closeStatus;
        private string? _closeStatusDescription;
        private readonly ILogger<WebSocketConnection>? _logger;

        // zlib-stream transport compression uses a shared decompression context
        // across the connection for better compression ratios (up to 40% bandwidth savings).
        // This is different from permessage-deflate WebSocket extension.
        /// <summary>
        /// Creates a new WebSocket connection.
        /// </summary>
        /// <param name="useCompression">Enable zlib-stream compression</param>
        /// <param name="useArrayPooling">Use ArrayPool for buffer management</param>
        /// <param name="bufferSizeKb">Receive buffer size in KB (default: 64)</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        public WebSocketConnection(bool useCompression = false, bool useArrayPooling = true, int bufferSizeKb = 64, ILogger<WebSocketConnection>? logger = null)
        {
            _webSocket = new ClientWebSocket();
            _useCompression = useCompression;
            _useArrayPooling = useArrayPooling;
            _logger = logger;
            // Clamp buffer size between 4KB and 1024KB (1MB)
            _bufferSize = Math.Clamp(bufferSizeKb, 4, 1024) * 1024;

            if (useCompression)
            {
                _compression = new ZlibStreamCompression();
            }
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            // Reset close status tracking for new connection
            _closeStatus = null;
            _closeStatusDescription = null;
            
            // ClientWebSocket cannot be reused after it has been closed. Dispose the
            // old instance and create a fresh one so that reconnection works correctly.
            if (_webSocket.State != WebSocketState.None)
            {
                try
                {
                    _webSocket.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, safe to ignore
                }
                _webSocket = new ClientWebSocket();
            }
            
            // Initialize compression context for new connection
            if (_useCompression && _compression != null)
            {
                _compression.Initialize();
            }
            
            await _webSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Only attempt a graceful close when the socket is in a closeable state.
                // It may already be Aborted or Closed if the remote end dropped the connection.
                if (_webSocket.State == WebSocketState.Open ||
                    _webSocket.State == WebSocketState.CloseReceived ||
                    _webSocket.State == WebSocketState.CloseSent)
                {
                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout for close handshake
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex) when (ex.CancellationToken != cancellationToken)
                    {
                        // Close handshake timed out, force abort
                        _logger?.LogWarning(ex, "WebSocket close handshake timed out");
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger?.LogDebug(ex, "WebSocket shutdown cancelled");
                    }
                    catch (WebSocketException ex)
                    {
                        _logger?.LogWarning(ex, "WebSocket may have been torn down remotely");
                    }
                }
            }
            finally
            {
                // Always reset compression context on disconnect
                if (_useCompression && _compression != null)
                {
                    try
                    {
                        _compression.Reset();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error resetting compression context");
                    }
                }
            }
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> ReceiveAsync(CancellationToken cancellationToken)
        {
            // Configurable buffer size reduces loop iterations for large events (e.g. GUILD_CREATE
            // for servers with thousands of members). Default is 64KB, configurable up to 1MB.
            // Use ArrayPool to reduce GC pressure for large allocations.
            byte[] buffer = _useArrayPooling 
                ? ArrayPool<byte>.Shared.Rent(_bufferSize)
                : new byte[_bufferSize];
            
            try
            {
                var messageBuilder = new StringBuilder();
                System.Net.WebSockets.WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        if (_useCompression && _compression != null)
                        {
                            // Handle compressed data
                            var decompressed = _compression.DecompressChunk(buffer.AsSpan(0, result.Count).ToArray());
                            if (decompressed != null)
                            {
                                messageBuilder.Append(decompressed);
                            }
                        }
                        else
                        {
                            // Handle uncompressed data
                            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Capture close status for error handling
                        _closeStatus = result.CloseStatus;
                        _closeStatusDescription = result.CloseStatusDescription;
                        break;
                    }
                } while (!result.EndOfMessage);

                return messageBuilder.ToString();
            }
            finally
            {
                if (_useArrayPooling)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        public bool IsConnected => _webSocket.State == WebSocketState.Open;
        public bool CompressionEnabled => _useCompression;
        
        /// <summary>
        /// The close status code from the last WebSocket close, if any.
        /// </summary>
        public WebSocketCloseStatus? CloseStatus => _closeStatus;
        
        /// <summary>
        /// The close status description from the last WebSocket close, if any.
        /// </summary>
        public string? CloseStatusDescription => _closeStatusDescription;
        
        /// <summary>
        /// True if the connection was closed with a Discord gateway error code.
        /// See https://docs.discord.com/developers/topics/opcodes-and-status-codes#gateway-close-event-codes
        /// </summary>
        public bool IsDiscordErrorClose => _closeStatus.HasValue && (int)_closeStatus.Value >= 4000;
        
        /// <summary>
        /// Disposes the WebSocket connection in a fire-and-forget manner.
        /// Dispose must remain synchronous per IDisposable contract, so callers that need
        /// a clean shutdown should await <see cref="WaitForDisposeAsync"/> after disposal.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Fire-and-forget graceful close to avoid blocking the calling thread.
            // Dispose() must remain synchronous per IDisposable contract.
            _disposeTask = Task.Run(async () =>
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Open ||
                        _webSocket.State == WebSocketState.CloseReceived ||
                        _webSocket.State == WebSocketState.CloseSent)
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", cts.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Graceful WebSocket close during dispose failed");
                }
                finally
                {
                    try { _webSocket.Dispose(); }
                    catch (Exception ex) { _logger?.LogDebug(ex, "WebSocket disposal error"); }
                    try { _compression?.Dispose(); }
                    catch (Exception ex) { _logger?.LogDebug(ex, "WebSocket compression disposal error"); }
                }
            });
        }

        /// <summary>
        /// Waits for the asynchronous dispose operation to complete.
        /// Call this after <see cref="Dispose"/> during a graceful shutdown.
        /// </summary>
        /// <param name="timeout">Optional timeout. Defaults to 5 seconds.</param>
        public async Task WaitForDisposeAsync(TimeSpan? timeout = null)
        {
            if (_disposeTask is not null)
            {
                timeout ??= TimeSpan.FromSeconds(5);
                try
                {
                    await _disposeTask.WaitAsync(timeout.Value).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    _logger?.LogDebug("WebSocket dispose did not complete within {Timeout}", timeout.Value);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            Dispose();
            await WaitForDisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}