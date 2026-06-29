#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PawSharp.API.Models;
using PawSharp.Core.Entities;
using PawSharp.Gateway.Events;

namespace PawSharp.Commands.Autocomplete;

/// <summary>
/// Built-in autocomplete providers for common Discord entities.
/// </summary>
public static class BuiltInAutocompleteProviders
{
    /// <summary>
    /// Autocomplete provider for users in a guild.
    /// </summary>
    public sealed class UserAutocompleteProvider
    {
        /// <summary>
        /// Provides autocomplete suggestions for users based on the input.
        /// </summary>
        /// <param name="ctx">The autocomplete context.</param>
        /// <param name="input">The user's input.</param>
        /// <returns>A list of autocomplete choices.</returns>
        public async Task<IEnumerable<ApplicationCommandOptionChoice>> ProvideAsync(AutocompleteContext ctx, string input)
        {
            if (!ctx.GuildId.HasValue)
                return Array.Empty<ApplicationCommandOptionChoice>();

            var guild = ctx.Client.Cache.GetGuild(ctx.GuildId.Value);
            if (guild == null)
                return Array.Empty<ApplicationCommandOptionChoice>();

            var choices = new List<ApplicationCommandOptionChoice>();
            
            // Try to get members from cache
            var members = guild.Members?.Where(m => 
                string.IsNullOrEmpty(input) || 
                m.User?.Username?.Contains(input, StringComparison.OrdinalIgnoreCase) == true ||
                (m.User?.GlobalName?.Contains(input, StringComparison.OrdinalIgnoreCase) == true)
            ).Take(25);

            if (members != null)
            {
                foreach (var member in members)
                {
                    if (member.User != null)
                    {
                        var displayName = string.IsNullOrEmpty(member.User.GlobalName) 
                            ? member.User.Username 
                            : $"{member.User.Username} ({member.User.GlobalName})";
                        
                        choices.Add(new ApplicationCommandOptionChoice
                        {
                            Name = displayName.Length > 100 ? displayName.Substring(0, 100) : displayName,
                            Value = member.User.Id.ToString()
                        });
                    }
                }
            }

            return choices;
        }
    }

    /// <summary>
    /// Autocomplete provider for roles in a guild.
    /// </summary>
    public sealed class RoleAutocompleteProvider
    {
        /// <summary>
        /// Provides autocomplete suggestions for roles based on the input.
        /// </summary>
        /// <param name="ctx">The autocomplete context.</param>
        /// <param name="input">The user's input.</param>
        /// <returns>A list of autocomplete choices.</returns>
        public async Task<IEnumerable<ApplicationCommandOptionChoice>> ProvideAsync(AutocompleteContext ctx, string input)
        {
            if (!ctx.GuildId.HasValue)
                return Array.Empty<ApplicationCommandOptionChoice>();

            var guild = ctx.Client.Cache.GetGuild(ctx.GuildId.Value);
            if (guild?.Roles == null)
                return Array.Empty<ApplicationCommandOptionChoice>();

            var choices = guild.Roles
                .Where(r => 
                    string.IsNullOrEmpty(input) || 
                    r.Name?.Contains(input, StringComparison.OrdinalIgnoreCase) == true)
                .Where(r => r.Id != ctx.GuildId.Value) // Exclude @everyone
                .Take(25)
                .Select(r => new ApplicationCommandOptionChoice
                {
                    Name = r.Name?.Length > 100 ? r.Name.Substring(0, 100) : r.Name ?? "@everyone",
                    Value = r.Id.ToString()
                })
                .ToList();

            return choices;
        }
    }

    /// <summary>
    /// Autocomplete provider for channels in a guild.
    /// </summary>
    public sealed class ChannelAutocompleteProvider
    {
        /// <summary>
        /// Provides autocomplete suggestions for channels based on the input.
        /// </summary>
        /// <param name="ctx">The autocomplete context.</param>
        /// <param name="input">The user's input.</param>
        /// <param name="channelType">Optional filter for specific channel types.</param>
        /// <returns>A list of autocomplete choices.</returns>
        public async Task<IEnumerable<ApplicationCommandOptionChoice>> ProvideAsync(AutocompleteContext ctx, string input, PawSharp.Core.Enums.ChannelType? channelType = null)
        {
            if (!ctx.GuildId.HasValue)
                return Array.Empty<ApplicationCommandOptionChoice>();

            var guild = ctx.Client.Cache.GetGuild(ctx.GuildId.Value);
            if (guild?.Channels == null)
                return Array.Empty<ApplicationCommandOptionChoice>();

            var channels = guild.Channels;

            // Filter by channel type if specified
            if (channelType.HasValue)
            {
                channels = channels.Where(c => c.Type == channelType.Value).ToList();
            }

            var choices = channels
                .Where(c => 
                    string.IsNullOrEmpty(input) || 
                    c.Name?.Contains(input, StringComparison.OrdinalIgnoreCase) == true)
                .Take(25)
                .Select(c => new ApplicationCommandOptionChoice
                {
                    Name = c.Name?.Length > 100 ? c.Name.Substring(0, 100) : c.Name ?? "unknown",
                    Value = c.Id.ToString()
                })
                .ToList();

            return choices;
        }
    }
}

/// <summary>
/// Context for autocomplete interactions.
/// </summary>
public class AutocompleteContext
{
    /// <summary>
    /// Gets the Discord client.
    /// </summary>
    public PawSharp.Client.DiscordClient Client { get; }

    /// <summary>
    /// Gets the guild ID if in a guild.
    /// </summary>
    public ulong? GuildId { get; }

    /// <summary>
    /// Gets the channel ID.
    /// </summary>
    public ulong ChannelId { get; }

    /// <summary>
    /// Gets the user who triggered the autocomplete.
    /// </summary>
    public PawSharp.Core.Entities.User User { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutocompleteContext"/> class.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="interaction">The autocomplete interaction event.</param>
    public AutocompleteContext(PawSharp.Client.DiscordClient client, InteractionCreateEvent interaction)
    {
        Client = client;
        GuildId = interaction.GuildId;
        ChannelId = interaction.ChannelId;
        User = interaction.Member?.User ?? interaction.User ?? new PawSharp.Core.Entities.User { Id = 0, Username = "unknown", Discriminator = "0" };
    }
}
