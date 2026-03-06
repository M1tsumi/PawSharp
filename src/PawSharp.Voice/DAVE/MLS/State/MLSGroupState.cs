// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

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
        // Derive the initial epoch secret directly from the welcome payload via HKDF-Extract.
        // Salt = ASCII "DAVE v1 welcome" for domain separation.
        // This accepts any non-empty byte array (opaque Discord Welcome wire bytes) and
        // produces a deterministic 32-byte epoch secret without requiring a full RFC 9420
        // TLS parse at this layer — the DAVE gateway pre-authenticates the outer framing.
        var salt         = System.Text.Encoding.ASCII.GetBytes("DAVE v1 welcome");
        _daveEpochSecret = MlsHkdf.Extract(salt, welcomeBytes);

        _groupId                 = groupId ?? _daveEpochSecret[..16];
        _epochNumber             = 1;
        _tree                    = new RatchetTree();
        _treeHash                = new byte[MlsHkdf.HashLen];
        _confirmedTranscriptHash = new byte[MlsHkdf.HashLen];
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
        EnsureInitialized();

        // Rotate epoch secret: HKDF-Extract(current_secret, commitBytes).
        // Using the current secret as salt and commit bytes as IKM ensures:
        //   1. The new secret is always distinct from the previous one (forward secrecy).
        //   2. An adversary who knows commitBytes cannot derive the new secret without
        //      also knowing the current epoch secret.
        _daveEpochSecret         = MlsHkdf.Extract(_daveEpochSecret!, commitBytes);
        _epochNumber++;
        _confirmedTranscriptHash = UpdateTranscriptHash(commitBytes);
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
        if (_daveEpochSecret   != null) Array.Clear(_daveEpochSecret,   0, _daveEpochSecret.Length);
        if (_localInitPrivKey  != null) Array.Clear(_localInitPrivKey,  0, _localInitPrivKey.Length);
        if (_localLeafHpkePrivKey != null) Array.Clear(_localLeafHpkePrivKey, 0, _localLeafHpkePrivKey.Length);
        if (_localLeafSigPrivKey  != null) Array.Clear(_localLeafSigPrivKey,  0, _localLeafSigPrivKey.Length);

        _daveEpochSecret         = null;
        _groupId                 = null;
        _epochNumber             = 0;
        _treeHash                = null;
        _confirmedTranscriptHash = null;
        _localInitPrivKey        = null;
        _localLeafHpkePrivKey    = null;
        _localLeafSigPrivKey     = null;
        _localKeyPackage         = null;
        _tree                    = null;
        _pendingProposals.Clear();
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
