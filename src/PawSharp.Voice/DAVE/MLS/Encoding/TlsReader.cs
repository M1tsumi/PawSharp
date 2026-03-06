// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace PawSharp.Voice.DAVE.MLS.Encoding;

/// <summary>
/// Reader for TLS presentation language wire format (RFC 8446 §3).
///
/// MLS messages are encoded using TLS vectors with big-endian length prefixes.
/// This reader wraps a <see cref="ReadOnlySpan{T}"/> and advances a position
/// cursor, throwing <see cref="MlsDecodeException"/> on any overrun or malformed input.
///
/// All integers are big-endian (network byte order) per TLS convention.
/// </summary>
internal ref struct TlsReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _pos;

    /// <summary>Initialise a reader over the given byte span.</summary>
    public TlsReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _pos  = 0;
    }

    /// <summary>Number of bytes remaining in the buffer.</summary>
    public int Remaining => _data.Length - _pos;

    /// <summary>True when the entire buffer has been consumed.</summary>
    public bool IsEmpty => _pos >= _data.Length;

    /// <summary>Current read position (0-based).</summary>
    public int Position => _pos;

    // ── Primitive reads ───────────────────────────────────────────────────────

    /// <summary>Reads a single unsigned byte.</summary>
    public byte ReadUint8()
    {
        EnsureAvailable(1);
        return _data[_pos++];
    }

    /// <summary>Reads a big-endian unsigned 16-bit integer.</summary>
    public ushort ReadUint16()
    {
        EnsureAvailable(2);
        ushort v = (ushort)((_data[_pos] << 8) | _data[_pos + 1]);
        _pos += 2;
        return v;
    }

    /// <summary>Reads a big-endian unsigned 32-bit integer.</summary>
    public uint ReadUint32()
    {
        EnsureAvailable(4);
        uint v = ((uint)_data[_pos]     << 24) |
                 ((uint)_data[_pos + 1] << 16) |
                 ((uint)_data[_pos + 2] << 8)  |
                  (uint)_data[_pos + 3];
        _pos += 4;
        return v;
    }

    /// <summary>Reads a big-endian unsigned 64-bit integer.</summary>
    public ulong ReadUint64()
    {
        EnsureAvailable(8);
        ulong v = ((ulong)_data[_pos]     << 56) |
                  ((ulong)_data[_pos + 1] << 48) |
                  ((ulong)_data[_pos + 2] << 40) |
                  ((ulong)_data[_pos + 3] << 32) |
                  ((ulong)_data[_pos + 4] << 24) |
                  ((ulong)_data[_pos + 5] << 16) |
                  ((ulong)_data[_pos + 6] << 8)  |
                   (ulong)_data[_pos + 7];
        _pos += 8;
        return v;
    }

    // ── Opaque / vector reads ─────────────────────────────────────────────────

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes and returns them as a new array.
    /// </summary>
    public byte[] ReadBytes(int count)
    {
        EnsureAvailable(count);
        var r = _data.Slice(_pos, count).ToArray();
        _pos += count;
        return r;
    }

    /// <summary>
    /// Reads a variable-length opaque vector prefixed with a 1-byte length
    /// (TLS <c>opaque&lt;0..255&gt;</c>).
    /// </summary>
    public byte[] ReadVector8()
    {
        int len = ReadUint8();
        return ReadBytes(len);
    }

    /// <summary>
    /// Reads a variable-length opaque vector prefixed with a 2-byte big-endian
    /// length (TLS <c>opaque&lt;0..65535&gt;</c>).
    /// </summary>
    public byte[] ReadVector16()
    {
        int len = ReadUint16();
        return ReadBytes(len);
    }

    /// <summary>
    /// Reads a variable-length opaque vector prefixed with a 4-byte big-endian
    /// length (TLS <c>opaque&lt;0..2^32-1&gt;</c>).
    /// </summary>
    public byte[] ReadVector32()
    {
        int len = (int)ReadUint32();
        return ReadBytes(len);
    }

    /// <summary>
    /// Returns a sub-reader scoped to the next <paramref name="count"/> bytes.
    /// Advances the current position by <paramref name="count"/>.
    /// </summary>
    public TlsReader Slice(int count)
    {
        EnsureAvailable(count);
        var sub = new TlsReader(_data.Slice(_pos, count));
        _pos += count;
        return sub;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureAvailable(int needed)
    {
        if (_pos + needed > _data.Length)
            throw new MlsDecodeException(
                $"TLS read overrun: needed {needed} bytes at offset {_pos}, only {Remaining} remaining.");
    }
}

/// <summary>
/// Thrown when a TLS-encoded MLS message cannot be decoded due to structural errors.
/// </summary>
public sealed class MlsDecodeException : Exception
{
    public MlsDecodeException(string message) : base(message) { }
    public MlsDecodeException(string message, Exception inner) : base(message, inner) { }
}
