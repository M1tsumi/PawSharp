#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using PawSharp.Voice.DAVE;
using PawSharp.Voice.DAVE.MLS.Crypto;
using PawSharp.Voice.DAVE.MLS.Encoding;
using PawSharp.Voice.DAVE.MLS.Messages;
using PawSharp.Voice.DAVE.MLS.Tree;

namespace PawSharp.Voice.Tests;

/// <summary>
/// Generates synthetic but structurally valid MLS Welcome and Commit messages
/// for test purposes.  Uses the same crypto primitives (HPKE, HKDF, AES-GCM)
/// that the production code relies on, so the test data exercises the real
/// MLS code paths.
/// </summary>
internal static class DAVETestData
{
    /// <summary>
    /// Generates a valid Welcome message targeted at the given MLSState.
    /// The state must have a KeyPackage already generated (call
    /// <c>state.GenerateKeyPackage(…)</c> first).
    ///
    /// Returns the TLS-encoded Welcome bytes and the joiner_secret used
    /// (so tests can verify the derived epoch secret if needed).
    /// </summary>
    public static (byte[] welcomeBytes, byte[] joinerSecret) CreateWelcome(MLSState state)
    {
        var identity = new byte[] { 0x01 };
        var kpBytes = state.GenerateKeyPackage(identity);
        var kp = KeyPackage.Decode(kpBytes);

        var joinerSecret = new byte[32];
        RandomNumberGenerator.Fill(joinerSecret);

        var groupSecrets = new GroupSecrets(joinerSecret);
        var plaintext = groupSecrets.Encode();

        var encryptedSecrets = HpkeP256.SealBase(
            kp.InitKey,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            plaintext,
            out var enc);

        var kpRef = ComputeKeyPackageRef(kp);
        var entry = new EncryptedGroupSecrets(kpRef, new HpkeCiphertext(enc, encryptedSecrets));

        var welcomeSecret = MlsHkdf.DeriveSecret(joinerSecret, "welcome");
        var welcomeKey = MlsHkdf.ExpandWithLabel(welcomeSecret, "key", ReadOnlySpan<byte>.Empty, 16);
        var welcomeNonce = MlsHkdf.ExpandWithLabel(welcomeSecret, "nonce", ReadOnlySpan<byte>.Empty, 12);

        var groupId = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var treeHash = MlsHkdf.Hash(ReadOnlySpan<byte>.Empty);
        var confirmedTranscriptHash = MlsHkdf.Hash(ReadOnlySpan<byte>.Empty);
        var groupContext = new GroupContext(groupId, 1, treeHash, confirmedTranscriptHash);

        var confirmationTag = new byte[32];
        var signature = new byte[64];
        var groupInfo = new GroupInfo(groupContext, confirmationTag, 0, signature);
        var groupInfoBytes = groupInfo.Encode();

        using var aes = new AesGcm(welcomeKey, 16);
        var ciphertext = new byte[groupInfoBytes.Length];
        var tag = new byte[16];
        aes.Encrypt(welcomeNonce, groupInfoBytes, ciphertext, tag);
        var encryptedGroupInfo = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, encryptedGroupInfo, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, encryptedGroupInfo, ciphertext.Length, tag.Length);

        var welcome = new WelcomeMessage(
            CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256,
            new List<EncryptedGroupSecrets> { entry },
            encryptedGroupInfo);

        return (welcome.Encode(), joinerSecret);
    }

    /// <summary>
    /// Generates a single Welcome message with individual EncryptedGroupSecrets
    /// entries for each of the given MLS states.  All entries share the same
    /// joiner_secret, so every recipient derives the same epoch secret — this
    /// is how real MLS Welcome messages work.
    ///
    /// Returns the TLS-encoded Welcome bytes and the shared joiner_secret.
    /// </summary>
    public static (byte[] welcomeBytes, byte[] joinerSecret) CreateMultiWelcome(
        IReadOnlyList<MLSState> states)
    {
        var identity = new byte[] { 0x01 };
        var joinerSecret = new byte[32];
        RandomNumberGenerator.Fill(joinerSecret);

        var groupSecrets = new GroupSecrets(joinerSecret);
        var plaintext = groupSecrets.Encode();

        var entries = new List<EncryptedGroupSecrets>(states.Count);
        foreach (var state in states)
        {
            var kpBytes = state.GenerateKeyPackage(identity);
            var kp = KeyPackage.Decode(kpBytes);

            var encryptedSecrets = HpkeP256.SealBase(
                kp.InitKey,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                plaintext,
                out var enc);

            var kpRef = ComputeKeyPackageRef(kp);
            entries.Add(new EncryptedGroupSecrets(kpRef, new HpkeCiphertext(enc, encryptedSecrets)));
        }

        var welcomeSecret = MlsHkdf.DeriveSecret(joinerSecret, "welcome");
        var welcomeKey = MlsHkdf.ExpandWithLabel(welcomeSecret, "key", ReadOnlySpan<byte>.Empty, 16);
        var welcomeNonce = MlsHkdf.ExpandWithLabel(welcomeSecret, "nonce", ReadOnlySpan<byte>.Empty, 12);

        var groupId = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var treeHash = MlsHkdf.Hash(ReadOnlySpan<byte>.Empty);
        var confirmedTranscriptHash = MlsHkdf.Hash(ReadOnlySpan<byte>.Empty);
        var groupContext = new GroupContext(groupId, 1, treeHash, confirmedTranscriptHash);

        var confirmationTag = new byte[32];
        var signature = new byte[64];
        var groupInfo = new GroupInfo(groupContext, confirmationTag, 0, signature);
        var groupInfoBytes = groupInfo.Encode();

        using var aes = new AesGcm(welcomeKey, 16);
        var ciphertext = new byte[groupInfoBytes.Length];
        var tag = new byte[16];
        aes.Encrypt(welcomeNonce, groupInfoBytes, ciphertext, tag);
        var encryptedGroupInfo = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, encryptedGroupInfo, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, encryptedGroupInfo, ciphertext.Length, tag.Length);

        var welcome = new WelcomeMessage(
            CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256,
            entries,
            encryptedGroupInfo);

        return (welcome.Encode(), joinerSecret);
    }

    /// <summary>
    /// Creates a synthetic MLS Commit with no proposals and no UpdatePath.
    /// When processed, it triggers the HKDF rotation fallback path in
    /// <see cref="DAVE.MLS.State.MLSGroupState"/>, which is sufficient
    /// for testing epoch advancement and sender key invalidation.
    /// </summary>
    public static byte[] CreateEmptyCommit()
    {
        var commit = new Commit(Array.Empty<Proposal>(), null);
        return commit.Encode();
    }

    private static byte[] ComputeKeyPackageRef(KeyPackage kp)
    {
        var kpBytes = kp.Encode();
        using var w = new TlsWriter(kpBytes.Length + 20);
        w.WriteBytes("MLS 1.0 KeyPackage"u8);
        w.WriteBytes(kpBytes);
        return MlsHkdf.Hash(w.ToArray());
    }
}
