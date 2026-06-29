using PawSharp.Client;
using PawSharp.Gateway;
using PawSharp.Core.Entities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        // Bot configuration
        string token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
            ?? throw new InvalidOperationException("DISCORD_TOKEN environment variable is required");

        // Create Discord client
        var client = new DiscordClient(token, loggerFactory) as IDiscordClient ?? throw new InvalidOperationException("Client is not an IDiscordClient");

        // Initialize moderation system
        var moderationSystem = new ModerationSystem(client, logger);

        // Subscribe to events
        client.Gateway.OnMessageCreate += moderationSystem.HandleMessageAsync;
        client.Gateway.OnGuildMemberAdd += moderationSystem.HandleMemberJoinAsync;

        logger.LogInformation("Starting Moderation Bot...");

        try
        {
            await client.ConnectAsync();
            logger.LogInformation("Bot connected successfully!");

            // Keep the bot running
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start bot");
            throw;
        }
    }
}

public class ModerationSystem
{
    private readonly IDiscordClient _client;
    private readonly ILogger _logger;
    private readonly HashSet<ulong> _mutedUsers = new();
    private readonly Dictionary<ulong, List<string>> _userWarnings = new();
    private readonly Dictionary<ulong, Queue<long>> _userMessageTimestamps = new(); // For rate limiting

    // Configurable settings
    private readonly string[] _bannedWords = { "spam", "inappropriate", "offensive" };
    private readonly int _maxWarnings = 3;
    private readonly TimeSpan _muteDuration = TimeSpan.FromMinutes(10);
    
    // Spam detection settings
    private readonly int _maxMessagesPerWindow = 5;           // Max messages allowed in a time window
    private readonly TimeSpan _messageTimeWindow = TimeSpan.FromSeconds(10); // Time window for rate limiting
    private readonly int _maxMentions = 10;                    // Max mentions in a single message
    private readonly double _spamCharacterThreshold = 0.5;     // % of repeated chars considered spam

    public ModerationSystem(IDiscordClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task HandleMessageAsync(Message message)
    {
        try
        {
            // Ignore bot messages
            if (message.Author?.IsBot == true) return;

            // Check for banned words
            if (ContainsBannedWords(message.Content))
            {
                await HandleViolationAsync(message, "Banned word usage");
                return;
            }

            // Check for spam
            if (IsSpam(message))
            {
                await HandleViolationAsync(message, "Spam detected");
                return;
            }

            // Handle commands
            if (message.Content?.StartsWith("!mod ") == true)
            {
                await HandleModerationCommandAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message {MessageId}", message.Id);
        }
    }

    public async Task HandleMemberJoinAsync(GuildMember member)
    {
        try
        {
            _logger.LogInformation("New member joined: {Username}#{Discriminator} ({UserId})",
                member.User?.Username, member.User?.Discriminator, member.User?.Id);

            // Welcome message
            var welcomeChannel = await GetWelcomeChannelAsync(member.GuildId);
            if (welcomeChannel != null)
            {
                await _client.Rest.CreateMessageAsync(welcomeChannel.Id,
                    $"Welcome {member.User?.Mention} to the server! Please read the rules.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling member join for user {UserId}", member.User?.Id);
        }
    }

    private async Task HandleModerationCommandAsync(Message message)
    {
        if (message.Content == null) return;

        var parts = message.Content.Split(' ', 3);
        if (parts.Length < 2) return;

        var command = parts[1].ToLower();
        var args = parts.Length > 2 ? parts[2] : "";

        // Check if user has moderator permissions (simplified check)
        if (!await HasModeratorPermissionsAsync(message.Author!.Id, message.GuildId))
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, "❌ You don't have permission to use moderation commands.");
            return;
        }

        switch (command)
        {
            case "warn":
                await WarnUserAsync(message, args);
                break;
            case "mute":
                await MuteUserAsync(message, args);
                break;
            case "kick":
                await KickUserAsync(message, args);
                break;
            case "ban":
                await BanUserAsync(message, args);
                break;
            case "warnings":
                await ShowWarningsAsync(message, args);
                break;
            default:
                await _client.Rest.CreateMessageAsync(message.ChannelId, "Unknown moderation command. Available: warn, mute, kick, ban, warnings");
                break;
        }
    }

    private async Task WarnUserAsync(Message message, string args)
    {
        var userId = ParseUserId(args);
        if (userId == 0)
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, "❌ Invalid user mention or ID.");
            return;
        }

