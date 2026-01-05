using System.Collections.Generic;
using PawSharp.Interactions.Models;

namespace PawSharp.Interactions.Builders;

/// <summary>
/// Builder for creating slash commands.
/// </summary>
public class SlashCommandBuilder
{
    private readonly ApplicationCommand _command = new();

    public SlashCommandBuilder(string name, string description)
    {
        _command.Name = name;
        _command.Description = description;
        _command.Type = ApplicationCommandType.ChatInput;
        _command.Options = new List<ApplicationCommandOption>();
    }

    public SlashCommandBuilder SetDefaultMemberPermissions(ulong permissions)
    {
        _command.DefaultMemberPermissions = permissions.ToString();
        return this;
    }

    public SlashCommandBuilder SetDmPermission(bool allowed)
    {
        _command.DmPermission = allowed;
        return this;
    }

    public SlashCommandBuilder SetNsfw(bool nsfw)
    {
        _command.Nsfw = nsfw;
        return this;
    }

    public SlashCommandBuilder AddOption(ApplicationCommandOption option)
    {
        _command.Options ??= new List<ApplicationCommandOption>();
        _command.Options.Add(option);
        return this;
    }

    public SlashCommandBuilder AddStringOption(string name, string description, bool required = false, bool autocomplete = false)
    {
        return AddOption(new ApplicationCommandOption
        {
            Type = ApplicationCommandOptionType.String,
            Name = name,
            Description = description,
            Required = required,
            Autocomplete = autocomplete
        });
    }

    public SlashCommandBuilder AddIntegerOption(string name, string description, bool required = false, long? minValue = null, long? maxValue = null)
    {
        return AddOption(new ApplicationCommandOption
        {
            Type = ApplicationCommandOptionType.Integer,
            Name = name,
            Description = description,
            Required = required,
            MinValue = minValue,
            MaxValue = maxValue
        });
    }

    public SlashCommandBuilder AddBooleanOption(string name, string description, bool required = false)
    {
        return AddOption(new ApplicationCommandOption
        {
            Type = ApplicationCommandOptionType.Boolean,
            Name = name,
            Description = description,
            Required = required
        });
    }

    public SlashCommandBuilder AddUserOption(string name, string description, bool required = false)
    {
        return AddOption(new ApplicationCommandOption
        {
            Type = ApplicationCommandOptionType.User,
            Name = name,
            Description = description,
            Required = required
        });
    }

    public SlashCommandBuilder AddChannelOption(string name, string description, bool required = false)
    {
        return AddOption(new ApplicationCommandOption
        {
            Type = ApplicationCommandOptionType.Channel,
            Name = name,
            Description = description,
            Required = required
        });
    }

    public SlashCommandBuilder AddRoleOption(string name, string description, bool required = false)
    {
        return AddOption(new ApplicationCommandOption
        {
            Type = ApplicationCommandOptionType.Role,
            Name = name,
            Description = description,
            Required = required
        });
    }

    public ApplicationCommand Build()
    {
        return _command;
    }
}
