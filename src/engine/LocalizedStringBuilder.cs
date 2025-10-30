using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SharedBase.Archive;

/// <summary>
///   Like a StringBuilder but with on-demand localization.
///   Calling ToString formats all the IFormattable's to the current culture and returns the formatted string.
///   This class is json serializable as long as all IFormattable's are serializable.
/// </summary>
/// <remarks>
///   <para>
///     When adding a IFormattable(e.g. Number, or LocalizedString),
///     the object is cached in a list and a string format index ({0}) is added to the StringBuilder.
///   </para>
/// </remarks>
public class LocalizedStringBuilder : IFormattable, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Things to format into this builder. This is no longer <see cref="IFormattable"/> as string would throw an
    ///   error on JSON load.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: the above constraint should no longer be true with the archive format
    ///   </para>
    /// </remarks>
    private readonly List<object> items;

    private readonly StringBuilder stringBuilder;
    private string formatString = string.Empty;
    private bool stringBuilderChanged;

    public LocalizedStringBuilder()
    {
        stringBuilder = new StringBuilder();
        items = new List<object>();
    }

    public LocalizedStringBuilder(int capacity)
    {
        stringBuilder = new StringBuilder(capacity);
        items = new List<object>();
    }

    private LocalizedStringBuilder(List<object> items, string formatString)
    {
        this.items = items;
        this.formatString = formatString;
        stringBuilder = new StringBuilder();
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.LocalizedStringBuilder;

    public bool CanBeReferencedInArchive => false;

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

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.LocalizedStringBuilder)
            throw new NotSupportedException();

        writer.WriteObject((LocalizedStringBuilder)obj);
    }

    public static LocalizedStringBuilder ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new LocalizedStringBuilder(reader.ReadObject<List<object>>(),
            reader.ReadString() ?? throw new NullArchiveObjectException());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(items);
        writer.Write(formatString);
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
