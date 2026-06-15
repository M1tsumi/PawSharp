using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Commands;
using PawSharp.Commands.Extensions;
using PawSharp.Core.Builders;
using PawSharp.Core.Models;
using PawSharp.Core.Enums;
using PawSharp.Voice;

namespace MusicBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Configure PawSharp
        var options = new PawSharpOptions
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? throw new InvalidOperationException("DISCORD_TOKEN env var required"),
            Intents = GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent
        };

        services.SetupPawSharp(options);
        services.AddCommands();
        services.AddSingleton<MusicService>();

        var serviceProvider = services.BuildServiceProvider();

        // Get the Discord client, music service, and logger
        var client = serviceProvider.GetRequiredService<IDiscordClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var musicService = serviceProvider.GetRequiredService<MusicService>();

        // Register commands
        var commandsExtension = client.UseCommands();
        var musicCommands = new MusicCommands(client, logger, musicService);
        commandsExtension.RegisterModule(client, musicCommands);

        // Connect
        try
        {
            logger.LogInformation("Starting Music Bot...");
            await client.ConnectAsync();
            logger.LogInformation("Bot connected successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect bot");
            throw;
        }

        // Keep the bot running
        await Task.Delay(-1);
    }
}

/// <summary>
/// Manages music playback state per guild
/// </summary>
public class MusicService
{
    private readonly IDiscordClient _client;
    private readonly ILogger _logger;
    private readonly Dictionary<ulong, GuildMusicPlayer> _players = new();

    public MusicService(IDiscordClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public GuildMusicPlayer GetOrCreatePlayer(ulong guildId)
    {
        if (!_players.TryGetValue(guildId, out var player))
        {
            player = new GuildMusicPlayer(guildId, _logger);
            _players[guildId] = player;
        }
        return player;
    }

    public bool TryGetPlayer(ulong guildId, out GuildMusicPlayer? player)
    {
        return _players.TryGetValue(guildId, out player);
    }
}

/// <summary>
/// Per-guild music player state
/// </summary>
public class GuildMusicPlayer
{
    public ulong GuildId { get; }
    public List<string> Queue { get; } = new();
    public string? CurrentTrack { get; set; }
    public int Volume { get; set; } = 100;
    public bool IsPlaying { get; set; }
    public bool IsPaused { get; set; }
    private readonly ILogger _logger;

    public GuildMusicPlayer(ulong guildId, ILogger logger)
    {
        GuildId = guildId;
        _logger = logger;
    }

    public void Enqueue(string track)
    {
        Queue.Add(track);
        _logger.LogInformation("Queued: {Track} (Queue size: {Count})", track, Queue.Count);
    }

    public string? Dequeue()
    {
        if (Queue.Count == 0)
            return null;
        
        var track = Queue[0];
        Queue.RemoveAt(0);
        return track;
    }

    public void ClearQueue()
    {
        Queue.Clear();
        _logger.LogInformation("Queue cleared for guild {GuildId}", GuildId);
    }

    public string GetQueueStatus()
    {
        if (Queue.Count == 0)
            return "Queue is empty";
        
        var status = $"Queue ({Queue.Count} songs):\n";
        for (int i = 0; i < Math.Min(Queue.Count, 5); i++)
        {
            status += $"{i + 1}. {Queue[i]}\n";
        }
        
        if (Queue.Count > 5)
            status += $"... and {Queue.Count - 5} more";
        
        return status;
    }
}

public class MusicCommands : BaseCommandModule
{
    private readonly MusicService _musicService;
    private readonly IDiscordClient _client;
    private readonly ILogger _logger;

    public MusicCommands(IDiscordClient client, ILogger logger, MusicService musicService)
    {
        _client = client;
        _logger = logger;
        _musicService = musicService;
    }

    private GuildMusicPlayer GetPlayer() 
        => _musicService.GetOrCreatePlayer(Context.GuildId ?? 0);

    [Command("play")]
    [Description("Play music from a URL or search query")]
    public async Task PlayAsync([Description("The URL or search query")] string query)
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        var player = GetPlayer();
        player.Enqueue(query);

