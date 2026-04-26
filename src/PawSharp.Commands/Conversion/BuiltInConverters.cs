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
}
