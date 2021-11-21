using System;
using System.Globalization;
using Godot;
using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public class TranslatingString : IFormattable
{
    [JsonProperty]
    private string translationKey;

    [JsonProperty]
    private object[] formatStringArgs;

    public TranslatingString(string translationKey)
        : this(translationKey, null)
    {
    }

    [JsonConstructor]
    public TranslatingString(string translationKey, params object[] formatStringArgs)
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
        return formatStringArgs == null || formatStringArgs.Length == 0 ?
            format ?? TranslationServer.Translate(translationKey) :
            string.Format(formatProvider ?? CultureInfo.CurrentCulture,
                format ?? TranslationServer.Translate(translationKey), formatStringArgs);
    }
}
