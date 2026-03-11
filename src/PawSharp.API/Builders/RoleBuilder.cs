#nullable enable
using System;
using PawSharp.API.Models;

namespace PawSharp.API.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="CreateRoleRequest"/> objects.
/// </summary>
/// <example>
/// <code>
/// var role = new RoleBuilder()
///     .WithName("Moderator")
///     .WithColor(0xFF5733)
///     .WithPermissions("8")          // Administrator
///     .WithHoist()
///     .WithMentionable()
///     .Build();
/// await client.CreateGuildRoleAsync(guildId, role);
/// </code>
/// </example>
public sealed class RoleBuilder
{
    private string? _name;
    private string? _permissions;
    private int? _color;
    private bool? _hoist;
    private string? _icon;
    private string? _unicodeEmoji;
    private bool? _mentionable;

    /// <summary>Sets the role name (max 100 characters; default is "new role").</summary>
    public RoleBuilder WithName(string name)
    {
        if (name.Length > 100)
            throw new ArgumentException("Role name cannot exceed 100 characters.", nameof(name));
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the permissions bit-set string (e.g. "8" for Administrator).
    /// Pass <c>"0"</c> to create a role with no permissions.
    /// </summary>
    public RoleBuilder WithPermissions(string permissions)
    {
        _permissions = permissions;
        return this;
    }

    /// <summary>
    /// Sets the role colour as a packed RGB integer (e.g. <c>0xFF5733</c>).
    /// Pass <c>0</c> to use the default colour (appears grey/colourless).
    /// </summary>
    public RoleBuilder WithColor(int color)
    {
        if (color < 0 || color > 0xFFFFFF)
            throw new ArgumentOutOfRangeException(nameof(color), "Color must be between 0x000000 and 0xFFFFFF.");
        _color = color;
        return this;
    }

    /// <summary>Displays role members separately from online members in the sidebar.</summary>
    public RoleBuilder WithHoist(bool hoist = true)
    {
        _hoist = hoist;
        return this;
    }

    /// <summary>
    /// Sets a custom role icon as a base64-encoded data URI (requires the guild to have
    /// the <c>ROLE_ICONS</c> feature; mutually exclusive with <see cref="WithUnicodeEmoji"/>).
    /// </summary>
    public RoleBuilder WithIcon(string imageDataUri)
    {
        _icon = imageDataUri;
        _unicodeEmoji = null;
        return this;
    }

    /// <summary>
    /// Sets a unicode emoji as the role icon (mutually exclusive with <see cref="WithIcon"/>).
    /// </summary>
    public RoleBuilder WithUnicodeEmoji(string emoji)
    {
        _unicodeEmoji = emoji;
        _icon = null;
        return this;
    }

    /// <summary>Allows anyone to @mention this role.</summary>
    public RoleBuilder WithMentionable(bool mentionable = true)
    {
        _mentionable = mentionable;
        return this;
    }

    /// <summary>Builds and returns the <see cref="CreateRoleRequest"/>.</summary>
    public CreateRoleRequest Build()
    {
        return new CreateRoleRequest
        {
            Name = _name,
            Permissions = _permissions,
            Color = _color,
            Hoist = _hoist,
            Icon = _icon,
            UnicodeEmoji = _unicodeEmoji,
            Mentionable = _mentionable
        };
    }
}
