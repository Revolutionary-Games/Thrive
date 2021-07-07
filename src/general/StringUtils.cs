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

    public static string FormatDecimal(this double number, int decimalPlaces)
    {
        var format = "0.";
        for (var i = 0; i < decimalPlaces; i++)
        {
            format += "0";
        }

        return number.ToString(format, CultureInfo.CurrentCulture);
    }
}
