#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.Core.Models;

namespace PawSharp.Gateway;

/// <summary>
/// Manages shard rebalancing for optimizing guild distribution across shards.
/// Monitors shard load and can suggest or perform rebalancing operations.
/// </summary>
public class ShardRebalancingManager
{
    private readonly ShardManager _shardManager;
    private readonly PawSharpOptions _options;
    private readonly ILogger? _logger;
    private readonly Dictionary<int, ShardLoadMetrics> _shardMetrics = new();
    private readonly object _lock = new();

    /// <summary>
    /// Threshold percentage difference between shards to trigger rebalancing consideration.
    /// </summary>
    public double ImbalanceThreshold { get; set; } = 0.3; // 30%

    /// <summary>
    /// Minimum guild count difference to consider rebalancing.
    /// </summary>
    public int MinGuildDifference { get; set; } = 100;

    /// <summary>
    /// Creates a new shard rebalancing manager.
    /// </summary>
    public ShardRebalancingManager(ShardManager shardManager, PawSharpOptions options, ILogger? logger = null)
    {
        _shardManager = shardManager;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Records guild count for a shard.
    /// </summary>
    public void RecordShardGuildCount(int shardId, int guildCount)
    {
        lock (_lock)
        {
            if (!_shardMetrics.TryGetValue(shardId, out var metrics))
            {
                metrics = new ShardLoadMetrics { ShardId = shardId };
                _shardMetrics[shardId] = metrics;
            }
            metrics.GuildCount = guildCount;
            metrics.LastUpdated = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Analyzes current shard distribution and returns rebalancing recommendations.
    /// </summary>
    public RebalancingAnalysis AnalyzeDistribution()
    {
        lock (_lock)
        {
            if (_shardMetrics.Count == 0)
            {
                return new RebalancingAnalysis 
                { 
                    IsBalanced = true, 
                    Recommendation = "No metrics available. Collect data first." 
                };
            }

            var shardList = _shardMetrics.Values.ToList();
            var avgGuilds = shardList.Average(s => s.GuildCount);
            var maxGuilds = shardList.Max(s => s.GuildCount);
            var minGuilds = shardList.Min(s => s.GuildCount);
            var diff = maxGuilds - minGuilds;
            var imbalanceRatio = avgGuilds > 0 ? (maxGuilds - minGuilds) / avgGuilds : 0;

            var analysis = new RebalancingAnalysis
            {
                Timestamp = DateTimeOffset.UtcNow,
                ShardCount = shardList.Count,
                TotalGuilds = shardList.Sum(s => s.GuildCount),
                AverageGuildsPerShard = avgGuilds,
                MaxGuildsOnShard = maxGuilds,
                MinGuildsOnShard = minGuilds,
                ImbalanceRatio = imbalanceRatio,
                ShardMetrics = shardList.ToDictionary(s => s.ShardId, s => s),
                IsBalanced = diff < MinGuildDifference || imbalanceRatio < ImbalanceThreshold
            };

            if (!analysis.IsBalanced)
            {
                analysis.Recommendation = GenerateRecommendation(shardList, avgGuilds);
                analysis.SuggestedMoves = CalculateSuggestedMoves(shardList, avgGuilds);
            }
            else
            {
                analysis.Recommendation = "Shard distribution is balanced.";
            }

            return analysis;
        }
    }

    /// <summary>
    /// Checks if rebalancing is recommended based on current metrics.
    /// </summary>
    public bool IsRebalancingRecommended()
    {
        var analysis = AnalyzeDistribution();
        return !analysis.IsBalanced;
    }

    /// <summary>
    /// Generates a recommendation string for rebalancing.
    /// </summary>
    private string GenerateRecommendation(List<ShardLoadMetrics> shards, double targetGuilds)
    {
        var overloaded = shards.Where(s => s.GuildCount > targetGuilds * (1 + ImbalanceThreshold / 2)).ToList();
        var underloaded = shards.Where(s => s.GuildCount < targetGuilds * (1 - ImbalanceThreshold / 2)).ToList();

        return $"Rebalancing recommended: {overloaded.Count} shards overloaded, {underloaded.Count} shards underloaded. " +
               $"Target: {targetGuilds:F0} guilds per shard.";
    }

    /// <summary>
    /// Calculates suggested guild moves for rebalancing.
    /// </summary>
    private List<SuggestedMove> CalculateSuggestedMoves(List<ShardLoadMetrics> shards, double targetGuilds)
    {
        var moves = new List<SuggestedMove>();
        var overloaded = shards.Where(s => s.GuildCount > targetGuilds).OrderByDescending(s => s.GuildCount).ToList();
        var underloaded = shards.Where(s => s.GuildCount < targetGuilds).OrderBy(s => s.GuildCount).ToList();

        foreach (var over in overloaded)
        {
            var excess = (int)(over.GuildCount - targetGuilds);
            if (excess < 10) continue; // Don't move small numbers

            foreach (var under in underloaded)
            {
                var capacity = (int)(targetGuilds - under.GuildCount);
                var toMove = Math.Min(excess, capacity);
                
                if (toMove >= 10)
                {
                    moves.Add(new SuggestedMove
                    {
                        FromShardId = over.ShardId,
                        ToShardId = under.ShardId,
                        EstimatedGuildCount = toMove,
                        Reason = $"Balance shard load (current: {over.GuildCount} -> {under.GuildCount})"
                    });

                    // Update counts for next iteration
                    over.GuildCount -= toMove;
                    under.GuildCount += toMove;
                    excess -= toMove;
                }

                if (excess < 10) break;
            }
        }

        return moves;
    }

    /// <summary>
    /// Logs current distribution analysis for monitoring.
    /// </summary>
    public void LogDistributionStatus()
    {
        var analysis = AnalyzeDistribution();
        
        _logger?.LogInformation(
            "Shard Distribution: {TotalGuilds} guilds across {ShardCount} shards, " +
            "avg {Avg:F0} per shard, imbalance: {Imbalance:P1}",
            analysis.TotalGuilds, analysis.ShardCount, analysis.AverageGuildsPerShard, analysis.ImbalanceRatio);

        foreach (var shard in analysis.ShardMetrics.Values.OrderBy(s => s.ShardId))
        {
            var deviation = analysis.AverageGuildsPerShard > 0 
                ? (shard.GuildCount - analysis.AverageGuildsPerShard) / analysis.AverageGuildsPerShard 
                : 0;
            _logger?.LogDebug(
                "Shard {ShardId}: {GuildCount} guilds ({Deviation:+0.0%;-0.0%;0.0%})",
                shard.ShardId, shard.GuildCount, deviation);
        }

        if (!analysis.IsBalanced)
        {
            _logger?.LogWarning("{Recommendation}", analysis.Recommendation);
            foreach (var move in analysis.SuggestedMoves.Take(3))
            {
                _logger?.LogWarning("  Suggested: Move ~{Count} guilds from shard {From} to shard {To}",
                    move.EstimatedGuildCount, move.FromShardId, move.ToShardId);
            }
        }
    }

    /// <summary>
    /// Represents load metrics for a single shard.
    /// </summary>
    public class ShardLoadMetrics
    {
        public int ShardId { get; set; }
        public int GuildCount { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents a suggested guild move for rebalancing.
    /// </summary>
    public class SuggestedMove
    {
        public int FromShardId { get; set; }
        public int ToShardId { get; set; }
        public int EstimatedGuildCount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Analysis result for shard distribution.
    /// </summary>
    public class RebalancingAnalysis
    {
        public DateTimeOffset Timestamp { get; set; }
        public int ShardCount { get; set; }
        public int TotalGuilds { get; set; }
        public double AverageGuildsPerShard { get; set; }
        public int MaxGuildsOnShard { get; set; }
        public int MinGuildsOnShard { get; set; }
        public double ImbalanceRatio { get; set; }
        public bool IsBalanced { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public Dictionary<int, ShardLoadMetrics> ShardMetrics { get; set; } = new();
        public List<SuggestedMove> SuggestedMoves { get; set; } = new();
    }
}

/// <summary>
/// Extension methods for ShardManager to add rebalancing capabilities.
/// </summary>
public static class ShardRebalancingExtensions
{
    /// <summary>
    /// Creates a rebalancing manager for this shard manager.
    /// </summary>
    public static ShardRebalancingManager WithRebalancing(this ShardManager shardManager, PawSharpOptions options, ILogger? logger = null)
    {
        return new ShardRebalancingManager(shardManager, options, logger);
    }
}
