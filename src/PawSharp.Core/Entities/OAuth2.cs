#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents an OAuth2 application.
/// </summary>
public class OAuth2Application : Application
{
    /// <summary>
    /// The application's public key.
    /// </summary>
    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; } = string.Empty;
    
    /// <summary>
    /// The application's redirect URIs.
    /// </summary>
    [JsonPropertyName("redirect_uris")]
    public List<string> RedirectUris { get; set; } = new();
    
    /// <summary>
    /// The application's scopes.
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = new();
    
    /// <summary>
    /// The application's bot user.
    /// </summary>
    [JsonPropertyName("bot")]
    public User? Bot { get; set; }
}

/// <summary>
/// Represents an OAuth2 token.
/// </summary>
public class OAuth2Token
{
    /// <summary>
    /// The access token.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// The token type.
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
    
    /// <summary>
    /// The expiration time of the access token.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// The refresh token.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// The scopes of the access token.
    /// </summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}

/// <summary>
/// Represents OAuth2 information.
/// </summary>
public class OAuth2Info
{
    /// <summary>
    /// The application information.
    /// </summary>
    [JsonPropertyName("application")]
    public OAuth2Application Application { get; set; } = null!;
    
    /// <summary>
    /// The scopes the user has authorized the application for.
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = new();
    
    /// <summary>
    /// When the access token expires.
    /// </summary>
    [JsonPropertyName("expires")]
    public DateTimeOffset Expires { get; set; }
    
    /// <summary>
    /// The user who has authorized, if the user has authorized with the identify scope.
    /// </summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }
}