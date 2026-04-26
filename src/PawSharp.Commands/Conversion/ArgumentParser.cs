#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace PawSharp.Commands.Conversion;

/// <summary>
/// Advanced argument parser with quote and escape character support.
/// </summary>
public static class ArgumentParser
{
    /// <summary>
    /// Parses a command argument string into individual arguments, respecting quotes and escape characters.
    /// </summary>
    /// <param name="input">The raw argument string.</param>
    /// <returns>A list of parsed arguments.</returns>
    public static List<string> ParseArguments(string input)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var escapeNext = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (escapeNext)
            {
                current.Append(c);
                escapeNext = false;
                continue;
            }

            switch (c)
            {
                case '\\':
                    escapeNext = true;
                    break;

                case '"':
                    inQuotes = !inQuotes;
                    break;

                case ' ':
                case '\t':
                    if (inQuotes)
                    {
                        current.Append(c);
                    }
                    else if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    break;

                default:
                    current.Append(c);
                    break;
            }
        }

        // Add the last argument if present
        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    /// <summary>
    /// Extracts the command name and raw arguments from a message content.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="prefix">The command prefix.</param>
    /// <returns>A tuple containing the command name and raw arguments.</returns>
    public static (string commandName, string rawArguments) ExtractCommand(string content, string prefix)
    {
        if (!content.StartsWith(prefix))
            return (string.Empty, string.Empty);

        var contentWithoutPrefix = content.Substring(prefix.Length);
        var parts = contentWithoutPrefix.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return (string.Empty, string.Empty);

        var commandName = parts[0];
        var rawArguments = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        return (commandName, rawArguments);
    }
}
