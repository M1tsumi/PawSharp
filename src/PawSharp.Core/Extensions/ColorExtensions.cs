namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for Discord color values.
/// </summary>
public static class ColorExtensions
{
    /// <summary>Discord's brand blurple color (0x5865F2).</summary>
    public const int DiscordBlurple = 0x5865F2;

    /// <summary>Discord's green color (0x57F287).</summary>
    public const int Green = 0x57F287;

    /// <summary>Discord's yellow color (0xFEE75C).</summary>
    public const int Yellow = 0xFEE75C;

    /// <summary>Discord's red color (0xED4245).</summary>
    public const int Red = 0xED4245;

    /// <summary>White color (0xFFFFFF).</summary>
    public const int White = 0xFFFFFF;

    /// <summary>Black color (0x000000).</summary>
    public const int Black = 0x000000;

    /// <summary>
    /// Converts a color integer to its hexadecimal string representation.
    /// </summary>
    /// <param name="color">The color integer.</param>
    /// <returns>The hexadecimal string (e.g., "5865F2").</returns>
    public static string ToHex(this int color)
    {
        return color.ToString("X6").PadLeft(6, '0');
    }

    /// <summary>
    /// Converts a color integer to its RGB component values.
    /// </summary>
    /// <param name="color">The color integer.</param>
    /// <returns>A tuple containing the R, G, and B components (0-255 each).</returns>
    public static (byte R, byte G, byte B) ToRgb(this int color)
    {
        return ((byte)((color >> 16) & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)(color & 0xFF));
    }

    /// <summary>
    /// Creates a color integer from RGB component values.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <returns>The color integer.</returns>
    public static int FromRgb(byte r, byte g, byte b)
    {
        return (r << 16) | (g << 8) | b;
    }
}
