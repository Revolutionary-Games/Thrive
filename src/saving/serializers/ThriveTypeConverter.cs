namespace Saving.Serializers;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

/// <summary>
///   Converts dictionary keys to / from json
/// </summary>
/// <remarks>
///   <para>
///     Please see the caveats mentioned on <see cref="ThriveJsonConverter.DeserializeObjectDynamic"/> about what this
///     can and can't convert properly. For an example type that uses this, see <see cref="Species"/> and
///     <see cref="Patch"/> which contains a dictionary of species.
///   </para>
/// </remarks>
public class ThriveTypeConverter : TypeConverter
{
    private static readonly Type StringType = typeof(string);

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == StringType;
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        if (destinationType == null)
            throw new ArgumentNullException(nameof(destinationType));

        return destinationType == StringType || destinationType.CustomAttributes.Any(
            a => a.AttributeType == typeof(UseThriveConverterAttribute));
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        if (value == null)
            return null;

        // Must have the dynamic type used on the object, otherwise this doesn't do many sensible things
        return ThriveJsonConverter.Instance.DeserializeObjectDynamic((string)value);
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
        Type destinationType)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (destinationType == StringType)
        {
            var type = value.GetType();

            if (type.CustomAttributes.Any(a =>
                    a.AttributeType == typeof(JSONAlwaysDynamicTypeAttribute) ||
                    a.AttributeType == typeof(JSONDynamicTypeAllowedAttribute)))
            {
                type = type.BaseType;
            }

            return ThriveJsonConverter.Instance.SerializeObject(value, type);
        }

        throw new NotSupportedException();
    }
}
