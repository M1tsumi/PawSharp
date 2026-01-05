#nullable enable
using System.Text.Json.Serialization;
using PawSharp.Core.Enums;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord user.
/// </summary>
public class User : DiscordEntity
{
    /// <summary>
    /// The user's username, not unique across the platform.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's Discord-tag (discriminator). Legacy field, will be "0" for new users.
    /// </summary>
    [JsonPropertyName("discriminator")]
    public string Discriminator { get; set; } = "0";
    
    /// <summary>
    /// The user's display name, if set. For bots, this is the application name.
    /// </summary>
    [JsonPropertyName("global_name")]
    public string? GlobalName { get; set; }
    
    /// <summary>
    /// The user's avatar hash.
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    
    /// <summary>
    /// Whether the user belongs to an OAuth2 application.
    /// </summary>
    [JsonPropertyName("bot")]
    public bool? Bot { get; set; }
    
    /// <summary>
    /// Whether the user is an Official Discord System user (part of the urgent message system).
    /// </summary>
    [JsonPropertyName("system")]
    public bool? System { get; set; }
    
    /// <summary>
    /// Whether the user has two factor enabled on their account.
    /// </summary>
    [JsonPropertyName("mfa_enabled")]
    public bool? MfaEnabled { get; set; }
    
    /// <summary>
    /// The user's banner hash.
    /// </summary>
    [JsonPropertyName("banner")]
    public string? Banner { get; set; }
    
    /// <summary>
    /// The user's banner color encoded as an integer representation of hexadecimal color code.
    /// </summary>
    [JsonPropertyName("accent_color")]
    public int? AccentColor { get; set; }
    
    /// <summary>
    /// The user's chosen language option.
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
    
    /// <summary>
    /// Whether the email on this account has been verified.
    /// </summary>
    [JsonPropertyName("verified")]
    public bool? Verified { get; set; }
    
    /// <summary>
    /// The user's email.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    /// <summary>
    /// The flags on a user's account.
    /// </summary>
    [JsonPropertyName("flags")]
    public UserFlags? Flags { get; set; }
    
    /// <summary>
    /// The type of Nitro subscription on a user's account.
    /// </summary>
    [JsonPropertyName("premium_type")]
    public int? PremiumType { get; set; }
    
    /// <summary>
    /// The public flags on a user's account.
    /// </summary>
    [JsonPropertyName("public_flags")]
    public UserFlags? PublicFlags { get; set; }
    
    /// <summary>
    /// The user's avatar decoration hash.
    /// </summary>
    [JsonPropertyName("avatar_decoration_data")]
    public object? AvatarDecorationData { get; set; }

    
    /// <summary>
    /// Gets the user's avatar URL.
    /// </summary>
    public string GetAvatarUrl(ushort size = 128)
    {
        if (string.IsNullOrEmpty(Avatar))
        {
            var defaultAvatar = (Id >> 22) % 6;
            return $"https://cdn.discordapp.com/embed/avatars/{defaultAvatar}.png";
        }
        
        var extension = Avatar.StartsWith("a_") ? "gif" : "png";
        return $"https://cdn.discordapp.com/avatars/{Id}/{Avatar}.{extension}?size={size}";
    }
}
