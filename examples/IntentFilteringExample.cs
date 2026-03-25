using PawSharp.Client;
using PawSharp.Core.Enums;
using PawSharp.Core.Events;

namespace PawSharp.Examples;

/// <summary>
/// Example demonstrating the Intent Filtering System feature.
/// 
/// Shows how to declare event interests, validate them, and handle
/// intent configuration issues gracefully.
/// </summary>
public sealed class IntentFilteringExample
{
    /// <summary>
    /// Example handler class using EventInterestAttribute to declare requirements.
    /// </summary>
    [EventInterest("MESSAGE_CREATE", "MESSAGE_UPDATE", "MESSAGE_DELETE")]
    public class MessageHandlers
    {
        private readonly DiscordClient _client;

        public MessageHandlers(DiscordClient client)
        {
            _client = client;
        }

        public async Task RegisterAsync()
        {
            // These handlers are protected by the [EventInterest] attribute
            // If the required intents aren't enabled, ValidateIntents() will warn
            
            _client.OnMessageCreated(async msg =>
            {
                if (msg.Author?.Bot == true) return;
                Console.WriteLine($"[MSG] {msg.Author?.Username}: {msg.Content}");

                // This handler requires MessageContent intent to read msg.Content
                if (msg.Content == "!ping")
                    await _client.Rest.CreateMessageAsync(msg.ChannelId, 
                        new() { Content = "Pong! 🏓" });
            });

            _client.OnMessageUpdated(async msg =>
            {
                Console.WriteLine($"[EDIT] Message {msg.Id} updated in #{msg.ChannelId}");
            });

            _client.OnMessageDeleted(async msg =>
            {
                Console.WriteLine($"[DELETE] Message {msg.Id} deleted from #{msg.ChannelId}");
            });
        }
    }

    /// <summary>
    /// Example showing how to validate intents before connecting.
    /// </summary>
    public static async Task ValidationExampleAsync()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Intent Filtering System Example");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") 
            ?? throw new InvalidOperationException("DISCORD_TOKEN not set");

        // Create client with minimal intents (intentionally leaving some out to demonstrate validation)
        var client = new PawSharpClientBuilder()
            .WithToken(token)
            .WithIntents(GatewayIntents.AllNonPrivileged) // Note: includes MessageContent
            .UseConsoleLogging()
            .Build();

        Console.WriteLine("1. SETTING UP HANDLERS WITH EVENT INTERESTS\n");

        var handlers = new MessageHandlers(client);
        await handlers.RegisterAsync();

        Console.WriteLine("   ✓ Registered 3 message handlers (requires: GuildMessages intent)\n");

        // Register a member handler
        client.OnGuildMemberJoined(async member =>
        {
            Console.WriteLine($"[JOIN] {member.User?.Username} joined {member.GuildId}");
        });
        Console.WriteLine("   ✓ Registered 1 member handler (requires: GuildMembers intent)\n");

        Console.WriteLine("2. VALIDATING INTENT CONFIGURATION\n");

        // Method 1: Simple validation
        var validationResult = client.ValidateIntents();
        
        Console.WriteLine($"   Validation result: {(validationResult.IsValid ? "PASSED ✓" : "FAILED ✗")}");
        Console.WriteLine($"   Issues found: {validationResult.Count}\n");

        if (!validationResult.IsValid)
        {
            Console.WriteLine("   Details of issues:");
            foreach (var (eventType, required, missing) in validationResult.Issues)
            {
                Console.WriteLine($"     - Event '{eventType}':");
                Console.WriteLine($"       Required: {required}");
                Console.WriteLine($"       Missing: {missing}\n");
            }
        }

        // Method 2: Get recommended intents
        Console.WriteLine("3. INTENT RECOMMENDATIONS\n");
        var recommended = client.GetRecommendedIntents();
        Console.WriteLine($"   Recommended intents: {recommended}");
        Console.WriteLine($"   Currently enabled:  {client.Config.Intents}\n");

        // Method 3: Detailed logging
        Console.WriteLine("4. STARTUP DIAGNOSTICS\n");
        client.LogIntentSummary();

        Console.WriteLine("\n5. CONNECTING WITH VALIDATED CONFIGURATION\n");
        
        if (validationResult.IsValid)
        {
            Console.WriteLine("   ✓ All intents are valid. Safe to connect!");
            Console.WriteLine("\n   Starting connection...\n");
            
            await client.ConnectAsync();
            
            // Keep running for a bit
            await Task.Delay(TimeSpan.FromSeconds(10));
            
            await client.DisconnectAsync();
            Console.WriteLine("\n✓ Connection closed gracefully");
        }
        else
        {
            Console.WriteLine("   ✗ Intent validation failed!");
            Console.WriteLine("   Skipping connection.\n");
            
            // In a real app, you might:
            // 1. Log the issues for later review
            // 2. Suggest fixes to the user
            // 3. Gracefully degrade by unregistering problematic handlers
        }

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Example complete!");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Example showing how to use event interest attributes on individual handlers.
    /// </summary>
    public static async Task HandlerLevelIntentExampleAsync()
    {
        var client = new PawSharpClientBuilder()
            .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
            .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.GuildMembers)
            .UseConsoleLogging()
            .Build();

        // Individual handlers can declare their intent requirements
        // (Though currently this is more for documentation and future optimization)

        client.OnMessageCreated(async msg =>
        {
            // This handler implicitly requires: GuildMessages + MessageContent
            if (msg.Content?.StartsWith("!help") == true)
                await client.Rest.CreateMessageAsync(msg.ChannelId, 
                    new() { Content = "Available commands: !ping, !help, !members" });
        });

        client.OnGuildMemberJoined(async member =>
        {
            // This handler requires: GuildMembers
            Console.WriteLine($"Welcome {member.User?.Username}!");
        });

        // Validate before connecting
        var result = client.ValidateIntents();
        if (result.IsValid)
        {
            await client.ConnectAsync();
        }
        else
        {
            Console.WriteLine("Intent configuration is invalid. See details above.");
        }
    }

    /// <summary>
    /// Example showing how to gracefully handle missing intents.
    /// </summary>
    public static async Task GracefulDegradationExampleAsync()
    {
        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!;

        // Start with minimal intents
        var client = new PawSharpClientBuilder()
            .WithToken(token)
            .WithIntents(GatewayIntents.GuildMessages) // Only messages, no members
            .UseConsoleLogging()
            .Build();

        // Register handlers that might fail
        client.OnGuildMemberJoined(async member =>
        {
            // This will be flagged as missing GuildMembers intent
            Console.WriteLine($"Member joined: {member.User?.Username}");
        });

        client.OnMessageCreated(async msg =>
        {
            // This is OK - we have GuildMessages intent
            Console.WriteLine($"Message from {msg.Author?.Username}");
        });

        // Check for issues
        var result = client.ValidateIntents();

        if (!result.IsValid)
        {
            Console.WriteLine("⚠️  Intent configuration issues detected:");
            foreach (var (eventType, _, missing) in result.Issues)
            {
                Console.WriteLine($"   - {eventType} is missing {missing}");
            }

            Console.WriteLine("\nDisabling handlers with missing intents and retrying...");

            // In a real app, you could:
            // 1. Unregister handlers with missing intents
            // 2. Log which features are disabled
            // 3. Proceed with degraded functionality
        }
    }
}
