// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using PawSharp.Voice.DAVE.MLS.Crypto;

namespace PawSharp.Voice.DAVE.MLS.State;

/// <summary>
/// RFC 9420 §8 — MLS Key Schedule.
///
/// Derives the per-epoch secrets from the commit secret (TreeKEM output)
/// and the current GroupContext.
///
/// Derivation chain (simplified for the DAVE ciphersuite):
///
///   init_secret(n-1)  →  ┐
///   commit_secret     →  ├→ Extract → joiner_secret
///                        ↓
///   joiner_secret     →  DeriveSecret("welcome")  → welcome_secret
///   welcome_secret    →  DeriveSecret("epoch")    → not used directly
///
///   joiner_secret + GroupContext →  Extract → epoch_secret
///   epoch_secret  →  DeriveSecret("exporter")     → exporter_secret
///   epoch_secret  →  DeriveSecret("confirmed")    → confirmation_key
///   epoch_secret  →  DeriveSecret("sender data")  → sender_data_secret
///
///   exporter_secret → used by DAVE to derive the 32-byte epoch secret
///                     passed to <see cref="DAVEKeyDerivation.ExtractEpochSecret"/>
///
/// References:
///   RFC 9420 §8 — https://www.rfc-editor.org/rfc/rfc9420#section-8
/// </summary>
internal sealed class MLSKeySchedule
{
    // ── Derived epoch secrets ─────────────────────────────────────────────────

    /// <summary>The init secret carried forward to the next epoch.</summary>
    public byte[] InitSecret { get; private set; } = Array.Empty<byte>();

    /// <summary>32-byte joiner secret (used to produce welcome_secret for new members).</summary>
    public byte[] JoinerSecret { get; private set; } = Array.Empty<byte>();

    /// <summary>32-byte epoch secret (root of all per-epoch key derivations).</summary>
    public byte[] EpochSecret { get; private set; } = Array.Empty<byte>();

    /// <summary>32-byte exporter secret — exposed to DAVE as the top-level epoch secret.</summary>
    public byte[] ExporterSecret { get; private set; } = Array.Empty<byte>();

    /// <summary>32-byte confirmation key — HMAC key for the confirmation tag.</summary>
    public byte[] ConfirmationKey { get; private set; } = Array.Empty<byte>();

    /// <summary>32-byte welcome secret — used to encrypt GroupInfo for new members.</summary>
    public byte[] WelcomeSecret { get; private set; } = Array.Empty<byte>();

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the key schedule for the very first epoch (epoch 0).
    /// Used when creating a new group.
    /// </summary>
    /// <param name="commitSecret">
    ///   The initial commit secret (32 zero bytes for the first epoch, per RFC 9420 §8).
    /// </param>
    /// <param name="groupContextBytes">Serialised GroupContext for epoch 0.</param>
    public MLSKeySchedule(byte[] commitSecret, byte[] groupContextBytes)
    {
        // init_secret for epoch 0 is the zero vector
        InitSecret = new byte[MlsHkdf.HashLen];
        (JoinerSecret, EpochSecret, ExporterSecret, ConfirmationKey, WelcomeSecret) =
            Advance(InitSecret, commitSecret, groupContextBytes);
    }

    // Private constructor used by FromJoinerSecret — fields are assigned directly.
    private MLSKeySchedule() { }

    /// <summary>
    /// Constructs a key schedule from a joiner_secret received in a Welcome.
    /// RFC 9420 §8.2 — joiner path.
    /// </summary>
    /// <param name="joinerSecret">Decrypted from Welcome's GroupSecrets.</param>
    /// <param name="groupContextBytes">The GroupContext from the GroupInfo in the Welcome.</param>
    public static MLSKeySchedule FromJoinerSecret(byte[] joinerSecret, byte[] groupContextBytes)
    {
        var (ep, exp, cfm, wlc, init) = DeriveFromJoiner(joinerSecret, groupContextBytes);
        return new MLSKeySchedule
        {
            JoinerSecret    = joinerSecret,
            EpochSecret     = ep,
            ExporterSecret  = exp,
            ConfirmationKey = cfm,
            WelcomeSecret   = wlc,
            InitSecret      = init,
        };
    }

