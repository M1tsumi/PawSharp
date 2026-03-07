// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable

namespace PawSharp.Voice.DAVE.MLS.Messages;

// ── RFC 9420 §5 — MLS content and proposal types ─────────────────────────────

/// <summary>MLS ciphersuite identifiers (RFC 9420 §5.1).</summary>
internal enum CipherSuite : ushort
{
    /// <summary>DAVE profile: DHKEM(X25519,HKDF-SHA256) + AES-128-GCM + SHA-256 + Ed25519.</summary>
    MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519 = 0x0001,
}

/// <summary>MLS protocol version (RFC 9420 §5.1).</summary>
internal enum ProtocolVersion : ushort
{
    Mls10 = 1,
}

/// <summary>MLS message content types (RFC 9420 §6).</summary>
internal enum ContentType : byte
{
    Application = 1,
    Proposal    = 2,
    Commit      = 3,
}

/// <summary>MLS proposal types (RFC 9420 §12.1).</summary>
internal enum ProposalType : ushort
{
    Add    = 1,
    Update = 2,
    Remove = 3,
    Psk    = 4,
    ReInit = 5,
    ExternalInit = 6,
    GroupContextExtensions = 7,
}

/// <summary>MLS credential types (RFC 9420 §5.3.1).</summary>
internal enum CredentialType : ushort
{
    Basic    = 1,
    X509     = 2,
}

/// <summary>MLS sender types (RFC 9420 §6.2.1).</summary>
internal enum SenderType : byte
{
    Member         = 1,
    External       = 2,
    NewMemberProposal = 3,
    NewMemberCommit   = 4,
}

/// <summary>MLS leaf node source (RFC 9420 §7.2).</summary>
internal enum LeafNodeSource : byte
{
    KeyPackage = 1,
    Update     = 2,
    Commit     = 3,
}
