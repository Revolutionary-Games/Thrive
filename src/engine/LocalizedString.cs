using System;
using System.Globalization;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   String that can be localized on demand for different locales.
///   This class caches the translationKey.
///   ToString returns the translated text for the current locale.
/// </summary>
/// <remarks>
///   This class can be used on its own, but was designed for the use within LocalizedStringBuilder.
/// </remarks>
[JSONDynamicTypeAllowed]
public class LocalizedString : IFormattable
{
    [JsonProperty]
    private string translationKey;

    [JsonProperty]
    private object[] formatStringArgs;

    public LocalizedString(string translationKey)
        : this(translationKey, null)
    {
    }

    [JsonConstructor]
    public LocalizedString(string translationKey, params object[] formatStringArgs)
    {
        this.translationKey = translationKey;
        this.formatStringArgs = formatStringArgs;
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        if (formatStringArgs == null || formatStringArgs.Length == 0)
        {
            return format ?? TranslationServer.Translate(translationKey);
        }

        return string.Format(formatProvider ?? CultureInfo.CurrentCulture,
            format ?? TranslationServer.Translate(translationKey), formatStringArgs);
    }
}
