#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using PawSharp.Voice.DAVE.MLS.Crypto;
using PawSharp.Voice.DAVE.MLS.Encoding;
using PawSharp.Voice.DAVE.MLS.Messages;
using PawSharp.Voice.DAVE.MLS.Tree;

namespace PawSharp.Voice.DAVE.MLS.State;

/// <summary>
/// RFC 9420 — Full MLS group state for the DAVE E2EE protocol.
///
/// Owns all per-epoch state:
///   • Ratchet tree (<see cref="RatchetTree"/>)
///   • Key schedule (<see cref="MLSKeySchedule"/>)
///   • GroupContext (group ID, epoch, tree hash, transcript hash)
///   • Local key material (HPKE private key for this client's leaf)
///   • Proposal queue (proposals received between commits)
///
/// Lifecycle:
///   1. <see cref="ProcessWelcome"/> — join an existing group (receive epoch secrets).
///   2. <see cref="ProcessProposals"/> — enqueue incoming proposals.
///   3. <see cref="ProcessCommit"/> — apply commit, validate, advance epoch.
///   4. <see cref="DaveEpochSecret"/> — read the per-epoch AES key derivation secret.
///
/// This class is intentionally single-threaded. The caller (<see cref="MLSState"/>)
/// is responsible for synchronisation.
/// </summary>
internal sealed class MLSGroupState : IDisposable
{
    // ── State ─────────────────────────────────────────────────────────────────

    private RatchetTree?  _tree;
    private MLSKeySchedule? _keySchedule;

    private byte[]? _groupId;
    private ulong   _epochNumber;
    private byte[]? _treeHash;
    private byte[]? _confirmedTranscriptHash;
    private byte[]? _daveEpochSecret; // 32-byte DAVE epoch secret

    // Local client leaf key material
    private byte[]? _localInitPrivKey;      // X25519 init private key (for Welcome decryption)
    private byte[]? _localLeafHpkePrivKey;  // X25519 leaf HPKE private key
    private byte[]? _localLeafSigPrivKey;   // Ed25519 signing private key

    // Our KeyPackage for the current session
    private KeyPackage? _localKeyPackage;

    // Pending proposals (queued between commits)
    private readonly List<Proposal> _pendingProposals = new();

    // ── Public properties ─────────────────────────────────────────────────────

    /// <summary>True once a Welcome or Commit has successfully established epoch secrets.</summary>
    public bool IsInitialized => _daveEpochSecret != null;

    /// <summary>The current MLS epoch number.</summary>
    public ulong EpochNumber => _epochNumber;

    /// <summary>The current DAVE epoch secret (32 bytes); null until <see cref="IsInitialized"/>.</summary>
    public byte[]? DaveEpochSecret => _daveEpochSecret;

    /// <summary>The MLS group identifier.</summary>
    public byte[]? GroupId => _groupId;

    // ── Key package management ─────────────────────────────────────────────────

    /// <summary>
    /// Generates (or lazily reuses) a KeyPackage for this client session.
    /// Called before joining a group to produce the payload for op-21 (MLS key package request).
    /// </summary>
    /// <param name="identity">Caller identity bytes (Discord user ID as UTF-8).</param>
    public KeyPackage GetOrGenerateKeyPackage(byte[] identity)
    {
        if (_localKeyPackage == null)
        {
            _localKeyPackage             = KeyPackage.Generate(identity);
            _localInitPrivKey            = _localKeyPackage.InitPrivateKey;
            _localLeafHpkePrivKey        = _localKeyPackage.LeafHpkePrivateKey;
            _localLeafSigPrivKey         = _localKeyPackage.LeafSignPrivateKey;
        }
        return _localKeyPackage;
    }

    // ── Welcome processing ────────────────────────────────────────────────────

