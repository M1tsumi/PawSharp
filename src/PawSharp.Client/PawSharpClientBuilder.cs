#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PawSharp.API.Clients;
using PawSharp.API.Interfaces;
using PawSharp.API.RateLimit;
using PawSharp.Cache.Interfaces;
using PawSharp.Cache.Providers;
using PawSharp.Core.Enums;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Interactions;

namespace PawSharp.Client;

/// <summary>
/// Lightweight fluent builder that wires up a fully-configured <see cref="DiscordClient"/>
/// without requiring the Microsoft DI container.
/// <para>
/// For ASP.NET Core or similar host-based applications prefer the
/// <c>services.AddPawSharp(options)</c> extension method instead.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var client = new PawSharpClientBuilder()
///     .WithToken("Bot YOUR_TOKEN_HERE")
///     .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
///     .UseConsoleLogging(LogLevel.Information)
///     .Build();
///
/// client.OnMessageCreated(msg => Console.WriteLine($"[{msg.ChannelId}] {msg.Author?.Username}: {msg.Content}"));
///
/// await client.ConnectAsync();
/// await Task.Delay(-1); // keep alive
/// </code>
/// </example>
public sealed class PawSharpClientBuilder
{
    private string           _token          = string.Empty;
    private GatewayIntents   _intents        = GatewayIntents.AllNonPrivileged;
    private int              _apiVersion     = 10;
    private int              _shards         = 1;
    private int              _shardCount     = 1;
    private bool             _compression    = false;
    private ILoggerFactory?  _loggerFactory;
    private IEntityCache?    _cache;
    private HttpClient?      _httpClient;
    private PawSharpOptions.PresenceOptions? _presence;

    // ── Token ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the bot token. Must start with <c>Bot </c> (Discord format).
    /// If you provide a raw token without the prefix, the prefix is added automatically.
    /// </summary>
    public PawSharpClientBuilder WithToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Bot token must not be null or empty.", nameof(token));

        // Strip the "Bot " prefix before format-validating the raw token.
        var rawToken = token.StartsWith("Bot ", StringComparison.OrdinalIgnoreCase)
            ? token.Substring(4)
            : token;

        // Discord bot tokens are three Base64url segments separated by '.'.
        // Rejecting obviously wrong values (webhook URLs, client secrets) early
        // produces a clear error rather than a silent HTTP 401 later.
        if (rawToken.Split('.').Length != 3)
            throw new ArgumentException(
                "The provided value does not appear to be a valid Discord bot token. " +
                "Ensure you are using a bot token, not a client secret or webhook URL.",
                nameof(token));

        // Accept both "Bot TOKEN" and raw "TOKEN" formats
        _token = token.StartsWith("Bot ", StringComparison.OrdinalIgnoreCase)
            ? token
            : $"Bot {token}";

