#nullable enable
namespace PawSharp.Voice.DAVE;

/// <summary>
/// Discord voice gateway opcodes, including DAVE E2EE-specific opcodes (21–31).
/// Standard voice opcodes are 0–20; DAVE opcodes are 21–31.
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

    /// <summary>Client sends its MLS key package to the server.</summary>
    DaveMlsKeyPackage = 21,
    /// <summary>Server requests a new MLS key package from the client.</summary>
    DaveMlsKeyPackageRequest = 22,
    /// <summary>
    /// Server tells clients to prepare for an upcoming DAVE protocol transition
    /// (e.g. switching DAVE version or establishing a new MLS group).
    /// </summary>
    DaveProtocolPrepareTransition = 23,
    /// <summary>
    /// Server signals that the DAVE protocol transition is now active.
    /// Clients should switch to using DAVE encryption.
    /// </summary>
    DaveProtocolReady = 24,
    /// <summary>Server distributes an MLS Welcome message to a new member.</summary>
    DaveMlsWelcome = 25,
    /// <summary>A member sends (or the server forwards) an MLS Commit message.</summary>
    DaveMlsCommit = 26,
    /// <summary>One or more MLS Proposal messages.</summary>
    DaveMlsProposals = 27,
    /// <summary>Server tells clients to prepare for a new MLS epoch.</summary>
    DaveProtocolPrepareEpoch = 28,
    /// <summary>
    /// A member announces the most recently applied MLS commit transition,
    /// so other members know which epoch they are on.
    /// </summary>
    DaveMlsAnnounceCommitTransition = 29,
    /// <summary>
    /// Server rejects an MLS commit and sends a new Welcome message instead,
    /// forcing the client to rejoin.
    /// </summary>
    DaveMlsInvalidCommitWelcome = 30,
    /// <summary>Server distributes the MLS external sender package.</summary>
    DaveMlsExternalSenderPackage = 31,
}
