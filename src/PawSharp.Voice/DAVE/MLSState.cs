#nullable enable
using System;
using System.Collections.Concurrent;
using PawSharp.Voice.DAVE.MLS.State;

namespace PawSharp.Voice.DAVE;

/// <summary>
/// MLS (Message Layer Security, RFC 9420) group state for DAVE.
///
/// Implements the full RFC 9420 protocol for the DAVE ciphersuite:
///   MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519
///
/// This class is the public face of the MLS implementation.  All heavy lifting
/// is delegated to <see cref="MLSGroupState"/> (the internal RFC 9420 engine).
///
/// Per-SSRC AES-128 sender keys are derived from the epoch secret on first access
/// and cached for the lifetime of the epoch.  The cache is invalidated on every
/// epoch advance (ProcessCommit / ProcessWelcome).
/// </summary>
public sealed class MLSState : IDisposable
{
    // ── Public state ─────────────────────────────────────────────────────────

    /// <summary>Current MLS epoch number.</summary>
    public ulong EpochNumber => _group.EpochNumber;

    /// <summary>
    /// Current 32-byte epoch secret (the DAVE-specific MLS exporter secret).
    /// Null until the group has been established via <see cref="ProcessWelcome"/>.
    /// </summary>
    public byte[]? EpochSecret => _group.DaveEpochSecret;

    /// <summary>MLS group identifier.</summary>
    public byte[]? GroupId => _group.GroupId;

    /// <summary>True when the MLS group has been established and the DAVE protocol is active.</summary>
    public bool IsInitialized => _group.IsInitialized;

    // ── Internal references ───────────────────────────────────────────────────

    private readonly MLSGroupState _group = new();

    // Per-SSRC sender keys, derived lazily from the current epoch secret
    private readonly ConcurrentDictionary<uint, byte[]> _senderKeyCache = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Processes an MLS Welcome message (opcode 25).
    ///
    /// Decrypts the GroupSecrets using the local init key, derives the epoch secret
    /// via the RFC 9420 key schedule, and transitions the group to epoch 1.
    /// </summary>
    /// <param name="welcome">Raw TLS-encoded MLS Welcome bytes.</param>
    /// <param name="groupId">Optional group ID for validation (may be null).</param>
    public void ProcessWelcome(byte[] welcome, byte[]? groupId = null)
    {
        if (welcome is null || welcome.Length == 0)
            throw new ArgumentException("Welcome payload must not be empty.", nameof(welcome));

        _group.ProcessWelcome(welcome, groupId);
        _senderKeyCache.Clear();
    }

    /// <summary>
    /// Processes an MLS Commit (opcode 26).
    ///
    /// Applies pending proposals and the UpdatePath to the ratchet tree,
    /// then advances the key schedule to derive the new epoch secret.
    /// </summary>
    /// <param name="commit">Raw TLS-encoded MLS Commit bytes.</param>
    public void ProcessCommit(byte[] commit)
    {
        if (commit is null || commit.Length == 0)
            throw new ArgumentException("Commit payload must not be empty.", nameof(commit));

        _group.ProcessCommit(commit);
        _senderKeyCache.Clear();
    }

    /// <summary>
    /// Queues one or more MLS Proposals (opcode 27) for application at the next Commit.
    ///
    /// Proposals (Add / Remove / Update) are parsed and cached internally.
    /// They do not affect the epoch secret until a Commit is processed.
    /// </summary>
    /// <param name="proposals">TLS-encoded MLS Proposal bytes (may be concatenated).</param>
    public void ProcessProposals(byte[] proposals)
    {
        if (proposals is null || proposals.Length == 0) return;
        _group.ProcessProposals(proposals);
    }

    // ── Key access ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the 16-byte AES-128 sender key for the given SSRC in the current epoch.
    /// Keys are derived on first access and cached for the lifetime of the epoch.
    /// </summary>
    /// <param name="ssrc">The sender's SSRC.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called before the MLS group has been established.
    /// </exception>
    public byte[] GetSenderKey(uint ssrc)
    {
        var epochSecret = EpochSecret;
        if (epochSecret is null)
            throw new InvalidOperationException("MLS group has not been initialised yet (no epoch secret).");

        return _senderKeyCache.GetOrAdd(ssrc, s => DAVEKeyDerivation.DeriveEncryptionKey(epochSecret, s));
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        _group.Dispose();
        _senderKeyCache.Clear();
    }
}