        return this;
    }

    // ── Intents ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the gateway intents to subscribe to.
    /// </summary>
    /// <remarks>
    /// <see cref="GatewayIntents.MessageContent"/>, <see cref="GatewayIntents.GuildMembers"/>,
    /// and <see cref="GatewayIntents.GuildPresences"/> are privileged — enable them in the
    /// Discord Developer Portal before using them.
    /// </remarks>
    public PawSharpClientBuilder WithIntents(GatewayIntents intents)
    {
        _intents = intents;
        return this;
    }

    /// <summary>
    /// Adds additional intents to the current set.
    /// </summary>
    public PawSharpClientBuilder AddIntents(GatewayIntents intents)
    {
        _intents |= intents;
        return this;
    }

    // ── API version ────────────────────────────────────────────────────────────

    /// <summary>Sets the Discord API version to use (default: 10).</summary>
    public PawSharpClientBuilder WithApiVersion(int version)
    {
        if (version < PawSharpOptions.MinSupportedApiVersion || version > PawSharpOptions.MaxSupportedApiVersion)
            throw new ArgumentOutOfRangeException(nameof(version),
                $"API version must be between {PawSharpOptions.MinSupportedApiVersion} and {PawSharpOptions.MaxSupportedApiVersion}.");
        _apiVersion = version;
        return this;
    }

    // ── Sharding ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Configures sharding for large bots (> ~2 500 guilds).
    /// </summary>
    /// <param name="shardId">Zero-based index of this shard.</param>
    /// <param name="totalShards">Total number of shards across all instances.</param>
    public PawSharpClientBuilder WithSharding(int shardId, int totalShards)
    {
        if (shardId   < 0) throw new ArgumentOutOfRangeException(nameof(shardId));
        if (totalShards < 1 || shardId >= totalShards)
            throw new ArgumentOutOfRangeException(nameof(totalShards),
                "totalShards must be >= 1 and shardId must be < totalShards.");

        _shards     = shardId;
        _shardCount = totalShards;
        return this;
    }

    // ── Compression ────────────────────────────────────────────────────────────

    /// <summary>Enables zlib gateway compression (reduces bandwidth on large bots).</summary>
    public PawSharpClientBuilder UseCompression()
    {
        _compression = true;
        return this;
    }

    // ── Logging ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Supplies a custom <see cref="ILoggerFactory"/> for all PawSharp components.
    /// </summary>
    public PawSharpClientBuilder UseLoggerFactory(ILoggerFactory factory)
    {
        _loggerFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    /// <summary>
    /// Configures PawSharp to write logs to <c>Console.Out</c> at the specified minimum level.
    /// </summary>
    /// <param name="minimumLevel">Minimum level to log (default: <see cref="LogLevel.Information"/>).</param>
    public PawSharpClientBuilder UseConsoleLogging(LogLevel minimumLevel = LogLevel.Information)
    {
        _loggerFactory = LoggerFactory.Create(b => b
            .SetMinimumLevel(minimumLevel)
            .AddSimpleConsole(o =>
            {
                o.SingleLine        = true;
                o.TimestampFormat   = "HH:mm:ss ";
                o.IncludeScopes     = false;
            }));
        return this;
    }

    // ── Cache ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Uses the built-in in-memory cache (default behaviour when no cache is specified).
    /// </summary>
    public PawSharpClientBuilder UseMemoryCache()
    {
        _cache = new MemoryCacheProvider();
        return this;
    }

    /// <summary>Supplies a custom <see cref="IEntityCache"/> implementation.</summary>
    public PawSharpClientBuilder UseCache(IEntityCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    // ── HTTP client ────────────────────────────────────────────────────────────

    /// <summary>
    /// Supplies a custom <see cref="HttpClient"/> for the REST layer
    /// (e.g. to inject a mock in tests or add a custom HTTP handler).
    /// If not provided, a default instance is created automatically.
    /// </summary>
    public PawSharpClientBuilder UseHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        return this;
    }

    // ── Presence ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the bot's initial presence shown immediately after the gateway READY event.
    /// </summary>
    /// <param name="activityName">Activity text shown in the user list (e.g. "with fire").</param>
    /// <param name="activityType">
    /// Activity type integer: 0 = Playing (default), 1 = Streaming, 2 = Listening,
    /// 3 = Watching, 5 = Competing.
    /// </param>
    /// <param name="status">Discord status: "online" (default), "idle", "dnd", or "invisible".</param>
    /// <param name="streamUrl">Stream URL required when <paramref name="activityType"/> is 1 (Streaming).</param>
    public PawSharpClientBuilder WithPresence(
        string? activityName,
        int activityType = 0,
        string status = "online",
        string? streamUrl = null)
    {
        _presence = new PawSharpOptions.PresenceOptions
        {
            Status       = status,
            ActivityName = activityName,
            ActivityType = activityType,
            StreamUrl    = streamUrl,
        };
        return this;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="PawSharpClientBuilder"/> with default settings.
    /// </summary>
    public static PawSharpClientBuilder Create()
    {
        return new PawSharpClientBuilder();
    }

    // ── Build ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates configuration and constructs a fully wired <see cref="IDiscordClient"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the configuration is invalid.</exception>
    /// <example>
    /// <code>
    /// var client = PawSharpClientBuilder.Create()
    ///     .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    ///     .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    ///     .UseConsoleLogging(LogLevel.Information)
    ///     .Build();
    /// 
    /// await client.ConnectAsync();
    /// </code>
    /// </example>
    public IDiscordClient Build()
    {
        if (string.IsNullOrWhiteSpace(_token))
            throw new InvalidOperationException(
                "A bot token is required. Use WithToken() or set PawSharpOptions.Token " +
                "before calling Build(). Tokens should be loaded from environment variables " +
                "or a secure configuration source.");

        if (_intents == GatewayIntents.None)
            throw new InvalidOperationException(
                "At least one gateway intent must be specified. Use WithIntents() or " +
                "AddIntents() to configure which events your bot needs.");

        int apiVersion = _apiVersion > 0 ? _apiVersion : 10;
        if (apiVersion < PawSharpOptions.MinSupportedApiVersion || apiVersion > PawSharpOptions.MaxSupportedApiVersion)
            throw new InvalidOperationException(
                $"API version {apiVersion} is not supported. " +
                $"Supported versions: {PawSharpOptions.MinSupportedApiVersion}-{PawSharpOptions.MaxSupportedApiVersion}.");

        var options = new PawSharpOptions
        {
            Token              = _token,
            Intents            = _intents,
            ApiVersion         = _apiVersion,
            Shards             = _shards,
            ShardCount         = _shardCount,
            EnableCompression  = _compression,
            Presence           = _presence,
        };

        var logFactory = _loggerFactory ?? NullLoggerFactory.Instance;
        var cache      = _cache         ?? new MemoryCacheProvider(logger: logFactory.CreateLogger<MemoryCacheProvider>());
        var http       = _httpClient    ?? new HttpClient(new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
            SslOptions = new SslClientAuthenticationOptions
            {
                // Enforce TLS 1.2+ for secure Discord API communication
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }
        })
        {
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionOrLower,
        };
        var limiter    = new AdvancedRateLimiter();

        var rest = new DiscordRestClient(
            http,
            options,
            logFactory.CreateLogger<DiscordRestClient>(),
            limiter);

        var gateway = new GatewayClient(
            options,
            logFactory.CreateLogger<GatewayClient>());

        var interactionHandler = new InteractionHandler(rest, logFactory.CreateLogger<InteractionHandler>());

        return new DiscordClient(
            options,
            cache,
            logFactory.CreateLogger<DiscordClient>(),
            rest,
            gateway,
            interactionHandler,
            logFactory);
    }
}
