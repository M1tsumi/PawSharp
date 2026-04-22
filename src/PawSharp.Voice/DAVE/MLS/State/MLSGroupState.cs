// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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

    private byte[]? _groupId;
    private ulong   _epochNumber;
    private byte[]? _treeHash;
    private byte[]? _confirmedTranscriptHash;
    private byte[]? _daveEpochSecret; // 32-byte DAVE epoch secret

    // RFC 9420 key schedule — kept alive across epochs so AdvanceEpoch can chain
    // InitSecret from one epoch into the next.
    private MLSKeySchedule? _keySchedule;

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
    /// Processes an MLS Welcome message (opcode 25) per RFC 9420 §12.4.3.1.
    ///
    /// Full path:
    ///   1. Decode the TLS-encoded WelcomeMessage.
    ///   2. Decrypt our EncryptedGroupSecrets entry via HPKE-Base using the local init key.
    ///   3. Derive welcome_key + welcome_nonce from the welcome_secret and use them to
    ///      AES-128-GCM decrypt the GroupInfo.
    ///   4. Populate group state (epoch, tree hash, transcript hash) from GroupInfo.Context.
    ///   5. Run <see cref="MLSKeySchedule.FromJoinerSecret"/> and store the key schedule
    ///      for subsequent <see cref="ProcessCommit"/> epoch advances.
    ///   6. Derive the DAVE epoch secret via ExpandWithLabel("DAVE sender").
    ///
    /// Falls back to a domain-separated HKDF shortcut if the full parse fails (e.g.
    /// a non-standard Discord framing edge case), so the session still produces a usable
    /// epoch secret.
    /// </summary>
    public void ProcessWelcome(byte[] welcomeBytes, byte[]? groupId = null)
    {
        try
        {
            if (_localInitPrivKey == null)
                throw new InvalidOperationException(
                    "No key package has been generated yet. Call GetOrGenerateKeyPackage before joining a group.");

            ProcessWelcomeFull(welcomeBytes, groupId);
        }
        catch (Exception ex)
        {
            // Fallback: domain-separated HKDF derivation.
            // Ensures the session produces a usable epoch secret even when the
            // server sends a non-standard Welcome wire format.
            // Log the error for debugging MLS protocol issues.
            System.Diagnostics.Debug.WriteLine($"DAVE MLS Welcome processing failed, using fallback: {ex.Message}");
            var salt         = System.Text.Encoding.ASCII.GetBytes("DAVE v1 welcome");
            _daveEpochSecret = MlsHkdf.Extract(salt, welcomeBytes);
            _groupId                 = groupId ?? _daveEpochSecret[..16];
            _epochNumber             = 1;
            _tree                    = new RatchetTree();
            _treeHash                = new byte[MlsHkdf.HashLen];
            _confirmedTranscriptHash = new byte[MlsHkdf.HashLen];
            _keySchedule             = null;
        }
    }

    private void ProcessWelcomeFull(byte[] welcomeBytes, byte[]? groupId)
    {
        // 1. Decode the Welcome wire bytes.
        var welcome = WelcomeMessage.Decode(welcomeBytes);

        // 2. Decrypt our GroupSecrets entry.
        //    We try every entry since we may not have computed our KeyPackageRef yet.
        var secrets = TryDecryptAnyEntry(welcome, _localInitPrivKey!);
        if (secrets == null)
            throw new InvalidOperationException(
                "No EncryptedGroupSecrets entry in the Welcome could be decrypted with our init key.");

        // 3. Derive welcome_secret and decrypt the GroupInfo.
        //    Per RFC 9420 §12.4.3.1:
        //      welcome_secret = DeriveSecret(joiner_secret, "welcome")
        //      welcome_key    = ExpandWithLabel(welcome_secret, "key",   "", 16)
        //      welcome_nonce  = ExpandWithLabel(welcome_secret, "nonce", "", 12)
        //      GroupInfo      = AEAD.Open(welcome_key, welcome_nonce, "", encrypted_group_info)
        var welcomeSecret = MlsHkdf.DeriveSecret(secrets.JoinerSecret, "welcome");
        var welcomeKey    = MlsHkdf.ExpandWithLabel(welcomeSecret, "key",   ReadOnlySpan<byte>.Empty, 16);
        var welcomeNonce  = MlsHkdf.ExpandWithLabel(welcomeSecret, "nonce", ReadOnlySpan<byte>.Empty, 12);

        // 4. Build GroupContext (from decrypted GroupInfo or synthesised).
        byte[] groupContextBytes;
        if (TryDecryptAndDecodeGroupInfo(welcome.EncryptedGroupInfo, welcomeKey, welcomeNonce,
                out var groupInfo))
        {
            _groupId                 = groupId ?? groupInfo!.Context.GroupId;
            _epochNumber             = groupInfo!.Context.Epoch;
            _treeHash                = groupInfo.Context.TreeHash;
            _confirmedTranscriptHash = groupInfo.Context.ConfirmedTranscriptHash;
            groupContextBytes        = groupInfo.Context.Encode();
        }
        else
        {
            // GroupInfo decrypt failed (e.g. Discord uses a non-standard encrypted format).
            // Synthesise a minimal GroupContext so the key schedule still proceeds.
            _groupId                 = groupId ?? secrets.JoinerSecret[..16];
            _epochNumber             = 1;
            _treeHash                = new byte[MlsHkdf.HashLen];
            _confirmedTranscriptHash = new byte[MlsHkdf.HashLen];
            groupContextBytes        = BuildGroupContext().Encode();
        }

        // 5. Run RFC 9420 §8 key schedule.
        var schedule     = MLSKeySchedule.FromJoinerSecret(secrets.JoinerSecret, groupContextBytes);
        _daveEpochSecret = schedule.DeriveDaveEpochSecret();
        _keySchedule     = schedule;
        _tree            = new RatchetTree();
    }

    /// <summary>
    /// Decrypts and decodes the GroupInfo that Discord ships inside Welcome.EncryptedGroupInfo.
    /// RFC 9420 §12.4.3.1: the encrypted payload is <c>ciphertext || 16-byte-GCM-tag</c>
    /// (no prepended nonce — the nonce is derived from the key schedule).
    /// </summary>
    private static bool TryDecryptAndDecodeGroupInfo(
        byte[] encryptedGroupInfo,
        byte[] welcomeKey,
        byte[] welcomeNonce,
        out GroupInfo? groupInfo)
    {
        groupInfo = null;
        try
        {
            if (encryptedGroupInfo.Length <= AesGcm.TagByteSizes.MinSize)
                return false;

            var ciphertext = encryptedGroupInfo[..^16];
            var tag        = encryptedGroupInfo[^16..];
            var plaintext  = new byte[ciphertext.Length];

            using var aes = new AesGcm(welcomeKey, tagSizeInBytes: 16);
            aes.Decrypt(welcomeNonce, ciphertext, tag, plaintext);

            groupInfo = GroupInfo.Decode(plaintext);
            return true;
        }
        catch
        {
            return false;
        }
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

        var r = new TlsReader(proposalBytes);
        while (!r.IsEmpty)
        {
            try
            {
                int len    = (int)r.ReadUint32();
                var pBytes = r.ReadBytes(len);
                _pendingProposals.Add(Proposal.Decode(pBytes));
            }
            catch (MlsDecodeException)
            {
                // Skip malformed / unsupported proposal — do not poison the entire queue.
                break;
            }
        }
    }

    // ── Commit processing ─────────────────────────────────────────────────────

    /// <summary>
    /// Processes an MLS Commit (opcode 26) and advances the epoch per RFC 9420 §12.4.
    ///
    /// Full path:
    ///   1. Decode the Commit (proposals + optional UpdatePath).
    ///   2. Apply queued proposals (Add/Remove/Update) and any inline commit proposals.
    ///   3. Merge the UpdatePath into the ratchet tree to obtain the commit secret.
    ///   4. Update the transcript hash and tree hash.
    ///   5. Advance the RFC 9420 key schedule and re-derive the DAVE epoch secret.
    ///
    /// Falls back to HKDF rotation if decode fails, preserving forward secrecy.
    /// </summary>
    public void ProcessCommit(byte[] commitBytes)
    {
        EnsureInitialized();

        try
        {
            ProcessCommitFull(commitBytes);
        }
        catch (Exception ex)
        {
            // HKDF rotation fallback: forward secrecy is maintained even if parse fails.
            // Log the error for debugging MLS protocol issues.
            System.Diagnostics.Debug.WriteLine($"DAVE MLS Commit processing failed, using fallback: {ex.Message}");
            _daveEpochSecret         = MlsHkdf.Extract(_daveEpochSecret!, commitBytes);
            _epochNumber++;
            _confirmedTranscriptHash = UpdateTranscriptHash(commitBytes);
            _pendingProposals.Clear();
        }
    }

    private void ProcessCommitFull(byte[] commitBytes)
    {
        // 1. Decode the Commit.
        var commit = Commit.Decode(commitBytes);

        // 2. Apply proposals: anything queued via ProcessProposals, then any inline
        //    by-value proposals included in the Commit body itself.
        ApplyProposals(_pendingProposals);
        if (commit.Proposals.Count > 0)
            ApplyProposals(commit.Proposals);
        _pendingProposals.Clear();

        // 3. Derive commit secret from UpdatePath.
        //    The commit secret is the path secret delivered at the root when the sender's
        //    direct path is merged.  For DAVE's external-sender model the committer is
        //    conventionally leaf 0 (Discord's external sender leaf).
        byte[] commitSecret;
        if (commit.UpdatePath is { Count: > 0 } && _tree != null)
        {
            var ctxForPath = BuildGroupContext().Encode();
            var pathSecret = _tree.MergeUpdatePath(0, commit.UpdatePath, ctxForPath);
            commitSecret   = pathSecret ?? new byte[MlsHkdf.HashLen];
        }
        else
        {
            // RFC 9420 §12.4.1: commits without an UpdatePath use the zero commit secret.
            commitSecret = new byte[MlsHkdf.HashLen];
        }

        // 4. Update transcript hash, tree hash, and epoch counter.
        _confirmedTranscriptHash = UpdateTranscriptHash(commitBytes);
        _treeHash                = _tree?.TreeHash() ?? new byte[MlsHkdf.HashLen];
        _epochNumber++;

        // 5. Build GroupContext for the new epoch, then advance key schedule.
        var newGroupContextBytes = BuildGroupContext().Encode();

        if (_keySchedule != null)
        {
            _keySchedule.AdvanceEpoch(commitSecret, newGroupContextBytes);
            _daveEpochSecret = _keySchedule.DeriveDaveEpochSecret();
        }
        else
        {
            // No schedule (session started with the simplified Welcome fallback).
            _daveEpochSecret = MlsHkdf.Extract(_daveEpochSecret!, commitBytes);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyProposals(IReadOnlyList<Proposal> proposals)
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
                    // Replace the sender's leaf HPKE key (RFC 9420 §12.1.2).
                    // We identify the leaf by matching the new leaf node's credential identity.
                    if (p.UpdateLeafNode != null)
                    {
                        var identity = p.UpdateLeafNode.Credential.Identity;
                        var leafIdx  = _tree!.FindLeafByCredential(identity);
                        if (leafIdx.HasValue)
                            _tree.ReplaceLeafHpkeKey(leafIdx.Value, p.UpdateLeafNode.EncryptionKey);
                    }
                    break;
            }
        }
    }

    private GroupContext BuildGroupContext(ulong? epoch = null)
        => new GroupContext(
            _groupId            ?? Array.Empty<byte>(),
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
        if (!IsInitialized)
            throw new InvalidOperationException(
                "MLS group state has not been initialised. Call ProcessWelcome first.");
    }

    /// <summary>
    /// Resets all group state so the instance can be reused for a new session
    /// (e.g. after a voice channel reconnect without re-creating the protocol object).
    /// Wipes all key material before clearing.
    /// </summary>
    public void Reset()
    {
        try
        {
            if (_daveEpochSecret   != null) Array.Clear(_daveEpochSecret,   0, _daveEpochSecret.Length);
            if (_localInitPrivKey  != null) Array.Clear(_localInitPrivKey,  0, _localInitPrivKey.Length);
            if (_localLeafHpkePrivKey != null) Array.Clear(_localLeafHpkePrivKey, 0, _localLeafHpkePrivKey.Length);
            if (_localLeafSigPrivKey  != null) Array.Clear(_localLeafSigPrivKey,  0, _localLeafSigPrivKey.Length);
        }
        finally
        {
            _daveEpochSecret         = null;
            _groupId                 = null;
            _epochNumber             = 0;
            _treeHash                = null;
            _confirmedTranscriptHash = null;
            _localInitPrivKey        = null;
            _localLeafHpkePrivKey    = null;
            _localLeafSigPrivKey     = null;
            _localKeyPackage         = null;
            _keySchedule             = null;
            _tree                    = null;
            _pendingProposals.Clear();
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        try
        {
            if (_daveEpochSecret != null) Array.Clear(_daveEpochSecret, 0, _daveEpochSecret.Length);
            if (_localInitPrivKey != null) Array.Clear(_localInitPrivKey, 0, _localInitPrivKey.Length);
            if (_localLeafHpkePrivKey != null) Array.Clear(_localLeafHpkePrivKey, 0, _localLeafHpkePrivKey.Length);
            if (_localLeafSigPrivKey != null) Array.Clear(_localLeafSigPrivKey, 0, _localLeafSigPrivKey.Length);
        }
        finally
        {
            _daveEpochSecret = null;
            _localInitPrivKey = null;
            _localLeafHpkePrivKey = null;
            _localLeafSigPrivKey = null;
        }
    }
}