        var warnings = _userWarnings.GetOrAdd(userId, _ => new List<string>());
        warnings.Add($"Warning by {message.Author?.Username}: {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}");

        if (warnings.Count >= _maxWarnings)
        {
            // Auto-ban for too many warnings
            await BanUserByIdAsync(message.GuildId, userId, "Too many warnings");
            await _client.Rest.CreateMessageAsync(message.ChannelId, $"🚫 User <@{userId}> has been banned for reaching {_maxWarnings} warnings.");
        }
        else
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, $"⚠️ User <@{userId}> has been warned. Total warnings: {warnings.Count}/{_maxWarnings}");
        }
    }

    private async Task MuteUserAsync(Message message, string args)
    {
        var userId = ParseUserId(args);
        if (userId == 0)
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, "❌ Invalid user mention or ID.");
            return;
        }

        _mutedUsers.Add(userId);

        // In a real implementation, you'd modify the user's roles or use Discord's timeout feature
        await _client.Rest.CreateMessageAsync(message.ChannelId, $"🔇 User <@{userId}> has been muted for {_muteDuration.TotalMinutes} minutes.");

        // Schedule unmute
        _ = Task.Delay(_muteDuration).ContinueWith(_ =>
        {
            _mutedUsers.Remove(userId);
            _logger.LogInformation("User {UserId} unmuted", userId);
        });
    }

    private async Task KickUserAsync(Message message, string args)
    {
        var userId = ParseUserId(args);
        if (userId == 0)
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, "❌ Invalid user mention or ID.");
            return;
        }

        try
        {
            await _client.Rest.RemoveGuildMemberAsync(message.GuildId, userId, "Kicked by moderator");
            await _client.Rest.CreateMessageAsync(message.ChannelId, $"👢 User <@{userId}> has been kicked.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to kick user {UserId}", userId);
            await _client.Rest.CreateMessageAsync(message.ChannelId, "❌ Failed to kick user. They may not be in the server or I lack permissions.");
        }
    }

    private async Task BanUserAsync(Message message, string args)
    {
        var userId = ParseUserId(args);
        if (userId == 0)
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, "❌ Invalid user mention or ID.");
            return;
        }

        if (await TryBanUserByIdAsync(message.GuildId, userId, "Banned by moderator"))
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, $"🚫 User <@{userId}> has been banned.");
        }
        else
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, "❌ Failed to ban user. They may not be in the server or I lack permissions.");
        }
    }

    private async Task<bool> TryBanUserByIdAsync(ulong guildId, ulong userId, string reason)
    {
        try
        {
            await _client.Rest.CreateGuildBanAsync(guildId, userId, reason: reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ban user {UserId}", userId);
            return false;
        }
    }

    private async Task ShowWarningsAsync(Message message, string args)
    {
        var userId = ParseUserId(args);
        if (userId == 0)
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, "❌ Invalid user mention or ID.");
            return;
        }

        if (_userWarnings.TryGetValue(userId, out var warnings))
        {
            var warningList = string.Join("\n", warnings.Select((w, i) => $"{i + 1}. {w}"));
            await _client.Rest.CreateMessageAsync(message.ChannelId, $"Warnings for <@{userId}>:\n{warningList}");
        }
        else
        {
            await _client.Rest.CreateMessageAsync(message.ChannelId, $"User <@{userId}> has no warnings.");
        }
    }

    private async Task HandleViolationAsync(Message message, string reason)
    {
        _logger.LogWarning("Violation detected: {Reason} by user {UserId} in channel {ChannelId}",
            reason, message.Author?.Id, message.ChannelId);

        // Delete the message
        try
        {
            await _client.Rest.DeleteMessageAsync(message.ChannelId, message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId}", message.Id);
        }

        // Send warning
        await _client.Rest.CreateMessageAsync(message.ChannelId,
            $"{message.Author?.Mention} Your message was removed for: {reason}");

        // Add warning
        await WarnUserAsync(message, $"<@{message.Author!.Id}>");
    }

    private bool ContainsBannedWords(string? content)
    {
        if (string.IsNullOrEmpty(content)) return false;

        return _bannedWords.Any(word =>
            Regex.IsMatch(content, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase));
    }

    private bool IsSpam(Message message)
    {
        if (message.Author?.Id == null) return false;

        var userId = message.Author.Id.Value;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Check 1: Rate limiting (too many messages in a short time)
        if (_userMessageTimestamps.TryGetValue(userId, out var timestamps))
        {
            // Remove old timestamps outside the window
            while (timestamps.Count > 0 && now - timestamps.Peek() > _messageTimeWindow.TotalMilliseconds)
            {
                timestamps.Dequeue();
            }

            // Check if user has sent too many messages
            if (timestamps.Count >= _maxMessagesPerWindow)
            {
                _logger.LogWarning("Spam detected: User {UserId} exceeded message rate limit", userId);
                return true;
            }

            timestamps.Enqueue(now);
        }
        else
        {
            _userMessageTimestamps[userId] = new Queue<long> { now };
        }

        if (string.IsNullOrEmpty(message.Content))
            return false;

        var content = message.Content;

        // Check 2: Excessive mentions (pinging spam)
        var mentionCount = Regex.Matches(content, @"<@[!&]?\d+>").Count;
        if (mentionCount > _maxMentions)
        {
            _logger.LogWarning("Spam detected: User {UserId} exceeded mention limit ({Count})", userId, mentionCount);
            return true;
        }

        // Check 3: Repeated character patterns (ASCII spam)
        if (HasExcessiveRepeatedCharacters(content))
        {
            _logger.LogWarning("Spam detected: User {UserId} sent excessive repeated characters", userId);
            return true;
        }

        // Check 4: All caps with excessive length
        if (content.Length > 50 && content.All(c => !char.IsLower(c) && char.IsLetter(c)))
        {
            _logger.LogWarning("Spam detected: User {UserId} sent excessive all-caps message", userId);
            return true;
        }

        return false;
    }

    private bool HasExcessiveRepeatedCharacters(string content)
    {
        if (content.Length < 5) return false;

        int repeatedCharCount = 0;
        char lastChar = '\0';
        int consecutiveCount = 0;

        foreach (char c in content)
        {
            if (c == lastChar)
            {
                consecutiveCount++;
                if (consecutiveCount >= 3) // 3+ repeats of same char
                    repeatedCharCount++;
            }
            else
            {
                consecutiveCount = 1;
                lastChar = c;
            }
        }

        double spamRatio = (double)repeatedCharCount / content.Length;
        return spamRatio > _spamCharacterThreshold;
    }

    private ulong ParseUserId(string input)
    {
        // Parse user mention <@123456> or just ID
        var match = Regex.Match(input, @"<?@?!?(\d+)>?");
        if (match.Success && ulong.TryParse(match.Groups[1].Value, out var userId))
        {
            return userId;
        }
        return 0;
    }

    private async Task<bool> HasModeratorPermissionsAsync(ulong userId, ulong guildId)
    {
        // In a real implementation, check the user's roles against configured moderator roles
        // For this example, we'll just check if they're the server owner or have a specific role
        try
        {
            var guild = await _client.Rest.GetGuildAsync(guildId);
            return guild.OwnerId == userId; // Only owner can moderate for this example
        }
        catch
        {
            return false;
        }
    }

    private async Task<Channel?> GetWelcomeChannelAsync(ulong guildId)
    {
        try
        {
            var channels = await _client.Rest.GetGuildChannelsAsync(guildId);
            // Find a channel named "welcome" or "general"
            return channels.FirstOrDefault(c =>
                c.Name?.Contains("welcome", StringComparison.OrdinalIgnoreCase) == true) ??
                channels.FirstOrDefault(c =>
                    c.Name?.Contains("general", StringComparison.OrdinalIgnoreCase) == true);
        }
        catch
        {
            return null;
        }
    }
}
