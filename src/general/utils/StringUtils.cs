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
    public const int INDENT_AMOUNT = 4;

    /// <summary>
    ///   Truncates large numbers with suffix added (e.g. M for million).
    ///   Adapted from https://stackoverflow.com/a/30181106 to allow negatives and translation.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: see if it would be possible to make a variant of this method that writes to a StringBuilder
    ///   </para>
    /// </remarks>
    public static string FormatNumber(this double number, bool withSuffix = true)
    {
        if (number is >= 1000000000 or <= -1000000000)
        {
            return withSuffix ?
                Localization.Translate("BILLION_ABBREVIATION")
                    .FormatSafe(number.ToString("0,,,.###", CultureInfo.CurrentCulture)) :
                number.ToString("0,,,.###", CultureInfo.CurrentCulture);
        }

        if (number is >= 1000000 or <= -1000000)
        {
            return withSuffix ?
                Localization.Translate("MILLION_ABBREVIATION")
                    .FormatSafe(number.ToString("0,,.##", CultureInfo.CurrentCulture)) :
                number.ToString("0,,.##", CultureInfo.CurrentCulture);
        }

        if (number is >= 1000 or <= -1000)
        {
            return withSuffix ?
                Localization.Translate("KILO_ABBREVIATION")
                    .FormatSafe(number.ToString("0,.#", CultureInfo.CurrentCulture)) :
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
    ///   Safely formats a string where the format is loaded form a translation
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This should always be used instead of <see cref="string.Format(string,object)"/>
    ///   </para>
    /// </remarks>
    /// <param name="format">The format to use</param>
    /// <param name="formatArguments">Arguments to pass to the formatting</param>
    /// <returns>
    ///   The formatted string or <see cref="format"/> if the placeholders were incorrect in the translation
    /// </returns>
    public static string FormatSafe(this string format, params object?[] formatArguments)
    {
        try
        {
            return string.Format(CultureInfo.CurrentCulture, format, formatArguments);
        }
        catch (FormatException e)
        {
            GD.PrintErr($"Invalid translation format for current language in text: {format} exception: {e}");
            return format;
        }
    }

    /// <summary>
    ///   Splits string into different chunks by whitespace.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Only handles a single whitespace ( ) and not tabs, multiple spaces, etc. For quoted
    ///     substrings handling, this only considers double quotes (") and not apostrophes (').
    ///     If there is no closing quote after an opening quote, the rest of string is considered within quotes.
    ///   </para>
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

    /// <summary>
    ///   Convert a value in a string with up to 3 digits.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If greater than 100, it should use F0, otherwise it should use F1.
    ///     Above 1000 it should use the number formatting which uses K for
    ///     thousands, M for millions etc. It can handle negative numbers
    ///   </para>
    /// </remarks>
    /// <param name="value">The value to format</param>
    /// <returns>The formatted string</returns>
    public static string ThreeDigitFormat(double value)
    {
        if (value is >= 1000 or <= -1000)
            return FormatNumber(value);

        if (value is >= 100 or <= -100)
            return value.ToString("F0", CultureInfo.CurrentCulture);

        return value.ToString("F1", CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///   Format variant that writes the result to a <see cref="StringBuilder"/>
    /// </summary>
    public static void ThreeDigitFormat(double value, StringBuilder result)
    {
        if (value is >= 1000 or <= -1000)
        {
            result.Append(FormatNumber(value));
            return;
        }

        if (value is >= 100 or <= -100)
        {
            result.Append(value.ToString("F0", CultureInfo.CurrentCulture));
            return;
        }

        result.Append(value.ToString("F1", CultureInfo.CurrentCulture));
    }

    public static string ThreeDigitFormat(long value)
    {
        if (value is >= 1000 or <= -1000)
            return FormatNumber(value);

        return value.ToString(CultureInfo.CurrentCulture);
    }

    public static string FormatPositiveWithLeadingPlus(long value)
    {
        if (value < 0)
            return value.ToString(CultureInfo.CurrentCulture);

        return '+' + value.ToString(CultureInfo.CurrentCulture);
    }

    public static string FormatPositiveWithLeadingPlus(float value)
    {
        // This check works better than "< 0" as this handles negative zero
        if (float.IsNegative(value))
            return value.ToString(CultureInfo.CurrentCulture);

        return '+' + value.ToString(CultureInfo.CurrentCulture);
    }

    public static string FormatPositiveWithLeadingPlus(string formatted, long value)
    {
        if (value < 0)
            return formatted;

        return '+' + formatted;
    }

    public static string FormatPositiveWithLeadingPlus(string formatted, double value)
    {
        if (double.IsNegative(value))
            return formatted;

        return '+' + formatted;
    }

    /// <summary>
    ///   Formats two numbers separated by a slash. The numbers will have
    ///   up to 3 digits each.
    /// </summary>
    /// <param name="numerator">The first number (numerator)</param>
    /// <param name="denominator">The second number (denominator)</param>
    /// <returns>The formatted string</returns>
    public static string SlashSeparatedNumbersFormat(double numerator, double denominator)
    {
        return ThreeDigitFormat(numerator) + " / " + ThreeDigitFormat(denominator);
    }

    public static string SlashSeparatedNumbersFormat(long numerator, long denominator)
    {
        return ThreeDigitFormat(numerator) + " / " + ThreeDigitFormat(denominator);
    }

    /// <summary>
    ///   Variant of this method that directly puts the result into a <see cref="StringBuilder"/>
    /// </summary>
    public static void SlashSeparatedNumbersFormat(double numerator, double denominator, StringBuilder result)
    {
        ThreeDigitFormat(numerator, result);
        result.Append(" / ");
        ThreeDigitFormat(denominator, result);
    }

    /// <summary>
    ///   Returns a suffix for a planet given which number of planet it is
    /// </summary>
    /// <param name="index">The index of the planet</param>
    /// <param name="capitalize">When true the names are capitalized</param>
    /// <param name="realLatin">
    ///   When true real latin words are used instead of the slightly butchered names like "prime"
    /// </param>
    /// <param name="switchToNumeralsAfter">
    ///   Highest value to show as fully typed out before switching to roman numerals
    /// </param>
    /// <returns>A name suffix</returns>
    public static string NameIndexSuffix(int index, bool capitalize = true, bool realLatin = true,
        int switchToNumeralsAfter = Constants.NAMING_SWITCH_TO_ROMAN_NUMERALS_AFTER)
    {
        ++index;

        if (index > switchToNumeralsAfter)
            return FormatAsRomanNumerals(index);

        // ReSharper disable StringLiteralTypo

        // The switches are a bit long and repetitive here but that's done here to avoid having to modify the string
        // in any way which would be slower at runtime
        if (!realLatin)
        {
            // It was hard to find a number sequence with prime in it so there's just a few tweaked non-real latin
            // forms here, otherwise we use the real latin words
            if (capitalize)
            {
                switch (index)
                {
                    case 1:
                        return "Prime";
                    case 2:
                        return "Secundus";
                    case 3:
                        return "Tertiam";
                    case 4:
                        return "Quaternam";
                }
            }
            else
            {
                switch (index)
                {
                    case 1:
                        return "prime";
                    case 2:
                        return "secundus";
                    case 3:
                        return "tertiam";
                    case 4:
                        return "quaternam";
                }
            }
        }

        if (capitalize)
        {
            switch (index)
            {
                case 1:
                    return "Primus";
                case 2:
                    return "Secundus";
                case 3:
                    return "Tertius";
                case 4:
                    return "Quartus";
                case 5:
                    return "Quintus";
                case 6:
                    return "Sextus";
                case 7:
                    return "Septimus";
                case 8:
                    return "Octavus";
                case 9:
                    return "Nonus";
                case 10:
                    return "Decimus";
                case 11:
                    return "Undecimus";
                case 12:
                    return "Duodecimus";
                case 13:
                    return "Tertius decimus";
                case 14:
                    return "Quartus decimus";
                case 15:
                    return "Quintus decimus";
                case 16:
                    return "Sextus decimus";
                case 17:
                    return "Septimus decimus";
                case 18:
                    return "Duodevicesimus";
                case 19:
                    return "Undevicesimus";
                case 20:
                    return "Vicesimus";
            }
        }
        else
        {
            switch (index)
            {
                case 1:
                    return "primus";
                case 2:
                    return "secundus";
                case 3:
                    return "tertius";
                case 4:
                    return "quartus";
                case 5:
                    return "quintus";
                case 6:
                    return "sextus";
                case 7:
                    return "septimus";
                case 8:
                    return "octavus";
                case 9:
                    return "nonus";
                case 10:
                    return "decimus";
                case 11:
                    return "undecimus";
                case 12:
                    return "duodecimus";
                case 13:
                    return "tertius decimus";
                case 14:
                    return "quartus decimus";
                case 15:
                    return "quintus decimus";
                case 16:
                    return "sextus decimus";
                case 17:
                    return "septimus decimus";
                case 18:
                    return "duodevicesimus";
                case 19:
                    return "undevicesimus";
                case 20:
                    return "vicesimus";
            }
        }

        // ReSharper restore StringLiteralTypo

        // Fallback to roman numerals
        return FormatAsRomanNumerals(index);
    }

    public static string FormatAsRomanNumerals(int number)
    {
        if (number == 0)
        {
            // Romans don't use a zero, but we don't want to throw an exception here so just returning 0 is done
            return "0";
        }

        var builder = new StringBuilder(10);

        if (number < 0)
        {
            // This also isn't a thing in roman numerals, but we should support this
            builder.Append('-');

            number *= -1;
        }

        while (number >= 1000)
        {
            number -= 1000;
            builder.Append('M');
        }

        if (number >= 900)
        {
            number -= 900;
            builder.Append("CM");
        }

        while (number >= 500)
        {
            number -= 500;
            builder.Append('D');
        }

        if (number >= 400)
        {
            number -= 400;
            builder.Append("CD");
        }

        while (number >= 100)
        {
            number -= 100;
            builder.Append('C');
        }

        if (number >= 90)
        {
            number -= 90;
            builder.Append("XC");
        }

        while (number >= 50)
        {
            number -= 50;
            builder.Append('L');
        }

        if (number >= 40)
        {
            number -= 40;
            builder.Append("XL");
        }

        while (number >= 10)
        {
            number -= 10;
            builder.Append('X');
        }

        if (number >= 9)
        {
            number -= 9;
            builder.Append("IX");
        }

        while (number >= 5)
        {
            number -= 5;
            builder.Append('V');
        }

        if (number >= 4)
        {
            number -= 4;
            builder.Append("IV");
        }

        while (number > 0)
        {
            --number;
            builder.Append('I');
        }

        return builder.ToString();
    }

    public static string GetIndent(int indentLevel)
    {
        if (indentLevel < 1)
            return string.Empty;

        return new string(' ', indentLevel * INDENT_AMOUNT);
    }

    public static int DetectLineIndentationLevel(string line)
    {
        int spaceCount = 0;

        foreach (var character in line)
        {
            if (character <= ' ')
            {
                ++spaceCount;
            }
            else
            {
                break;
            }
        }

        return spaceCount;
    }

    /// <summary>
    ///   Makes a string safe to store as a meta tag in rich text.
    /// </summary>
    public static string ConvertToSafeMetaTag(string tagContent)
    {
        return tagContent
            .Replace("[", "LEFT_BRACE").Replace("]", "RIGHT_BRACE")
            .Replace("{", "LEFT_CURLY_BRACE").Replace("}", "RIGHT_CURLY_BRACE");
    }

    /// <summary>
    ///   Gets the original text from the value of a meta tag.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Assumes the text doesn't contain the literal strings LEFT_BRACE, RIGHT_BRACE,
    ///     LEFT_CURLY_BRACE or RIGHT_CURLY_BRACE.
    ///   </para>
    /// </remarks>
    public static string ConvertFromSafeMetaTag(string tagContent)
    {
        return tagContent
            .Replace("LEFT_BRACE", "[").Replace("RIGHT_BRACE", "]")
            .Replace("LEFT_CURLY_BRACE", "{").Replace("RIGHT_CURLY_BRACE", "}");
    }
}
