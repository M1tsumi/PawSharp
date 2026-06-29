// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

/// <summary>
/// Factory for creating crypto provider instances.
/// Uses BouncyCastle by default for optimal performance and security.
/// </summary>
internal static class CryptoProviderFactory
{
    private static ICryptoProvider? _instance;

    /// <summary>
    /// Gets the singleton crypto provider instance.
    /// Thread-safe lazy initialization.
    /// </summary>
    public static ICryptoProvider Instance
    {
        get
        {
            if (_instance != null) return _instance;
            
            lock (typeof(CryptoProviderFactory))
            {
                _instance ??= CreateProvider();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Creates the appropriate crypto provider.
    /// Currently uses BouncyCastle for all operations.
    /// </summary>
    private static ICryptoProvider CreateProvider()
    {
        // BouncyCastle provides the best balance of:
        // - Performance (optimized C# assembly for P-256 ECDH/ECDSA)
        // - Security (audited codebase)
        // - Portability (pure C#, no native dependencies)
        return new BouncyCastleCryptoProvider();
    }

    /// <summary>
    /// Resets the provider instance (primarily for testing).
    /// </summary>
    internal static void Reset()
    {
        lock (typeof(CryptoProviderFactory))
        {
            _instance = null;
        }
    }
}