    // ── Epoch advancement ─────────────────────────────────────────────────────

    /// <summary>
    /// Advances the key schedule by one epoch using the new commit secret.
    /// Must be called after every successful Commit.
    /// </summary>
    /// <param name="newCommitSecret">TreeKEM path secret at the root for this Commit.</param>
    /// <param name="newGroupContextBytes">GroupContext for the new epoch.</param>
    public void AdvanceEpoch(byte[] newCommitSecret, byte[] newGroupContextBytes)
    {
        (JoinerSecret, EpochSecret, ExporterSecret, ConfirmationKey, WelcomeSecret) =
            Advance(InitSecret, newCommitSecret, newGroupContextBytes);
    }

    // ── RFC 9420 §8 derivation ────────────────────────────────────────────────

    /// <summary>
    /// Core key schedule derivation.
    ///
    ///   joiner_secret = HKDF-Extract(init_secret, commit_secret)
    ///   welcome_secret = DeriveSecret(joiner_secret, "welcome")
    ///   epoch_secret  = HKDF-Extract(DeriveSecret(joiner_secret, "epoch"), groupContext)
    ///   exporter_secret = DeriveSecret(epoch_secret, "exporter")
    ///   confirmation_key = DeriveSecret(epoch_secret, "confirmed")
    ///   new_init_secret  = DeriveSecret(epoch_secret, "init")
    /// </summary>
    private (byte[] joiner, byte[] epoch, byte[] exporter, byte[] confirmation, byte[] welcome)
        Advance(byte[] initSecret, byte[] commitSecret, byte[] groupContextBytes)
    {
        // joiner_secret = HKDF-Extract(init_secret, commit_secret)
        var joiner = MlsHkdf.Extract(initSecret, commitSecret);

        // welcome_secret = DeriveSecret(joiner_secret, "welcome")
        var welcome = MlsHkdf.DeriveSecret(joiner, "welcome");

        // epoch_secret = HKDF-Extract(DeriveSecret(joiner_secret, "epoch"), GroupContext_bytes)
        var epochPrk = MlsHkdf.DeriveSecret(joiner, "epoch");
        var epoch    = MlsHkdf.Extract(epochPrk, groupContextBytes);

        // Update InitSecret for next epoch
        InitSecret = MlsHkdf.DeriveSecret(epoch, "init");

        var exporter = MlsHkdf.DeriveSecret(epoch, "exporter");
        var confirm  = MlsHkdf.DeriveSecret(epoch, "confirmed");

        return (joiner, epoch, exporter, confirm, welcome);
    }

    private static (byte[] epoch, byte[] exporter, byte[] confirm, byte[] welcome, byte[] init)
        DeriveFromJoiner(byte[] joiner, byte[] groupContextBytes)
    {
        var welcome  = MlsHkdf.DeriveSecret(joiner, "welcome");
        var epochPrk = MlsHkdf.DeriveSecret(joiner, "epoch");
        var epoch    = MlsHkdf.Extract(epochPrk, groupContextBytes);
        var init     = MlsHkdf.DeriveSecret(epoch, "init");
        var exporter = MlsHkdf.DeriveSecret(epoch, "exporter");
        var confirm  = MlsHkdf.DeriveSecret(epoch, "confirmed");
        return (epoch, exporter, confirm, welcome, init);
    }

    // ── DAVE exporter interface ────────────────────────────────────────────────

    /// <summary>
    /// Derives the 32-byte DAVE epoch secret from <see cref="ExporterSecret"/>
    /// using MLS-Export with the DAVE-specific label.
    ///
    /// RFC 9420 §8.5 — MLS-Exporter:
    ///   exporter_value = DeriveSecret(
    ///       ExpandWithLabel(exporter_secret, label, context, HashLen),
    ///       "exporter")
    /// </summary>
    public byte[] DeriveDaveEpochSecret()
    {
        // DAVE label defined in the DAVE protocol specification
        const string DaveLabel   = "DAVE sender";
        var exportedSecret = MlsHkdf.ExpandWithLabel(
            ExporterSecret,
            DaveLabel,
            ReadOnlySpan<byte>.Empty,
            MlsHkdf.HashLen);
        return MlsHkdf.DeriveSecret(exportedSecret, "exporter");
    }
}