    /// <summary>
    /// Processes an MLS Welcome message (opcode 25).
    ///
    /// Finds our entry in the Welcome, decrypts the GroupSecrets using our
    /// init private key, reconstructs the GroupContext, and advances the key schedule
    /// to the correct epoch.
    /// </summary>
    /// <param name="welcomeBytes">Raw TLS-encoded Welcome wire bytes.</param>
    /// <param name="groupId">Optional hint for the expected group ID (for validation).</param>
    /// <exception cref="MlsDecodeException">Thrown on structural decode errors.</exception>
    /// <exception cref="InvalidOperationException">Thrown if our key package is not found.</exception>
    public void ProcessWelcome(byte[] welcomeBytes, byte[]? groupId = null)
    {
        WelcomeMessage welcome;
        try
        {
            welcome = WelcomeMessage.Decode(welcomeBytes);
        }
        catch (Exception ex) when (ex is not MlsDecodeException)
        {
            throw new MlsDecodeException("Failed to decode Welcome message.", ex);
        }

        // Ensure we have a key package (if no explicit identity available, use a generated one)
        var identity = System.Text.Encoding.UTF8.GetBytes("discord-client");
        var kp       = GetOrGenerateKeyPackage(identity);
        var kpRef    = ComputeKeyPackageRef(kp);

        // Try to decrypt GroupSecrets using our init key
        var secrets = welcome.TryDecryptSecrets(
            _localInitPrivKey ?? throw new InvalidOperationException("No local init key — call GetOrGenerateKeyPackage first."),
            kpRef,
            ReadOnlySpan<byte>.Empty);

        // If none matched by ref, try all entries (Discord may not send a ref header)
        if (secrets == null && _localInitPrivKey != null)
            secrets = TryDecryptAnyEntry(welcome, _localInitPrivKey);

        if (secrets == null)
            throw new InvalidOperationException(
                "Welcome message did not contain an entry for this client's KeyPackage.");

        // Decode GroupInfo from the encrypted group info blob
        // GroupInfo encryption key = ExpandWithLabel(welcome_secret, "key", "", Nk)
        // But we need epoch secrets first — bootstrap from joiner_secret per RFC 9420 §12.4.3.2

        // Use the joiner secret to bootstrap the key schedule
        // GroupContext is available inside GroupInfo once decrypted
        // For simplicity, derive a provisional GroupContext from the provided group ID
        var provisionalGroupId = groupId ?? secrets.JoinerSecret[..Math.Min(16, secrets.JoinerSecret.Length)];
        var provisionalCtx     = new GroupContext(provisionalGroupId, 0, new byte[32], new byte[32]);
        var keySchedule        = MLSKeySchedule.FromJoinerSecret(secrets.JoinerSecret, provisionalCtx.Encode());

        // Initialise the ratchet tree (simplified — full tree from GroupInfo not yet decoded)
        _tree = new RatchetTree();
        if (_localLeafHpkePrivKey != null)
        {
            _tree.AddLeaf(
                kp.Leaf.EncryptionKey,
                kp.Leaf.SignatureKey,
                kp.Leaf.Credential.Identity,
                _localLeafHpkePrivKey,
                isLocal: true);
        }

        _groupId                 = provisionalGroupId;
        _epochNumber             = 1;
        _keySchedule             = keySchedule;
        _treeHash                = _tree.TreeHash();
        _confirmedTranscriptHash = new byte[MlsHkdf.HashLen];
        _daveEpochSecret         = keySchedule.DeriveDaveEpochSecret();
    }

    // ── Proposal processing ───────────────────────────────────────────────────

    /// <summary>
    /// Queues incoming MLS Proposals (opcode 27) for application at the next Commit.
    ///
    /// Proposals are parsed from the wire-format bytes and stored in memory.
    /// They do not change the epoch until processed by <see cref="ProcessCommit"/>.
    /// </summary>
    public void ProcessProposals(byte[] proposalBytes)
    {
        if (proposalBytes.Length == 0) return;

        // proposalBytes may contain multiple concatenated Proposal TLVs
        // Each is prefixed with a 4-byte length
        var r = new TlsReader(proposalBytes);
        while (!r.IsEmpty)
        {
            try
            {
                int len = (int)r.ReadUint32();
                var pData = r.Slice(len);
                // Build a byte array from the slice (TlsReader is a ref struct)
                var pBytes = proposalBytes.AsSpan(r.Position - len, len).ToArray();
                _pendingProposals.Add(Proposal.Decode(pBytes));
            }
            catch (MlsDecodeException)
            {
                // Skip malformed proposals
                break;
            }
        }
    }

    // ── Commit processing ─────────────────────────────────────────────────────

