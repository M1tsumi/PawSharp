using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PawSharp.Gateway.Connection
{
    public class WebSocketConnection
    {
        private ClientWebSocket _webSocket;
        private ZlibStreamCompression? _compression;
        private readonly bool _useCompression;
        private readonly bool _useArrayPooling;
        private bool _disposed;

        // zlib-stream transport compression uses a shared decompression context
        // across the connection for better compression ratios (up to 40% bandwidth savings).
        // This is different from permessage-deflate WebSocket extension.
        public WebSocketConnection(bool useCompression = false, bool useArrayPooling = true)
        {
            _webSocket = new ClientWebSocket();
            _useCompression = useCompression;
            _useArrayPooling = useArrayPooling;
            
            if (useCompression)
            {
                _compression = new ZlibStreamCompression();
            }
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            // ClientWebSocket cannot be reused after it has been closed. Dispose the
            // old instance and create a fresh one so that reconnection works correctly.
            if (_webSocket.State != WebSocketState.None)
            {
                _webSocket.Dispose();
                _webSocket = new ClientWebSocket();
            }
            
            // Initialize compression context for new connection
            if (_useCompression && _compression != null)
            {
                _compression.Initialize();
            }
            
            await _webSocket.ConnectAsync(uri, cancellationToken);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            // Only attempt a graceful close when the socket is in a closeable state.
            // It may already be Aborted or Closed if the remote end dropped the connection.
            if (_webSocket.State == WebSocketState.Open ||
                _webSocket.State == WebSocketState.CloseReceived ||
                _webSocket.State == WebSocketState.CloseSent)
            {
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WebSocket shutdown cancellation: {ex.Message}");
                }
                catch (WebSocketException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WebSocket may have been torn down remotely: {ex.Message}");
                }
            }
            
            // Reset compression context on disconnect
            if (_useCompression && _compression != null)
            {
                _compression.Reset();
            }
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task<string> ReceiveAsync(CancellationToken cancellationToken)
        {
            // 64 KB buffer reduces loop iterations for large events (e.g. GUILD_CREATE
            // for servers with thousands of members).
            // Use ArrayPool to reduce GC pressure for large allocations.
            byte[] buffer = _useArrayPooling 
                ? ArrayPool<byte>.Shared.Rent(65536)
                : new byte[65536];
            
            try
            {
                var messageBuilder = new StringBuilder();
                WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
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
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _webSocket.Dispose();
            _compression?.Dispose();
        }
    }
}