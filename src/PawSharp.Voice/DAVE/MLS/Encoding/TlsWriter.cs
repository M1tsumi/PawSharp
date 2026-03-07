// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.IO;

namespace PawSharp.Voice.DAVE.MLS.Encoding;

/// <summary>
/// Writer for TLS presentation language wire format (RFC 8446 §3).
///
/// Wraps a <see cref="MemoryStream"/> and provides helpers for writing
/// the big-endian integers and length-prefixed vectors that make up all
/// MLS message structures.
/// </summary>
internal sealed class TlsWriter : IDisposable
{
    private readonly MemoryStream _buf;

    /// <summary>Creates a new writer backed by a resizable <see cref="MemoryStream"/>.</summary>
    public TlsWriter() => _buf = new MemoryStream();

    /// <summary>Creates a new writer with an initial buffer capacity hint.</summary>
    public TlsWriter(int initialCapacity) => _buf = new MemoryStream(initialCapacity);

    // ── Primitive writes ──────────────────────────────────────────────────────

    /// <summary>Writes a single unsigned byte.</summary>
    public TlsWriter WriteUint8(byte v)
    {
        _buf.WriteByte(v);
        return this;
    }

    /// <summary>Writes a big-endian unsigned 16-bit integer.</summary>
    public TlsWriter WriteUint16(ushort v)
    {
        _buf.WriteByte((byte)(v >> 8));
        _buf.WriteByte((byte)v);
        return this;
    }

    /// <summary>Writes a big-endian unsigned 32-bit integer.</summary>
    public TlsWriter WriteUint32(uint v)
    {
        _buf.WriteByte((byte)(v >> 24));
        _buf.WriteByte((byte)(v >> 16));
        _buf.WriteByte((byte)(v >> 8));
        _buf.WriteByte((byte)v);
        return this;
    }

    /// <summary>Writes a big-endian unsigned 64-bit integer.</summary>
    public TlsWriter WriteUint64(ulong v)
    {
        _buf.WriteByte((byte)(v >> 56));
        _buf.WriteByte((byte)(v >> 48));
        _buf.WriteByte((byte)(v >> 40));
        _buf.WriteByte((byte)(v >> 32));
        _buf.WriteByte((byte)(v >> 24));
        _buf.WriteByte((byte)(v >> 16));
        _buf.WriteByte((byte)(v >> 8));
        _buf.WriteByte((byte)v);
        return this;
    }

    // ── Raw byte writes ───────────────────────────────────────────────────────

    /// <summary>Writes the raw bytes from a span (no length prefix).</summary>
    public TlsWriter WriteBytes(ReadOnlySpan<byte> data)
    {
        _buf.Write(data);
        return this;
    }

    // ── Variable-length vector writes ─────────────────────────────────────────

    /// <summary>
    /// Writes a variable-length opaque vector with a 1-byte length prefix
    /// (TLS <c>opaque&lt;0..255&gt;</c>).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when data exceeds 255 bytes.</exception>
    public TlsWriter WriteVector8(ReadOnlySpan<byte> data)
    {
        if (data.Length > byte.MaxValue)
            throw new ArgumentException($"Vector8 payload exceeds 255 bytes ({data.Length}).");
        WriteUint8((byte)data.Length);
        WriteBytes(data);
        return this;
    }

    /// <summary>
    /// Writes a variable-length opaque vector with a 2-byte big-endian length prefix
    /// (TLS <c>opaque&lt;0..65535&gt;</c>).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when data exceeds 65535 bytes.</exception>
    public TlsWriter WriteVector16(ReadOnlySpan<byte> data)
    {
        if (data.Length > ushort.MaxValue)
            throw new ArgumentException($"Vector16 payload exceeds 65535 bytes ({data.Length}).");
        WriteUint16((ushort)data.Length);
        WriteBytes(data);
        return this;
    }

    /// <summary>
    /// Writes a variable-length opaque vector with a 4-byte big-endian length prefix
    /// (TLS <c>opaque&lt;0..2^32-1&gt;</c>).
    /// </summary>
    public TlsWriter WriteVector32(ReadOnlySpan<byte> data)
    {
        WriteUint32((uint)data.Length);
        WriteBytes(data);
        return this;
    }

    // ── Nested writer support ─────────────────────────────────────────────────

    /// <summary>
    /// Writes the encoded output of an inner <see cref="TlsWriter"/> as a Vector16
    /// (2-byte length prefix + inner bytes).
    /// </summary>
    public TlsWriter WriteNested16(TlsWriter inner)
    {
        var bytes = inner.ToArray();
        WriteVector16(bytes);
        return this;
    }

    /// <summary>
    /// Writes the encoded output of an inner <see cref="TlsWriter"/> as a Vector32
    /// (4-byte length prefix + inner bytes).
    /// </summary>
    public TlsWriter WriteNested32(TlsWriter inner)
    {
        var bytes = inner.ToArray();
        WriteVector32(bytes);
        return this;
    }

    // ── Output ────────────────────────────────────────────────────────────────

    /// <summary>Returns the accumulated bytes as a new array.</summary>
    public byte[] ToArray() => _buf.ToArray();

    /// <summary>Returns the current length of the buffer in bytes.</summary>
    public int Length => (int)_buf.Length;

    public void Dispose() => _buf.Dispose();
}
