#nullable enable
using System;
using System.Globalization;
using System.Threading.Tasks;
using PawSharp.Core.Entities;

namespace PawSharp.Commands.Conversion;

/// <summary>
/// Built-in type converters for common types.
/// </summary>
internal static class BuiltInConverters
{
    /// <summary>
    /// String converter (identity conversion).
    /// </summary>
    internal sealed class StringTypeConverter : SyncTypeConverter<string>
    {
        protected override TypeConverterResult<string> ConvertSync(string value, CommandContext context)
        {
            return TypeConverterResult<string>.FromSuccess(value);
        }
    }

    /// <summary>
    /// Int32 converter.
    /// </summary>
    internal sealed class Int32Converter : SyncTypeConverter<int>
    {
        protected override TypeConverterResult<int> ConvertSync(string value, CommandContext context)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<int>.FromSuccess(result);
            }
            return TypeConverterResult<int>.FromError($"Unable to parse '{value}' as an integer.");
        }
    }

    /// <summary>
    /// Int64 converter.
    /// </summary>
    internal sealed class Int64Converter : SyncTypeConverter<long>
    {
        protected override TypeConverterResult<long> ConvertSync(string value, CommandContext context)
        {
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<long>.FromSuccess(result);
            }
            return TypeConverterResult<long>.FromError($"Unable to parse '{value}' as a long integer.");
        }
    }

    /// <summary>
    /// UInt64 converter (for Discord IDs).
    /// </summary>
    internal sealed class UInt64Converter : SyncTypeConverter<ulong>
    {
        protected override TypeConverterResult<ulong> ConvertSync(string value, CommandContext context)
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<ulong>.FromSuccess(result);
            }
            return TypeConverterResult<ulong>.FromError($"Unable to parse '{value}' as a snowflake ID.");
        }
    }

    /// <summary>
    /// Boolean converter.
    /// </summary>
    internal sealed class BooleanConverter : SyncTypeConverter<bool>
    {
        protected override TypeConverterResult<bool> ConvertSync(string value, CommandContext context)
        {
            var normalized = value.Trim().ToLowerInvariant();
            if (normalized == "true" || normalized == "yes" || normalized == "1" || normalized == "y")
            {
                return TypeConverterResult<bool>.FromSuccess(true);
            }
            if (normalized == "false" || normalized == "no" || normalized == "0" || normalized == "n")
            {
                return TypeConverterResult<bool>.FromSuccess(false);
            }
            return TypeConverterResult<bool>.FromError($"Unable to parse '{value}' as a boolean. Use true/false, yes/no, or 1/0.");
        }
    }

    /// <summary>
    /// Double converter.
    /// </summary>
    internal sealed class DoubleConverter : SyncTypeConverter<double>
    {
        protected override TypeConverterResult<double> ConvertSync(string value, CommandContext context)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<double>.FromSuccess(result);
            }
            return TypeConverterResult<double>.FromError($"Unable to parse '{value}' as a number.");
        }
    }

    /// <summary>
    /// Float converter.
    /// </summary>
    internal sealed class FloatConverter : SyncTypeConverter<float>
    {
        protected override TypeConverterResult<float> ConvertSync(string value, CommandContext context)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<float>.FromSuccess(result);
            }
            return TypeConverterResult<float>.FromError($"Unable to parse '{value}' as a number.");
        }
    }

    /// <summary>
    /// DateTime converter.
    /// </summary>
    internal sealed class DateTimeConverter : SyncTypeConverter<DateTime>
    {
        protected override TypeConverterResult<DateTime> ConvertSync(string value, CommandContext context)
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return TypeConverterResult<DateTime>.FromSuccess(result);
            }
            return TypeConverterResult<DateTime>.FromError($"Unable to parse '{value}' as a date/time.");
        }
    }

    /// <summary>
    /// TimeSpan converter.
    /// </summary>
    internal sealed class TimeSpanConverter : SyncTypeConverter<TimeSpan>
    {
        protected override TypeConverterResult<TimeSpan> ConvertSync(string value, CommandContext context)
        {
            if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<TimeSpan>.FromSuccess(result);
            }
            return TypeConverterResult<TimeSpan>.FromError($"Unable to parse '{value}' as a time span. Try formats like '1:30:00' or '2.5:30:00'.");
        }
    }

    /// <summary>
    /// User converter (converts snowflake ID to User entity).
    /// </summary>
    internal sealed class UserConverter : SyncTypeConverter<User>
    {
        protected override TypeConverterResult<User> ConvertSync(string value, CommandContext context)
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
            {
                // Try to get user from cache
                var user = context.Client.Cache.GetUser(userId);
                if (user != null)
                {
                    return TypeConverterResult<User>.FromSuccess(user);
                }
                
                // Create a minimal user object with the ID
                return TypeConverterResult<User>.FromSuccess(new User { Id = userId, Username = value, Discriminator = "0000" });
            }
            return TypeConverterResult<User>.FromError($"Unable to parse '{value}' as a user ID.");
        }
    }

    /// <summary>
    /// Decimal converter.
    /// </summary>
    internal sealed class DecimalConverter : SyncTypeConverter<decimal>
    {
        protected override TypeConverterResult<decimal> ConvertSync(string value, CommandContext context)
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<decimal>.FromSuccess(result);
            }
            return TypeConverterResult<decimal>.FromError($"Unable to parse '{value}' as a decimal.");
        }
    }

    /// <summary>
    /// Guid converter.
    /// </summary>
    internal sealed class GuidConverter : SyncTypeConverter<Guid>
    {
        protected override TypeConverterResult<Guid> ConvertSync(string value, CommandContext context)
        {
            if (Guid.TryParse(value, out var result))
            {
                return TypeConverterResult<Guid>.FromSuccess(result);
            }
            return TypeConverterResult<Guid>.FromError($"Unable to parse '{value}' as a GUID.");
        }
    }

    /// <summary>
    /// Uri converter.
    /// </summary>
    internal sealed class UriConverter : SyncTypeConverter<Uri>
    {
        protected override TypeConverterResult<Uri> ConvertSync(string value, CommandContext context)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var result))
            {
                return TypeConverterResult<Uri>.FromSuccess(result);
            }
            return TypeConverterResult<Uri>.FromError($"Unable to parse '{value}' as a URL.");
        }
    }

    /// <summary>
    /// DateTimeOffset converter.
    /// </summary>
    internal sealed class DateTimeOffsetConverter : SyncTypeConverter<DateTimeOffset>
    {
        protected override TypeConverterResult<DateTimeOffset> ConvertSync(string value, CommandContext context)
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return TypeConverterResult<DateTimeOffset>.FromSuccess(result);
            }
            return TypeConverterResult<DateTimeOffset>.FromError($"Unable to parse '{value}' as a date/time offset.");
        }
    }

    /// <summary>
    /// Enum converter (generic base for enum types).
    /// </summary>
    internal sealed class EnumConverter : SyncTypeConverter<Enum>
    {
        protected override TypeConverterResult<Enum> ConvertSync(string value, CommandContext context)
        {
            // This is a fallback converter - specific enum types should be handled by the generic enum converter below
            return TypeConverterResult<Enum>.FromError($"Enum conversion requires specific enum type. Use the generic enum converter for your specific enum type.");
        }
    }

    /// <summary>
    /// Generic enum converter for specific enum types.
    /// </summary>
    internal sealed class GenericEnumConverter<T> : SyncTypeConverter<T> where T : struct, Enum
    {
        protected override TypeConverterResult<T> ConvertSync(string value, CommandContext context)
        {
            // Try to parse by name (case-insensitive)
            if (Enum.TryParse<T>(value, true, out var result))
            {
                return TypeConverterResult<T>.FromSuccess(result);
            }

            // Try to parse by numeric value
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                if (Enum.IsDefined(typeof(T), intValue))
                {
                    return TypeConverterResult<T>.FromSuccess((T)Enum.ToObject(typeof(T), intValue));
                }
            }

            var validValues = string.Join(", ", Enum.GetNames<T>());
            return TypeConverterResult<T>.FromError($"Unable to parse '{value}' as {typeof(T).Name}. Valid values: {validValues}");
        }
    }

    /// <summary>
    /// Channel converter (converts snowflake ID to Channel entity).
    /// </summary>
    internal sealed class ChannelConverter : SyncTypeConverter<Channel>
    {
        protected override TypeConverterResult<Channel> ConvertSync(string value, CommandContext context)
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var channelId))
            {
                // Try to get channel from cache
                var channel = context.Client.Cache.GetChannel(channelId);
                if (channel != null)
                {
                    return TypeConverterResult<Channel>.FromSuccess(channel);
                }
                
                // Create a minimal channel object with the ID
                return TypeConverterResult<Channel>.FromSuccess(new Channel { Id = channelId, Name = value });
            }
            return TypeConverterResult<Channel>.FromError($"Unable to parse '{value}' as a channel ID.");
        }
    }

    /// <summary>
    /// Role converter (converts snowflake ID to Role entity).
    /// </summary>
    internal sealed class RoleConverter : SyncTypeConverter<Role>
    {
        protected override TypeConverterResult<Role> ConvertSync(string value, CommandContext context)
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var roleId))
            {
                // Try to get role from cache
                if (context.GuildId.HasValue)
                {
                    var guild = context.Client.Cache.GetGuild(context.GuildId.Value);
                    if (guild != null && guild.Roles != null)
                    {
                        var role = guild.Roles.FirstOrDefault(r => r.Id == roleId);
                        if (role != null)
                        {
                            return TypeConverterResult<Role>.FromSuccess(role);
                        }
                    }
                }
                
                // Create a minimal role object with the ID
                return TypeConverterResult<Role>.FromSuccess(new Role { Id = roleId, Name = value });
            }
            return TypeConverterResult<Role>.FromError($"Unable to parse '{value}' as a role ID.");
        }
    }

    /// <summary>
    /// GuildMember converter (converts snowflake ID to GuildMember entity).
    /// </summary>
    internal sealed class GuildMemberConverter : SyncTypeConverter<GuildMember>
    {
        protected override TypeConverterResult<GuildMember> ConvertSync(string value, CommandContext context)
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
            {
                // Try to get member from cache
                if (context.GuildId.HasValue)
                {
                    var member = context.Client.Cache.GetGuildMember(context.GuildId.Value, userId);
                    if (member != null)
                    {
                        return TypeConverterResult<GuildMember>.FromSuccess(member);
                    }
                }
                
                // Try to get user and create a minimal member
                var user = context.Client.Cache.GetUser(userId);
                if (user != null)
                {
                    return TypeConverterResult<GuildMember>.FromSuccess(new GuildMember { User = user });
                }
                
                return TypeConverterResult<GuildMember>.FromError($"Unable to resolve guild member for user ID '{value}'.");
            }
            return TypeConverterResult<GuildMember>.FromError($"Unable to parse '{value}' as a user ID.");
        }
    }
}
