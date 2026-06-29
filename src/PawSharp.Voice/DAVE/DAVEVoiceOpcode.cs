// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
namespace PawSharp.Voice.DAVE;

/// <summary>
/// Discord voice gateway opcodes, including DAVE E2EE-specific opcodes (21–31).
/// Standard voice opcodes are 0–20; DAVE opcodes are 21–31.
///
/// Mapping per Discord's official voice gateway specification.
/// Ops 21–24, 31 are JSON text messages.
/// Ops 25–30 are binary WebSocket messages.
/// </summary>
public enum DAVEVoiceOpcode
{
    /// <summary>Begin a new voice session.</summary>
    Identify = 0,
    /// <summary>Select the voice protocol and encryption mode.</summary>
    SelectProtocol = 1,
    /// <summary>Server responds with connection info (SSRC, port, etc.).</summary>
    Ready = 2,
    /// <summary>Keep the connection alive.</summary>
    Heartbeat = 3,
    /// <summary>Server sends encryption keys and session description.</summary>
    SessionDescription = 4,
    /// <summary>Notify speaking status change.</summary>
    Speaking = 5,
    /// <summary>Heartbeat acknowledgement.</summary>
    HeartbeatAck = 6,
    /// <summary>Resume a dropped session.</summary>
    Resume = 7,
    /// <summary>Server sends the heartbeat interval.</summary>
    Hello = 8,
    /// <summary>Server acknowledges a resumed session.</summary>
    Resumed = 9,
    /// <summary>List of SSRCs for clients currently connected.</summary>
    ClientsConnect = 11,
    /// <summary>A client disconnected from the voice channel.</summary>
    ClientDisconnect = 13,
    /// <summary>Session-level update (used for codec negotiation).</summary>
    SessionUpdate = 14,
    /// <summary>Request the user's media receive sink preferences.</summary>
    MediaSinkWants = 15,
    /// <summary>Backend version info.</summary>
    VoiceBackendVersion = 16,
    /// <summary>Update per-channel audio options.</summary>
    ChannelOptionsUpdate = 17,
    /// <summary>Bitfield of voice flags active in the session.</summary>
    Flags = 18,
    /// <summary>Speed-test probe payload.</summary>
    SpeedTest = 19,
    /// <summary>Platform identifier sent by the client.</summary>
    Platform = 20,

    // ── DAVE E2EE opcodes ─────────────────────────────────────────────────
    // Ops 21–24, 31: JSON text messages
    // Ops 25–30: Binary WebSocket messages

    /// <summary>
    /// Server announces an upcoming DAVE protocol transition (e.g. downgrade).
    /// JSON payload: dave_protocol_version, dave_transition_id, allowed_cipher_suites, allowed_versions.
    /// If max_dave_protocol_version &lt; dave_protocol_version, the client must disconnect.
    /// </summary>
    DavePrepareTransition = 21,

    /// <summary>
    /// Server signals the DAVE transition is now executing.
    /// After this op, binary DAVE messages follow.
    /// </summary>
    DaveExecuteTransition = 22,

    /// <summary>
    /// Client acknowledges DAVE transition readiness.
    /// JSON payload: dave_transition_id, key_package (base64).
    /// </summary>
    DaveTransitionReady = 23,

    /// <summary>
    /// Server announces a new epoch (version/group change).
    /// JSON payload: epoch, group_id.
    /// </summary>
    DavePrepareEpoch = 24,

    /// <summary>
    /// Server sends the MLS external sender credential and public key.
    /// BINARY payload.
    /// </summary>
    DaveMlsExternalSender = 25,

    /// <summary>
    /// Client sends its MLS key package for group membership.
    /// BINARY payload.
    /// </summary>
    DaveMlsKeyPackage = 26,

    /// <summary>
    /// Server sends MLS proposals (Add, Remove, Update).
    /// BINARY payload.
    /// </summary>
    DaveMlsProposals = 27,

    /// <summary>
    /// Client sends an MLS Commit message with an optional Welcome.
    /// BINARY payload.
    /// </summary>
    DaveMlsCommitWelcome = 28,

    /// <summary>
    /// Server announces the most recently applied MLS commit transition,
    /// confirming the epoch advancement.
    /// BINARY payload.
    /// </summary>
    DaveMlsAnnounceCommitTransition = 29,

    /// <summary>
    /// Server distributes an MLS Welcome message to late-joining members
    /// or for recovery after an invalid commit.
    /// BINARY payload.
    /// </summary>
    DaveMlsWelcome = 30,

    /// <summary>
    /// Client signals that a received MLS commit or Welcome is invalid.
    /// JSON payload with error details.
    /// </summary>
    DaveMlsInvalidCommitWelcome = 31,
}
