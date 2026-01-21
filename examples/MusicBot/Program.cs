using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Commands;
using PawSharp.Core.Models;
using PawSharp.Interactivity;

namespace MusicBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(config => config.AddConsole());

        // Configure PawSharp
        var options = new PawSharpOptions
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? "your-bot-token-here",
            Intents = PawSharp.Core.Enums.GatewayIntents.AllNonPrivileged
        };

        services.AddSingleton(options);
        services.AddPawSharpClient();
        services.AddPawSharpCommands();
        services.AddPawSharpInteractivity();

        var serviceProvider = services.BuildServiceProvider();

        // Get the Discord client
        var client = serviceProvider.GetRequiredService<DiscordClient>();

        // Register commands
        var commandService = serviceProvider.GetRequiredService<CommandService>();
        await commandService.AddModuleAsync<MusicCommands>();

        // Connect and start
        await client.ConnectAsync();

        // Keep the bot running
        await Task.Delay(-1);
    }
}

[CommandModule("music")]
public class MusicCommands : CommandModule
{
    private readonly InteractivityService _interactivity;

    public MusicCommands(InteractivityService interactivity)
    {
        _interactivity = interactivity;
    }

    [Command("play")]
    [Description("Play music from a URL or search query")]
    public async Task PlayAsync(
        [Description("The URL or search query")] string query)
    {
        await ReplyAsync($"🎵 Playing: {query}");
        // Note: Actual music playback would require voice implementation
        // This is a placeholder for the concept
    }

    [Command("stop")]
    [Description("Stop the current music playback")]
    public async Task StopAsync()
    {
        await ReplyAsync("⏹️ Stopped playback");
    }

    [Command("queue")]
    [Description("Show the current music queue")]
    public async Task QueueAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Music Queue")
            .WithDescription("No songs in queue")
            .WithColor(Color.Blue);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("volume")]
    [Description("Set the playback volume (0-100)")]
    public async Task VolumeAsync(
        [Description("Volume level")] int level)
    {
        if (level < 0 || level > 100)
        {
            await ReplyAsync("❌ Volume must be between 0 and 100");
            return;
        }

        await ReplyAsync($"🔊 Volume set to {level}%");
    }

    [Command("nowplaying")]
    [Alias("np")]
    [Description("Show currently playing song")]
    public async Task NowPlayingAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Now Playing")
            .WithDescription("Nothing is currently playing")
            .WithColor(Color.Green);

        await ReplyAsync(embed: embed.Build());
    }
}