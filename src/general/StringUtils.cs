using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;

/// <summary>
///   Helpers for any string related stuff.
/// </summary>
public static class StringUtils
{
    /// <summary>
    ///   Truncates large numbers with suffix added (e.g. M for million).
    ///   Adapted from https://stackoverflow.com/a/30181106 to allow negatives and translation.
    /// </summary>
    public static string FormatNumber(this double number, bool withSuffix = true)
    {
        if (number is >= 1000000000 or <= -1000000000)
        {
            return withSuffix ?
                string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate("BILLION_ABBREVIATION"),
                    number.ToString("0,,,.###", CultureInfo.CurrentCulture)) :
                number.ToString("0,,,.###", CultureInfo.CurrentCulture);
        }

        if (number is >= 1000000 or <= -1000000)
        {
            return withSuffix ?
                string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate("MILLION_ABBREVIATION"),
                    number.ToString("0,,.##", CultureInfo.CurrentCulture)) :
                number.ToString("0,,.##", CultureInfo.CurrentCulture);
        }

        if (number is >= 1000 or <= -1000)
        {
            return withSuffix ?
                string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate("KILO_ABBREVIATION"),
                    number.ToString("0,.#", CultureInfo.CurrentCulture)) :
                number.ToString("0,.#", CultureInfo.CurrentCulture);
        }

        return number.ToString("0.#", CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///   Truncates large numbers with suffix added (e.g. M for million).
    ///   Adapted from https://stackoverflow.com/a/30181106 to allow negatives and translation.
    /// </summary>
    public static string FormatNumber(this long number, bool withSuffix = true)
    {
        return ((double)number).FormatNumber();
    }

    /// <summary>
    ///   Splits string into different chunks by whitespace.
    /// </summary>
    /// <remarks>
    ///   Only handles a single whitespace ( ) and not tabs, multiple spaces, etc. For quoted
    ///   substrings handling, this only considers double quotes (") and not apostrophes (').
    ///   If there is no closing quote after an opening quote, the rest of string is considered within quotes.
    /// </remarks>
    /// <param name="input">String to split.</param>
    /// <param name="ignoreWithinQuotes">Ignore whitespace within quotation marks.</param>
    /// <returns>A list of the substrings, starting from the beginning of the input string.</returns>
    public static List<string> SplitByWhitespace(string input, bool ignoreWithinQuotes)
    {
        var result = new List<string>();

        var cutPosition = 0;
        var insideQuote = false;

        for (var i = 0; i < input.Length; ++i)
        {
            var character = input[i];

            var isAnEscapeSequence = i > 0 && input[i - 1] == '\\';

            if (character == '"' && !isAnEscapeSequence && !insideQuote && ignoreWithinQuotes)
            {
                insideQuote = true;
            }
            else if (character == '"' && !isAnEscapeSequence && insideQuote)
            {
                insideQuote = false;
            }

            if (character == ' ' && !insideQuote)
            {
                if ((i == 0) || (i + 1 < input.Length && input[i + 1] == ' '))
                {
                    cutPosition++;
                    continue;
                }

                result.Add(input.Substring(cutPosition, i - cutPosition).Unescape());
                cutPosition = i + 1;
            }

            // Reached end of string, add the rest of it from last cut point (whitespace)
            if (i == input.Length - 1)
                result.Add(input.Substring(cutPosition, i - cutPosition + 1).Unescape());
        }

        return result;
    }

    /// <summary>
    ///   Parses a list of "key=value" pairs into a dictionary. Overrides duplicate keys with newer ones.
    /// </summary>
    /// <returns>
    ///   A dictionary of key and value string pairs collected from input. If input string list is null,
    ///   the return value is an empty dictionary.
    /// </returns>
    public static Dictionary<string, string> ParseKeyValuePairs(List<string>? input)
    {
        var result = new Dictionary<string, string>();

        if (input == null)
            return result;

        foreach (var entry in input)
        {
            if (entry.IndexOf("=", StringComparison.InvariantCulture) == -1)
                continue;

            var split = entry.Split("=");

            if (split.Length != 2)
                continue;

            result[split[0]] = split[1];
        }

        return result;
    }

    /// <summary>
    ///   Checks and returns true if the input string starts and ends with the given string.
    /// </summary>
    public static bool StartsAndEndsWith(this string input, string what)
    {
        return input.StartsWith(what, StringComparison.InvariantCulture) &&
            input.EndsWith(what, StringComparison.InvariantCulture);
    }

    /// <summary>
    ///   Converts any escaped characters in the input string.
    /// </summary>
    /// <returns>A string with any escaped characters replaced by their unescaped form.</returns>
    public static string Unescape(this string input)
    {
        var result = new StringBuilder(input);

        result.Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\v", "\v")
            .Replace("\\'", "\'")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");

        return result.ToString();
    }
}
