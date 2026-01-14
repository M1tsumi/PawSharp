using System;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PawSharp.Gateway.Connection
{
    public class WebSocketConnection
    {
        private readonly ClientWebSocket _webSocket;
        private bool _useCompression;

        public WebSocketConnection(bool useCompression = false)
        {
            _webSocket = new ClientWebSocket();
            _useCompression = useCompression;
            
            if (_useCompression)
            {
                // Request permessage-deflate extension
                _webSocket.Options.AddSubProtocol("permessage-deflate");
            }
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await _webSocket.ConnectAsync(uri, cancellationToken);
            // Check if compression was negotiated
            _useCompression = _webSocket.SubProtocol == "permessage-deflate";
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task<string> ReceiveAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            var messageBuilder = new StringBuilder();
            WebSocketReceiveResult result;

            do
            {
                result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    if (_useCompression && result.EndOfMessage)
                    {
                        // Decompress if compression is enabled
                        using var compressedStream = new MemoryStream(buffer, 0, result.Count);
                        using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                        using var decompressedStream = new MemoryStream();
                        await deflateStream.CopyToAsync(decompressedStream);
                        var decompressedData = decompressedStream.ToArray();
                        messageBuilder.Append(Encoding.UTF8.GetString(decompressedData));
                    }
                    else
                    {
                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Handle close
                    break;
                }
            } while (!result.EndOfMessage);

            return messageBuilder.ToString();
        }

        public bool IsConnected => _webSocket.State == WebSocketState.Open;
        public bool CompressionEnabled => _useCompression;
    }
}