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
            // Developer note: Provide context about valid range and format
            return TypeConverterResult<int>.FromError($"Unable to parse '{value}' as an integer. Valid range: -2,147,483,648 to 2,147,483,647. Ensure the input contains only digits and an optional leading minus sign.");
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
            // Developer note: Provide context about valid range for Discord IDs
            return TypeConverterResult<long>.FromError($"Unable to parse '{value}' as a long integer. Valid range: -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807. For Discord IDs, use ulong instead.");
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
            // Developer note: Discord snowflake IDs are always positive 64-bit integers
            return TypeConverterResult<ulong>.FromError($"Unable to parse '{value}' as a snowflake ID. Discord IDs are positive 64-bit integers (0 to 18,446,744,073,709,551,615). Ensure the input contains only digits, no minus sign or decimal point.");
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
            // Developer note: List all accepted values for clarity
            return TypeConverterResult<bool>.FromError($"Unable to parse '{value}' as a boolean. Accepted values (case-insensitive): true/yes/1/y for true, false/no/0/n for false.");
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
            // Developer note: Specify format requirements
            return TypeConverterResult<double>.FromError($"Unable to parse '{value}' as a number. Use invariant culture format (dot as decimal separator, no thousands separators). Example: 3.14 or -0.5.");
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
            // Developer note: Specify format and precision requirements
            return TypeConverterResult<float>.FromError($"Unable to parse '{value}' as a number. Use invariant culture format (dot as decimal separator). Single precision range: ±1.5e-45 to ±3.4e38.");
        }
    }

    /// <summary>
    /// SByte converter.
    /// </summary>
    internal sealed class SByteConverter : SyncTypeConverter<sbyte>
    {
        protected override TypeConverterResult<sbyte> ConvertSync(string value, CommandContext context)
        {
            if (sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<sbyte>.FromSuccess(result);
            }
            return TypeConverterResult<sbyte>.FromError($"Unable to parse '{value}' as an sbyte.");
        }
    }

    /// <summary>
    /// Byte converter.
    /// </summary>
    internal sealed class ByteConverter : SyncTypeConverter<byte>
    {
        protected override TypeConverterResult<byte> ConvertSync(string value, CommandContext context)
        {
            if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<byte>.FromSuccess(result);
            }
            return TypeConverterResult<byte>.FromError($"Unable to parse '{value}' as a byte.");
        }
    }

    /// <summary>
    /// Int16 converter.
    /// </summary>
    internal sealed class Int16Converter : SyncTypeConverter<short>
    {
        protected override TypeConverterResult<short> ConvertSync(string value, CommandContext context)
        {
            if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<short>.FromSuccess(result);
            }
            return TypeConverterResult<short>.FromError($"Unable to parse '{value}' as a short.");
        }
    }

    /// <summary>
    /// UInt16 converter.
    /// </summary>
    internal sealed class UInt16Converter : SyncTypeConverter<ushort>
    {
        protected override TypeConverterResult<ushort> ConvertSync(string value, CommandContext context)
        {
            if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<ushort>.FromSuccess(result);
            }
            return TypeConverterResult<ushort>.FromError($"Unable to parse '{value}' as a ushort.");
        }
    }

    /// <summary>
    /// UInt32 converter.
    /// </summary>
    internal sealed class UInt32Converter : SyncTypeConverter<uint>
    {
        protected override TypeConverterResult<uint> ConvertSync(string value, CommandContext context)
        {
            if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return TypeConverterResult<uint>.FromSuccess(result);
            }
            return TypeConverterResult<uint>.FromError($"Unable to parse '{value}' as a uint.");
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
            // Developer note: Provide comprehensive format examples
            return TypeConverterResult<TimeSpan>.FromError($"Unable to parse '{value}' as a time span. Accepted formats: '1:30:00' (hours:minutes:seconds), '2.5:30:00' (days.hours:minutes:seconds), '1.5h' (ISO 8601 duration), or '00:30:00' (standard time format).");
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
                
                // Developer note: Explain why user not found and suggest alternatives
                return TypeConverterResult<User>.FromError($"User with ID '{value}' not found in cache. Ensure the user is in a shared guild or has been cached. Try mentioning the user (@username) instead of providing the ID directly.");
            }
            // Developer note: Suggest mention format
            return TypeConverterResult<User>.FromError($"Unable to parse '{value}' as a user ID. Provide a valid snowflake ID (numeric string) or mention the user with @username.");
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
            // Developer note: Provide both name and numeric value options
            return TypeConverterResult<T>.FromError($"Unable to parse '{value}' as {typeof(T).Name}. Valid values (case-insensitive): {validValues}. You can also use the numeric value of the enum member.");
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
                
                // Note: Channel not in cache, returning minimal object with ID only
                return TypeConverterResult<Channel>.FromSuccess(new Channel { Id = channelId, Name = value });
            }
            // Developer note: Suggest channel mention format
            return TypeConverterResult<Channel>.FromError($"Unable to parse '{value}' as a channel ID. Provide a valid snowflake ID or mention the channel with #channel-name.");
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
                
                // Note: Role not found in cache or missing guild context
                return TypeConverterResult<Role>.FromSuccess(new Role { Id = roleId, Name = value });
            }
            // Developer note: Suggest role mention format
            return TypeConverterResult<Role>.FromError($"Unable to parse '{value}' as a role ID. Provide a valid snowflake ID or mention the role with @role-name. Note: Role conversion requires guild context.");
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
                    
                    // Note: Guild member not in cache, falling back to user lookup
                    var user = context.Client.Cache.GetUser(userId);
                    if (user != null)
                    {
                        return TypeConverterResult<GuildMember>.FromSuccess(new GuildMember { User = user });
                    }
                
                // Developer note: Provide detailed failure explanation
                return TypeConverterResult<GuildMember>.FromError($"Unable to resolve guild member for user ID '{value}'. Ensure the user is in the guild and the guild member list is cached. Try mentioning the user (@username) instead.");
            }
            // Developer note: Suggest user mention format
            return TypeConverterResult<GuildMember>.FromError($"Unable to parse '{value}' as a user ID. Provide a valid snowflake ID or mention the user with @username. Note: Guild member conversion requires guild context.");
        }
    }
}
