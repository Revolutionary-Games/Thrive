using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

/// <summary>
///   Like a StringBuilder but with on demand localization.
///   Calling ToString formats all the IFormattable's to the current culture and returns the formatted string.
///   This class is json serializable as long as all IFormattable's are serializable.
/// </summary>
/// <remarks>
///   <para>
///     When adding a IFormattable(e.g. Number, or LocalizedString),
///     the object is cached in a list and a string format index ({0}) is added to the StringBuilder.
///   </para>
/// </remarks>
[JSONDynamicTypeAllowed]
public class LocalizedStringBuilder : IFormattable
{
    [JsonProperty]
    private readonly List<IFormattable> items = new();

    private readonly StringBuilder stringBuilder;
    private string formatString = string.Empty;
    private bool stringBuilderChanged;

    public LocalizedStringBuilder()
    {
        stringBuilder = new StringBuilder();
    }

    public LocalizedStringBuilder(int capacity)
    {
        stringBuilder = new StringBuilder(capacity);
    }

    [JsonProperty]
    private string? FormatString
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
    ///   where the JsonProperty was a string and now is a LocalizedStringBuilder.
    /// </summary>
    public static explicit operator LocalizedStringBuilder(string value)
    {
        return new LocalizedStringBuilder { FormatString = value };
    }

    public LocalizedStringBuilder Append(LocalizedString translatingString)
    {
        return Append((object)translatingString);
    }

    public LocalizedStringBuilder Append(string value)
    {
        return Append((object)value);
    }

    public LocalizedStringBuilder Append(string value, params object[] args)
    {
        return Append((object)value.FormatSafe(args));
    }

    public LocalizedStringBuilder Append(char value)
    {
        return Append((object)value);
    }

    public LocalizedStringBuilder Append(object value)
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

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider ?? CultureInfo.CurrentCulture,
            format ?? FormatString ?? string.Empty,
            items.ToArray<object>());
    }
}
