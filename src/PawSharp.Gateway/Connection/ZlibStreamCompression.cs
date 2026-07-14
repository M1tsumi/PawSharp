#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace PawSharp.Gateway.Connection
{
    /// <summary>
    /// Handles zlib-stream transport compression for Discord Gateway.
    /// Uses a shared decompression context across the connection for better compression ratios.
    /// </summary>
    /// <remarks>
    /// Discord Gateway uses zlib-stream compression with Z_SYNC_FLUSH.
    /// Each compressed message ends with the suffix 0x00 0x00 0xFF 0xFF.
    /// The decompression context is shared across the connection to leverage
    /// historical data for better compression ratios (up to 40% bandwidth savings).
    /// </remarks>
    public class ZlibStreamCompression : IDisposable
    {
        private readonly MemoryStream _buffer = new();
        private readonly byte[] _zlibSuffix = { 0x00, 0x00, 0xFF, 0xFF };
        private DeflateStream? _compressor;
        private DeflateStream? _decompressor;
        private bool _disposed;

        /// <summary>
        /// Gets whether compression is currently enabled.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Initializes the compression context for a new connection.
        /// </summary>
        public void Initialize()
        {
            if (IsEnabled) return;

            _buffer.SetLength(0);
            _buffer.Position = 0;

            _decompressor = new DeflateStream(_buffer, CompressionMode.Decompress, leaveOpen: true);
            
            var compressBuffer = new MemoryStream();
            _compressor = new DeflateStream(compressBuffer, CompressionMode.Compress, leaveOpen: true);
            
            IsEnabled = true;
        }

        /// <summary>
        /// Maximum buffer size (4 MB) to prevent memory exhaustion from malformed streams.
        /// </summary>
        private const int MaxBufferSize = 4 * 1024 * 1024;

        /// <summary>
        /// Decompresses a chunk of data received from the WebSocket.
        /// Returns null if the chunk doesn't contain a complete message.
        /// </summary>
        /// <param name="chunk">The compressed chunk received from the WebSocket.</param>
        /// <returns>The decompressed message, or null if more data is needed.</returns>
        public string? DecompressChunk(byte[] chunk)
        {
            if (!IsEnabled || _decompressor == null)
                throw new InvalidOperationException("Compression not initialized");

            // Check if chunk ends with Z_SYNC_FLUSH suffix
            if (chunk.Length < 4 || 
                chunk[^4] != 0x00 || chunk[^3] != 0x00 || 
                chunk[^2] != 0xFF || chunk[^1] != 0xFF)
            {
                // Not a complete message, buffer it
                if (_buffer.Length + chunk.Length > MaxBufferSize)
                {
                    System.Diagnostics.Debug.WriteLine($"Zlib decompression buffer exceeded {MaxBufferSize} bytes — discarding buffer to prevent memory exhaustion");
                    _buffer.SetLength(0);
                    _buffer.Position = 0;
                }
                _buffer.Write(chunk, 0, chunk.Length);
                return null;
            }

            // Write chunk to buffer
            _buffer.Write(chunk, 0, chunk.Length);

            try
            {
                // Decompress the entire buffer
                _buffer.Position = 0;
                using var decompressed = new MemoryStream();
                _decompressor.CopyTo(decompressed);

                var decompressedBytes = decompressed.ToArray();
                return System.Text.Encoding.UTF8.GetString(decompressedBytes);
            }
            finally
            {
                _buffer.SetLength(0);
                _buffer.Position = 0;
            }
        }

        /// <summary>
        /// Compresses a message for sending to the Gateway.
        /// Note: Discord does not accept compressed messages from clients,
        /// so this method is not currently used but provided for completeness.
        /// </summary>
        /// <param name="message">The message to compress.</param>
        /// <returns>The compressed message bytes.</returns>
        public byte[] CompressMessage(string message)
        {
            if (!IsEnabled || _compressor == null)
                throw new InvalidOperationException("Compression not initialized");

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            _compressor.Write(messageBytes, 0, messageBytes.Length);
            _compressor.Flush();
            
            var compressBuffer = _compressor.BaseStream as MemoryStream;
            if (compressBuffer == null)
                throw new InvalidOperationException("Compressor stream is not a MemoryStream");

            var compressed = compressBuffer.ToArray();
            compressBuffer.SetLength(0);
            
            return compressed;
        }

        /// <summary>
        /// Resets the compression context (called on reconnection).
        /// </summary>
        public void Reset()
        {
            _buffer.SetLength(0);
            _compressor?.Dispose();
            _decompressor?.Dispose();
            _compressor = null;
            _decompressor = null;
            IsEnabled = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _compressor?.Dispose();
            _decompressor?.Dispose();
            _buffer.Dispose();
        }
    }
}
