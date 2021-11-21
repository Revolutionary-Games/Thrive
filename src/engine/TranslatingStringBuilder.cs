using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public class TranslatingStringBuilder : IFormattable
{
    [JsonProperty]
    private readonly List<IFormattable> items = new List<IFormattable>();

    private readonly StringBuilder stringBuilder;
    private string formatString = string.Empty;
    private bool stringBuilderChanged;

    public TranslatingStringBuilder()
    {
        stringBuilder = new StringBuilder();
    }

    public TranslatingStringBuilder(int capacity)
    {
        stringBuilder = new StringBuilder(capacity);
    }

    [JsonProperty]
    private string FormatString
    {
        get
        {
            if (stringBuilderChanged)
            {
                formatString += stringBuilder.ToString();
                stringBuilder.Clear();
                stringBuilderChanged = false;
            }

            return formatString;
        }
        set => formatString = value ?? string.Empty;
    }

    /// <summary>
    ///   Explicit conversion from string to support backward compatibility with older saves,
    ///   where the JsonProperty was a string.
    /// </summary>
    public static explicit operator TranslatingStringBuilder(string value)
    {
        return new TranslatingStringBuilder { FormatString = value };
    }

    public TranslatingStringBuilder Append(TranslatingString translatingString)
    {
        return Append((object)translatingString);
    }

    public TranslatingStringBuilder Append(string value)
    {
        return Append((object)value);
    }

    public TranslatingStringBuilder Append(string value, params object[] args)
    {
        return Append((object)string.Format(CultureInfo.CurrentCulture, value, args));
    }

    public TranslatingStringBuilder Append(char value)
    {
        return Append((object)value);
    }

    public TranslatingStringBuilder Append(object value)
    {
        stringBuilderChanged = true;

        if (value is IFormattable formattable)
        {
            stringBuilder.Append('{');
            stringBuilder.Append(items.Count);
            stringBuilder.Append('}');
            items.Add(formattable);
            return this;
        }

        stringBuilder.Append(value);
        return this;
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return string.Format(
            formatProvider ?? CultureInfo.CurrentCulture,
            format ?? FormatString,
            items.ToArray<object>());
    }
}
