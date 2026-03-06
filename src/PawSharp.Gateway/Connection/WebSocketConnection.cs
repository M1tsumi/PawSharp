using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PawSharp.Gateway.Connection
{
    public class WebSocketConnection
    {
        private readonly ClientWebSocket _webSocket;

        // Compression is disabled: permessage-deflate is a WebSocket *extension*, not a
        // subprotocol, and ClientWebSocket does not support extensions. Discord uses
        // zlib-stream transport compression which requires separate framing logic.
        // Proper zlib-stream support is tracked for 0.7.0.
        public WebSocketConnection(bool useCompression = false)
        {
            _webSocket = new ClientWebSocket();
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
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
                catch (OperationCanceledException) { /* shutdown cancellation is expected */ }
                catch (WebSocketException) { /* socket may have been torn down remotely */ }
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
            var buffer = new byte[65536];
            var messageBuilder = new StringBuilder();
            WebSocketReceiveResult result;

            do
            {
                result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            } while (!result.EndOfMessage);

            return messageBuilder.ToString();
        }

        public bool IsConnected => _webSocket.State == WebSocketState.Open;
        public bool CompressionEnabled => false;
    }
}