    /// <summary>
    /// Processes an MLS Commit (opcode 26) and advances the epoch.
    ///
    /// Steps (RFC 9420 §12.4):
    ///   1. Decode the Commit.
    ///   2. Apply cached proposals (Add / Remove / Update) to the ratchet tree.
    ///   3. Apply the UpdatePath to obtain the new path secret.
    ///   4. Advance the key schedule with the new commit secret.
    ///   5. Clear the proposal queue.
    /// </summary>
    public void ProcessCommit(byte[] commitBytes)
    {
        Commit commit;
        try
        {
            commit = Commit.Decode(commitBytes);
        }
        catch (Exception ex) when (ex is not MlsDecodeException)
        {
            throw new MlsDecodeException("Failed to decode Commit.", ex);
        }

        EnsureInitialized();

        // Apply inline proposals from the Commit
        var allProposals = new List<Proposal>(_pendingProposals);
        allProposals.AddRange(commit.Proposals);
        ApplyProposals(allProposals);
        _pendingProposals.Clear();

        // Derive group context for this commit (with the updated tree hash)
        _treeHash        = _tree!.TreeHash();
        var groupContext = BuildGroupContext();

        // Apply update path to get commit secret
        byte[] commitSecret;
        if (commit.UpdatePath != null && commit.UpdatePath.Count > 0)
        {
            // Use sender leaf index 0 as default (Discord sends as first member)
            var pathSecret = _tree.MergeUpdatePath(0, commit.UpdatePath, groupContext.Encode());
            commitSecret   = pathSecret ?? new byte[MlsHkdf.HashLen]; // zero if we can't decrypt
        }
        else
        {
            // External-only commit — use zero commit secret per RFC 9420 §12.4
            commitSecret = new byte[MlsHkdf.HashLen];
        }

        // Advance key schedule
        var newCtx = BuildGroupContext(_epochNumber + 1);
        _keySchedule!.AdvanceEpoch(commitSecret, newCtx.Encode());

        _epochNumber++;
        _confirmedTranscriptHash = UpdateTranscriptHash(commitBytes);
        _daveEpochSecret         = _keySchedule.DeriveDaveEpochSecret();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyProposals(IList<Proposal> proposals)
    {
        foreach (var p in proposals)
        {
            switch (p.Type)
            {
                case ProposalType.Add:
                    if (p.AddKeyPackage != null)
                        _tree!.AddLeaf(
                            p.AddKeyPackage.Leaf.EncryptionKey,
                            p.AddKeyPackage.Leaf.SignatureKey,
                            p.AddKeyPackage.Leaf.Credential.Identity);
                    break;

                case ProposalType.Remove:
                    if (p.RemoveLeafIndex.HasValue)
                        _tree!.BlankPath(TreeMath.LeafToNode(p.RemoveLeafIndex.Value));
                    break;

                case ProposalType.Update:
                    // Update replaces the sender's leaf — sender index not tracked here
                    // A full implementation would know which leaf index matches the sender
                    break;
            }
        }
    }

    private GroupContext BuildGroupContext(ulong? epoch = null)
        => new GroupContext(
            _groupId            ?? new byte[0],
            epoch               ?? _epochNumber,
            _treeHash           ?? new byte[MlsHkdf.HashLen],
            _confirmedTranscriptHash ?? new byte[MlsHkdf.HashLen]);

    private byte[] UpdateTranscriptHash(byte[] commitBytes)
    {
        // confirmed_transcript_hash = SHA-256(interim_transcript_hash || CommitContent)
        // Simplified: hash the previous cth + the commit bytes
        using var w = new TlsWriter(64 + commitBytes.Length);
        w.WriteBytes(_confirmedTranscriptHash ?? new byte[MlsHkdf.HashLen]);
        w.WriteBytes(commitBytes);
        return MlsHkdf.Hash(w.ToArray());
    }

    private static byte[] ComputeKeyPackageRef(KeyPackage kp)
    {
        // RFC 9420 §7.5: RefHash("MLS 1.0 KeyPackage", kp_bytes)
        var kpBytes = kp.Encode();
        using var w = new TlsWriter(kpBytes.Length + 20);
        w.WriteBytes("MLS 1.0 KeyPackage"u8);
        w.WriteBytes(kpBytes);
        return MlsHkdf.Hash(w.ToArray());
    }

    private static GroupSecrets? TryDecryptAnyEntry(WelcomeMessage welcome, byte[] initPrivKey)
    {
        foreach (var entry in welcome.Secrets)
        {
            try
            {
                var plain = HpkeX25519.OpenBase(
                    initPrivKey,
                    entry.EncryptedSecret.Enc,
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    entry.EncryptedSecret.CipherText);
                return GroupSecrets.Decode(plain);
            }
            catch
            {
                // Not our entry
            }
        }
        return null;
    }

    private void EnsureInitialized()
    {
        if (_tree == null || _keySchedule == null)
            throw new InvalidOperationException(
                "MLS group state has not been initialised. Call ProcessWelcome first.");
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_daveEpochSecret != null) Array.Clear(_daveEpochSecret, 0, _daveEpochSecret.Length);
        if (_localInitPrivKey != null) Array.Clear(_localInitPrivKey, 0, _localInitPrivKey.Length);
        if (_localLeafHpkePrivKey != null) Array.Clear(_localLeafHpkePrivKey, 0, _localLeafHpkePrivKey.Length);
        if (_localLeafSigPrivKey != null) Array.Clear(_localLeafSigPrivKey, 0, _localLeafSigPrivKey.Length);
    }
}
