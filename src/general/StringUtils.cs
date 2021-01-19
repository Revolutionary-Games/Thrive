using System.Globalization;
using Godot;

/// <summary>
///   Helper for any string related stuff.
/// </summary>
public static class StringUtils
{
    /// <summary>
    ///   Truncates large numbers with suffix added (e.g. M for million).
    ///   Adapted from https://stackoverflow.com/a/30181106 to allow negatives.
    /// </summary>
    public static string FormatNumber(this double number, bool withSuffix = true)
    {
        if (number >= 1000000000 || number <= -1000000000)
        {
            return number.ToString("0,,,.###", CultureInfo.CurrentCulture) +
                (withSuffix ? TranslationServer.Translate("BILLION_ABBREVIATION") : string.Empty);
        }

        if (number >= 1000000 || number <= -1000000)
        {
            return number.ToString("0,,.##", CultureInfo.CurrentCulture) +
                (withSuffix ? TranslationServer.Translate("MILLION_ABBREVIATION") : string.Empty);
        }

        if (number >= 1000 || number <= -1000)
        {
            return number.ToString("0,.#", CultureInfo.CurrentCulture) +
                (withSuffix ? TranslationServer.Translate("KILO_ABBREVIATION") : string.Empty);
        }

        return number.ToString("0.#", CultureInfo.CurrentCulture);
    }
}
