#nullable enable
using System;
using PawSharp.Voice.DAVE.MLS.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Messages;

/// <summary>
/// RFC 9420 §5.3.1 — MLS credential.
///
/// A credential binds an identity to a leaf node's signature key.
/// DAVE uses BasicCredential (type=1) with a raw identity byte string
/// (typically the Discord user ID encoded as UTF-8).
/// </summary>
internal sealed class Credential
{
    public CredentialType Type { get; }

    /// <summary>Raw identity bytes (for BasicCredential: UTF-8 Discord user ID).</summary>
    public byte[] Identity { get; }

    /// <summary>X.509 DER certificate chain (for X509Credential, unused by DAVE).</summary>
    public byte[][]? Certificates { get; }

    private Credential(CredentialType type, byte[] identity, byte[][]? certs)
    {
        Type         = type;
        Identity     = identity;
        Certificates = certs;
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    /// <summary>Creates a BasicCredential with an identity byte string.</summary>
    public static Credential Basic(byte[] identity)
        => new Credential(CredentialType.Basic, identity, null);

    // ── Serialisation ─────────────────────────────────────────────────────────

    /// <summary>Encodes the credential as a TLS struct.</summary>
    public byte[] Encode()
    {
        using var w = new TlsWriter(Identity.Length + 4);
        w.WriteUint16((ushort)Type);

        if (Type == CredentialType.Basic)
            w.WriteVector16(Identity);

        return w.ToArray();
    }

    /// <summary>Decodes a credential from TLS bytes.</summary>
    public static Credential Decode(ReadOnlySpan<byte> data)
    {
        var r    = new TlsReader(data);
        var type = (CredentialType)r.ReadUint16();

        if (type == CredentialType.Basic)
        {
            var identity = r.ReadVector16();
            return new Credential(type, identity, null);
        }

        throw new MlsDecodeException($"Unsupported credential type: {type}");
    }
}
