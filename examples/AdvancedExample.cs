using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.API.Clients;
using PawSharp.API.Models;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Core.Logging;
using PawSharp.Core.Metrics;
using PawSharp.Core.Models;

namespace PawSharp.Examples;

/// <summary>
/// Comprehensive example demonstrating PawSharp features including:
/// - Bot initialization and configuration
/// - Event handling (messages, joins, presence)
/// - Command processing
/// - Caching
/// - Error handling
/// - Performance monitoring
/// </summary>
public class AdvancedExampleBot
{
    private DiscordClient _client;
    private readonly ILogger<AdvancedExampleBot> _logger;
    private readonly PerformanceMetrics _metrics;
    private readonly MemoryMetrics _memoryMetrics;
    private readonly Dictionary<string, Func<Message, Task>> _commands;
    private bool _isRunning;

    public AdvancedExampleBot()
    {
        _metrics = new PerformanceMetrics();
        _memoryMetrics = new MemoryMetrics();
        _commands = new Dictionary<string, Func<Message, Task>>(StringComparer.OrdinalIgnoreCase);
        _logger = null!; // Will be set in Initialize
    }

    /// <summary>
    /// Initialize the bot with all required services.
    /// </summary>
    public async Task InitializeAsync(string botToken)
    {
        // Configure dependency injection
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.AddPawSharpLogging(LogLevel.Information);
            builder.AddConsole();
        });

        var options = new PawSharpOptions
        {
            Token = botToken,
            ApiVersion = 10
        };

        services.AddSingleton(options);
        services.AddSingleton<IEntityCache, EntityCache>();
        services.AddSingleton(_metrics);
        services.AddSingleton(_memoryMetrics);
        services.AddHttpClient<IDiscordRestClient, DiscordRestClient>();

        var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<AdvancedExampleBot>();

        var httpClient = provider.GetRequiredService<HttpClient>();
        var cache = provider.GetRequiredService<IEntityCache>();
        var restClient = provider.GetRequiredService<IDiscordRestClient>();

        _client = new DiscordClient(options, cache, 
            loggerFactory.CreateLogger<DiscordClient>(), restClient);

        // Register event handlers
        RegisterEventHandlers();
        
        // Register commands
        RegisterCommands();

        _logger.LogInformation("Bot initialized successfully");
    }

    /// <summary>
    /// Connect to Discord and start the bot.
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            _logger.LogInformation("Connecting to Discord...");
            await _client.ConnectAsync();
            _isRunning = true;
            
            _logger.LogInformation("Bot is running. Press Ctrl+C to stop.");
            
            // Keep bot running
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error");
            throw;
        }
    }

    /// <summary>
    /// Register gateway event handlers.
    /// </summary>
    private void RegisterEventHandlers()
    {
        // Bot is ready
        _client.Gateway.OnReady += async (ready) =>
        {
            _logger.LogInformation("Bot logged in as {Username}#{Discriminator}", 
                ready.User.Username, ready.User.Discriminator ?? "0000");
            
            _logger.LogInformation("Connected to {GuildCount} guilds", 
                ready.Guilds?.Count ?? 0);
        };

        // Message received
        _client.Gateway.OnMessageCreate += async (message) =>
        {
            try
            {
                // Record metric
                _metrics.RecordGatewayMessage("MESSAGE_CREATE");

                // Ignore bot's own messages
                if (message.Author?.Id == _client.Cache.GetUser(message.Author.Id)?.Id)
                    return;

                _logger.LogInformation("Message from {Author} in {Channel}: {Content}",
                    message.Author?.Username, message.ChannelId, message.Content);

                // Process commands if message starts with !
                if (message.Content?.StartsWith("!") ?? false)
                {
                    await ProcessCommandAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message");
            }
        };

        // Member joined
        _client.Gateway.OnGuildMemberAdd += async (member) =>
        {
            try
            {
                _metrics.RecordGatewayMessage("GUILD_MEMBER_ADD");
                
                _logger.LogInformation("Member joined: {Username}", member.User?.Username);
                
                // Send welcome message to system channel if available
                if (member.Guild?.SystemChannelId.HasValue ?? false)
                {
                    var channelId = member.Guild.SystemChannelId.Value;
                    var message = $"Welcome {member.User?.Username}!";
                    
                    await _client.Rest.CreateMessageAsync(channelId,
                        new CreateMessageRequest { Content = message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to handle member join");
            }
        };

        // Member left
        _client.Gateway.OnGuildMemberRemove += async (user) =>
        {
            _logger.LogInformation("Member left: {Username}", user.Username);
            _metrics.RecordGatewayMessage("GUILD_MEMBER_REMOVE");
        };

        // Presence updated
        _client.Gateway.OnPresenceUpdate += async (presence) =>
        {
            _metrics.RecordGatewayMessage("PRESENCE_UPDATE");
            
            if (presence.Status != null)
            {
                _logger.LogDebug("{Username} is now {Status}",
                    presence.User?.Username, presence.Status);
            }
        };

        // Reconnecting
        _client.Gateway.OnReconnecting += async (attempt, maxAttempts) =>
        {
            _logger.LogWarning("Reconnecting... Attempt {Attempt}/{MaxAttempts}",
                attempt, maxAttempts);
        };

        // Reconnected
        _client.Gateway.OnReconnected += async () =>
        {
            _logger.LogInformation("✓ Reconnected to Discord");
        };
    }

    /// <summary>
    /// Register bot commands.
    /// </summary>
    private void RegisterCommands()
    {
        // !ping - Check bot latency
        _commands["ping"] = async (msg) =>
        {
            await _client.Rest.CreateMessageAsync(msg.ChannelId,
                new CreateMessageRequest { Content = "Pong!" });
        };

        // !help - Show available commands
        _commands["help"] = async (msg) =>
        {
            var commands = string.Join("\n", _commands.Keys.Select(k => $"  !{k}"));
            var response = $"Available commands:\n{commands}";
            
            await _client.Rest.CreateMessageAsync(msg.ChannelId,
                new CreateMessageRequest { Content = response });
        };

        // !stats - Show bot statistics
        _commands["stats"] = async (msg) =>
        {
            var metrics = _metrics.GetSummary();
            var memory = _memoryMetrics.GetSummary();
            
            var stats = $@"Bot Statistics
API Requests: {metrics.TotalApiRequests}
Avg Response: {metrics.AverageApiDurationMs}ms
Error Rate: {metrics.ApiErrorRate:F2}%
Cache Hit Rate: {metrics.CacheHitRate:F2}%
Memory: {memory.CurrentMemoryMB:F2}MB
Peak Memory: {memory.PeakMemoryMB:F2}MB
Threads: {memory.Threads}
Uptime: {memory.UptimeSeconds}s";
            
            await _client.Rest.CreateMessageAsync(msg.ChannelId,
                new CreateMessageRequest { Content = stats });
        };

        // !user <id> - Get user info
        _commands["user"] = async (msg) =>
        {
            var args = msg.Content?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args?.Length < 2 || !ulong.TryParse(args[1], out var userId))
            {
                await _client.Rest.CreateMessageAsync(msg.ChannelId,
                    new CreateMessageRequest { Content = "Usage: !user <user_id>" });
                return;
            }

            try
            {
                // Check cache first
                var user = _client.Cache.GetUser(userId);
                user ??= await _client.Rest.GetUserAsync(userId);

                if (user != null)
                {
                    var userInfo = $@"User Info
Username: {user.Username}#{user.Discriminator}
ID: {user.Id}
Bot: {user.Bot}
Created: {user.CreatedAt:g}";
                    
                    await _client.Rest.CreateMessageAsync(msg.ChannelId,
                        new CreateMessageRequest { Content = userInfo });
                }
            }
            catch (PawSharp.Core.Exceptions.DiscordApiException ex) when (ex.StatusCode == 404)
            {
                await _client.Rest.CreateMessageAsync(msg.ChannelId,
                    new CreateMessageRequest { Content = "User not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user");
                await _client.Rest.CreateMessageAsync(msg.ChannelId,
                    new CreateMessageRequest { Content = "Error fetching user." });
            }
        };

        // !echo <text> - Echo a message
        _commands["echo"] = async (msg) =>
        {
            var text = msg.Content?[5..].Trim(); // Skip "!echo"
            if (string.IsNullOrWhiteSpace(text))
            {
                await _client.Rest.CreateMessageAsync(msg.ChannelId,
                    new CreateMessageRequest { Content = "Usage: !echo <text>" });
                return;
            }

            try
            {
                await _client.Rest.CreateMessageAsync(msg.ChannelId,
                    new CreateMessageRequest { Content = text });
            }
            catch (PawSharp.Core.Exceptions.ValidationException ex)
            {
                _logger.LogWarning("Message too long");
                await _client.Rest.CreateMessageAsync(msg.ChannelId,
                    new CreateMessageRequest { Content = "Message is too long (max 2000 chars)." });
            }
        };

        _logger.LogInformation("Registered {CommandCount} commands", _commands.Count);
    }

    /// <summary>
    /// Process a command from a message.
    /// </summary>
    private async Task ProcessCommandAsync(Message message)
    {
        try
        {
            var parts = message.Content?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts?.Length == 0) return;

            var command = parts[0][1..].ToLower(); // Remove ! prefix

            if (_commands.TryGetValue(command, out var handler))
            {
                _logger.LogInformation("Executing command: {Command}", command);
                await handler(message);
            }
            else
            {
                await _client.Rest.CreateMessageAsync(message.ChannelId,
                    new CreateMessageRequest { Content = $"Unknown command: !{command}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command");
        }
    }

    /// <summary>
    /// Stop the bot gracefully.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _logger.LogInformation("Shutting down...");
        
        // Log final metrics
        var metrics = _metrics.GetSummary();
        _logger.LogInformation(
            "Final stats - Requests: {Requests}, Avg: {Avg}ms, Cache: {Cache:F2}%",
            metrics.TotalApiRequests,
            metrics.AverageApiDurationMs,
            metrics.CacheHitRate);

        _isRunning = false;
    }

    /// <summary>
    /// Main entry point.
    /// </summary>
    public static async Task Main(string[] args)
    {
        var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Error: DISCORD_BOT_TOKEN environment variable not set");
            return;
        }

        var bot = new AdvancedExampleBot();
        await bot.InitializeAsync(token);
        
        // Handle graceful shutdown
        Console.CancelKeyPress += async (s, e) =>
        {
            e.Cancel = true;
            await bot.StopAsync();
        };

        await bot.RunAsync();
    }
}
