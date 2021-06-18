using System.Collections.Generic;
using System.Globalization;
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
        if (number >= 1000000000 || number <= -1000000000)
        {
            return withSuffix ?
                string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate("BILLION_ABBREVIATION"),
                    number.ToString("0,,,.###", CultureInfo.CurrentCulture)) :
                number.ToString("0,,,.###", CultureInfo.CurrentCulture);
        }

        if (number >= 1000000 || number <= -1000000)
        {
            return withSuffix ?
                string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate("MILLION_ABBREVIATION"),
                    number.ToString("0,,.##", CultureInfo.CurrentCulture)) :
                number.ToString("0,,.##", CultureInfo.CurrentCulture);
        }

        if (number >= 1000 || number <= -1000)
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
    ///   Splits string into different chunks by whitespace.
    /// </summary>
    /// <param name="input">String to split.</param>
    /// <param name="ignoreWithinQuotes">Ignore whitespaces within quotation marks.</param>
    public static string[] SplitByWhiteSpace(string input, bool ignoreWithinQuotes)
    {
        var result = new List<string>();

        var cutPoint = 0;
        var validToSplit = true;

        for (var i = 0; i < input.Length; ++i)
        {
            var character = input[i];

            if (character == '"' && validToSplit && ignoreWithinQuotes)
            {
                validToSplit = false;
            }
            else if (character == '"' && !validToSplit)
            {
                validToSplit = true;
            }

            if (character == ' ' && validToSplit)
            {
                if ((i == 0) || (i + 1 < input.Length && input[i + 1] == ' '))
                {
                    cutPoint++;
                    continue;
                }

                result.Add(input.Substr(cutPoint, i - cutPoint));
                cutPoint = i + 1;
            }

            // Reached end of string, add the rest of it from last cut point (whitespace)
            if (i == input.Length - 1)
                result.Add(input.Substr(cutPoint, i - cutPoint + 1));
        }

        return result.ToArray();
    }
}