        if (!player.IsPlaying)
        {
            player.CurrentTrack = player.Dequeue();
            player.IsPlaying = true;
            await ReplyAsync($"🎵 Now playing: **{player.CurrentTrack}**");
            _logger.LogInformation("Now playing: {Track} in guild {GuildId}", player.CurrentTrack, Context.GuildId);
        }
        else
        {
            await ReplyAsync($"✅ Added to queue: **{query}** (Position: {player.Queue.Count})");
        }
    }

    [Command("stop")]
    [Description("Stop the current music playback and clear the queue")]
    public async Task StopAsync()
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        var player = GetPlayer();
        player.IsPlaying = false;
        player.IsPaused = false;
        player.CurrentTrack = null;
        player.ClearQueue();
        
        await ReplyAsync("⏹️ Stopped playback and cleared queue");
    }

    [Command("pause")]
    [Description("Pause the current music playback")]
    public async Task PauseAsync()
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        var player = GetPlayer();
        if (!player.IsPlaying)
        {
            await ReplyAsync("❌ Nothing is currently playing");
            return;
        }

        player.IsPaused = true;
        await ReplyAsync("⏸️ Paused playback");
    }

    [Command("resume")]
    [Description("Resume the paused music playback")]
    public async Task ResumeAsync()
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        var player = GetPlayer();
        if (!player.IsPaused)
        {
            await ReplyAsync("❌ Music is not paused");
            return;
        }

        player.IsPaused = false;
        await ReplyAsync("▶️ Resumed playback");
    }

    [Command("skip")]
    [Description("Skip the current track and play the next one")]
    public async Task SkipAsync()
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        var player = GetPlayer();
        if (!player.IsPlaying)
        {
            await ReplyAsync("❌ Nothing is currently playing");
            return;
        }

        var skipped = player.CurrentTrack;
        player.CurrentTrack = player.Dequeue();
        
        if (player.CurrentTrack != null)
        {
            await ReplyAsync($"⏭️ Skipped: **{skipped}**\n🎵 Now playing: **{player.CurrentTrack}**");
        }
        else
        {
            player.IsPlaying = false;
            await ReplyAsync($"⏭️ Skipped: **{skipped}**\n✅ Queue is now empty");
        }
    }

    [Command("queue")]
    [Description("Show the current music queue")]
    public async Task QueueAsync()
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        var player = GetPlayer();
        var queueStatus = player.GetQueueStatus();
        
        var currentStatus = player.IsPlaying 
            ? $"**Now Playing:** {player.CurrentTrack} {(player.IsPaused ? "(⏸️ Paused)" : "")}\n\n"
            : "**Not Playing**\n\n";

        var embed = new EmbedBuilder()
            .WithTitle("🎵 Music Queue")
            .WithDescription(currentStatus + queueStatus)
            .WithColor(0x9B59B6)
            .WithFooter($"Volume: {player.Volume}%")
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("volume")]
    [Alias("vol")]
    [Description("Set the playback volume (0-100)")]
    public async Task VolumeAsync([Description("Volume level")] int level)
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        if (level < 0 || level > 100)
        {
            await ReplyAsync("❌ Volume must be between 0 and 100");
            return;
        }

        var player = GetPlayer();
        player.Volume = level;
        
        await ReplyAsync($"🔊 Volume set to **{level}%**");
    }

    [Command("nowplaying")]
    [Alias("np")]
    [Description("Show currently playing song")]
    public async Task NowPlayingAsync()
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        var player = GetPlayer();
        
        if (!player.IsPlaying || player.CurrentTrack == null)
        {
            await ReplyAsync("❌ Nothing is currently playing");
            return;
        }

        var statusText = player.IsPaused ? "⏸️ Paused" : "🎵 Playing";
        
        var embed = new EmbedBuilder()
            .WithTitle(statusText)
            .WithDescription($"**{player.CurrentTrack}**")
            .WithColor(0x2ECC71)
            .AddField("Queue Position", $"1 of {player.Queue.Count + 1}", inline: true)
            .AddField("Volume", $"{player.Volume}%", inline: true)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("clear")]
    [Description("Clear the music queue")]
    public async Task ClearAsync()
    {
        if (Context.GuildId == null)
        {
            await ReplyAsync("❌ This command can only be used in a guild");
            return;
        }

        var player = GetPlayer();
        player.ClearQueue();
        
        await ReplyAsync("🗑️ Queue cleared");
    }
}