#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Limits how often a command can be executed by tracking invocations against a rolling window.
/// </summary>
/// <remarks>
/// A bucket key is derived from the invoking user, channel, or guild depending on
/// <see cref="BucketType"/>.  Each bucket is reset once the <see cref="Per"/>
/// window has elapsed since the <em>first</em> use in that window.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class CooldownAttribute : Attribute, IPrecondition
{
    /// <summary>
    /// Maximum number of uses allowed within the time <see cref="Per"/>.
    /// </summary>
    public int MaxUses { get; }

    /// <summary>
    /// The rolling window over which <see cref="MaxUses"/> applies.
    /// </summary>
    public TimeSpan Per { get; }

    /// <summary>
    /// Determines how invocations are bucketed (per user, per channel, per guild, or globally).
    /// </summary>
    public CooldownBucketType BucketType { get; }

    private readonly ConcurrentDictionary<string, BucketState> _buckets = new();
    private readonly object _cleanupLock = new();
    private DateTimeOffset _lastCleanup = DateTimeOffset.UtcNow;
    private const int CleanupIntervalSeconds = 300; // Clean up every 5 minutes

    /// <summary>
    /// Initialises the attribute.
    /// </summary>
    /// <param name="maxUses">Maximum uses within the window.</param>
    /// <param name="perSeconds">Length of the rolling window in seconds.</param>
    /// <param name="bucketType">Scope of the cooldown bucket.</param>
    public CooldownAttribute(int maxUses, double perSeconds, CooldownBucketType bucketType = CooldownBucketType.User)
    {
        if (maxUses   < 1)                        throw new ArgumentOutOfRangeException(nameof(maxUses));
        if (perSeconds <= 0)                      throw new ArgumentOutOfRangeException(nameof(perSeconds));

        MaxUses    = maxUses;
        Per        = TimeSpan.FromSeconds(perSeconds);
        BucketType = bucketType;
    }

    /// <inheritdoc/>
    public Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        var key    = GetBucketKey(ctx);
        var now    = DateTimeOffset.UtcNow;
        var bucket = _buckets.GetOrAdd(key, _ => new BucketState(now));

        try
        {
            lock (bucket)
            {
                // Reset the bucket if the window has expired
                if (now - bucket.WindowStart >= Per)
                {
                    bucket.WindowStart     = now;
                    bucket.InvocationCount = 0;
                }

                if (bucket.InvocationCount < MaxUses)
                {
                    bucket.InvocationCount++;
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }

                var remaining = Per - (now - bucket.WindowStart);
                return Task.FromResult(PreconditionResult.FromError(
                    $"You are on cooldown. Try again in {remaining.TotalSeconds:F1} second(s)."));
            }
        }
        finally
        {
            // Periodically clean up expired buckets to prevent memory leaks
            if (now - _lastCleanup >= TimeSpan.FromSeconds(CleanupIntervalSeconds))
            {
                CleanupExpiredBuckets(now);
            }
        }
    }

    private void CleanupExpiredBuckets(DateTimeOffset now)
    {
        // Use a lock to prevent multiple concurrent cleanups
        if (!Monitor.TryEnter(_cleanupLock))
            return;

        try
        {
            // Double-check after acquiring lock
            if (now - _lastCleanup < TimeSpan.FromSeconds(CleanupIntervalSeconds))
                return;

            var expiredKeys = new List<string>();
            foreach (var kvp in _buckets)
            {
                lock (kvp.Value)
                {
                    // Remove buckets that haven't been used for 3x the cooldown period
                    if (now - kvp.Value.WindowStart > Per * 3)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in expiredKeys)
            {
                _buckets.TryRemove(key, out _);
            }

            _lastCleanup = now;
        }
        finally
        {
            Monitor.Exit(_cleanupLock);
        }
    }

    private string GetBucketKey(CommandContext ctx) => BucketType switch
    {
        CooldownBucketType.User    => $"u:{ctx.User.Id}",
        CooldownBucketType.Channel => $"c:{ctx.ChannelId}",
        CooldownBucketType.Guild   => $"g:{ctx.GuildId?.ToString() ?? "dm"}",
        _                          => "global"
    };

    private sealed class BucketState
    {
        internal DateTimeOffset WindowStart;
        internal int            InvocationCount;

        internal BucketState(DateTimeOffset windowStart) => WindowStart = windowStart;
    }
}

/// <summary>
/// Determines how a <see cref="CooldownAttribute"/> bucket is keyed.
/// </summary>
public enum CooldownBucketType
{
    /// <summary>One bucket per invoking user across all channels.</summary>
    User,

    /// <summary>One bucket per channel (shared across all users).</summary>
    Channel,

    /// <summary>One bucket per guild (shared across all users and channels).</summary>
    Guild,

    /// <summary>A single global bucket across all users, channels, and guilds.</summary>
    Global,
}
