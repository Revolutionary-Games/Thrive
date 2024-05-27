namespace Saving.Serializers;

using System;
using System.ComponentModel;
using System.Globalization;

/// <summary>
///   Handles converting plain strings into <see cref="LocalizedString"/>s for loading older saves
/// </summary>
public class LocalizedStringTypeConverter : TypeConverter
{
    private static readonly Type StringType = typeof(string);
    private static readonly Type LocalizedType = typeof(LocalizedString);

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == StringType;
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == LocalizedType;
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        if (value == null)
            return null;

        return new LocalizedString((string)value);
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
        Type destinationType)
    {
        throw new NotSupportedException();
    }
}
