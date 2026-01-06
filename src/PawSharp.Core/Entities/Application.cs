#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord application.
/// </summary>
public class Application : DiscordEntity
{
    /// <summary>
    /// The name of the app.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The icon hash of the app.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
    
    /// <summary>
    /// The description of the app.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// An array of rpc origin urls, if rpc is enabled.
    /// </summary>
    [JsonPropertyName("rpc_origins")]
    public List<string>? RpcOrigins { get; set; }
    
    /// <summary>
    /// When false only app owner can join the app's bot to guilds.
    /// </summary>
    [JsonPropertyName("bot_public")]
    public bool BotPublic { get; set; }
    
    /// <summary>
    /// When true the app's bot will only join upon completion of the full oauth2 code grant flow.
    /// </summary>
    [JsonPropertyName("bot_require_code_grant")]
    public bool BotRequireCodeGrant { get; set; }
    
    /// <summary>
    /// The url of the app's terms of service.
    /// </summary>
    [JsonPropertyName("terms_of_service_url")]
    public string? TermsOfServiceUrl { get; set; }
    
    /// <summary>
    /// The url of the app's privacy policy.
    /// </summary>
    [JsonPropertyName("privacy_policy_url")]
    public string? PrivacyPolicyUrl { get; set; }
    
    /// <summary>
    /// Partial user object containing info on the owner of the application.
    /// </summary>
    [JsonPropertyName("owner")]
    public User? Owner { get; set; }
    
    /// <summary>
    /// Deprecated and will be removed in v11. An empty string.
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// The hex encoded key for verification in interactions and the GameSDK's GetTicket.
    /// </summary>
    [JsonPropertyName("verify_key")]
    public string VerifyKey { get; set; } = string.Empty;
    
    /// <summary>
    /// The team's associated with the app.
    /// </summary>
    [JsonPropertyName("team")]
    public Team? Team { get; set; }
    
    /// <summary>
    /// If the application belongs to a team, this will be a list of the members of that team.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// If this is a game sold on Discord, this field will be the guild to which it has been linked.
    /// </summary>
    [JsonPropertyName("primary_sku_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? PrimarySkuId { get; set; }
    
    /// <summary>
    /// If this is a game sold on Discord, this field will be the URL slug that links to the store page.
    /// </summary>
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }
    
    /// <summary>
    /// The application's default rich presence invite cover image hash.
    /// </summary>
    [JsonPropertyName("cover_image")]
    public string? CoverImage { get; set; }
    
    /// <summary>
    /// The application's public flags.
    /// </summary>
    [JsonPropertyName("flags")]
    public ApplicationFlags? Flags { get; set; }
    
    /// <summary>
    /// Up to 5 tags describing the content and functionality of the application.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// Settings for the application's default in-app authorization link, if enabled.
    /// </summary>
    [JsonPropertyName("install_params")]
    public InstallParams? InstallParams { get; set; }
    
    /// <summary>
    /// The application's default custom authorization link, if enabled.
    /// </summary>
    [JsonPropertyName("custom_install_url")]
    public string? CustomInstallUrl { get; set; }
    
    /// <summary>
    /// The application's role connection verification entry point, which when configured will render the app as a verification method in the guild role verification configuration.
    /// </summary>
    [JsonPropertyName("role_connections_verification_url")]
    public string? RoleConnectionsVerificationUrl { get; set; }
}

/// <summary>
/// Represents a team.
/// </summary>
public class Team
{
    /// <summary>
    /// A hash of the image of the team's icon.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
    
    /// <summary>
    /// The unique id of the team.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    /// <summary>
    /// The members of the team.
    /// </summary>
    [JsonPropertyName("members")]
    public List<TeamMember> Members { get; set; } = new();
    
    /// <summary>
    /// The name of the team.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The user id of the current team owner.
    /// </summary>
    [JsonPropertyName("owner_user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong OwnerUserId { get; set; }
}

/// <summary>
/// Represents a team member.
/// </summary>
public class TeamMember
{
    /// <summary>
    /// The user's membership state on the team.
    /// </summary>
    [JsonPropertyName("membership_state")]
    public MembershipState MembershipState { get; set; }
    
    /// <summary>
    /// Will always be ["*"].
    /// </summary>
    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();
    
    /// <summary>
    /// The id of the parent team of which they are a member.
    /// </summary>
    [JsonPropertyName("team_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong TeamId { get; set; }
    
    /// <summary>
    /// The avatar, discriminator, id, and username of the user.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
    
    /// <summary>
    /// The role of the team member.
    /// </summary>
    [JsonPropertyName("role")]
    public TeamMemberRole Role { get; set; }
}

/// <summary>
/// Represents install parameters.
/// </summary>
public class InstallParams
{
    /// <summary>
    /// The scopes to add the application to the server with.
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = new();
    
    /// <summary>
    /// The permissions to request for the bot role.
    /// </summary>
    [JsonPropertyName("permissions")]
    public string Permissions { get; set; } = string.Empty;
}

/// <summary>
/// Membership state.
/// </summary>
public enum MembershipState
{
    /// <summary>
    /// Invited.
    /// </summary>
    Invited = 1,
    
    /// <summary>
    /// Accepted.
    /// </summary>
    Accepted = 2
}

/// <summary>
/// Team member role.
/// </summary>
public enum TeamMemberRole
{
    /// <summary>
    /// Owner.
    /// </summary>
    Owner,
    
    /// <summary>
    /// Admin.
    /// </summary>
    Admin,
    
    /// <summary>
    /// Developer.
    /// </summary>
    Developer,
    
    /// <summary>
    /// Read-only.
    /// </summary>
    ReadOnly
}

/// <summary>
/// Application flags.
/// </summary>
[Flags]
public enum ApplicationFlags
{
    /// <summary>
    /// Indicates if an app uses the Auto Moderation API.
    /// </summary>
    AutoModerationRuleCreateBadge = 1 << 6,
    
    /// <summary>
    /// Intent required for bots in 100 or more servers to receive presence_update events.
    /// </summary>
    GatewayPresence = 1 << 12,
    
    /// <summary>
    /// Intent required for bots in 100 or more servers to receive member-related events like guild_member_add.
    /// </summary>
    GatewayGuildMembers = 1 << 13,
    
    /// <summary>
    /// Intent required for bots in 100 or more servers to receive message events like message_create.
    /// </summary>
    GatewayMessageContent = 1 << 14,
    
    /// <summary>
    /// Indicates unusual growth of an app that prevents verification.
    /// </summary>
    GatewayMessageContentLimited = 1 << 15,
    
    /// <summary>
    /// Indicates if an app is embedded within the Discord client (currently unavailable publicly).
    /// </summary>
    Embedded = 1 << 17,
    
    /// <summary>
    /// Intent required for bots in 100 or more servers to receive message reaction events like message_reaction_add.
    /// </summary>
    GatewayGuildModerationConfiguration = 1 << 20
}