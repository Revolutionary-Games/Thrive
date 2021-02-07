using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

/// <summary>
///   Converts dictionary keys to / from json
/// </summary>
public class ThriveTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return typeof(string) == sourceType;
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return typeof(string) == destinationType || destinationType.CustomAttributes.Any(
            attr => attr.AttributeType == typeof(UseThriveConverterAttribute));
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        // Must have the dynamic type used on the object, otherwise this doesn't do many sensible things
        return ThriveJsonConverter.Instance.DeserializeObjectDynamic((string)value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            var type = value.GetType();

            if (type.CustomAttributes.Any(attr =>
                attr.AttributeType == typeof(JSONAlwaysDynamicTypeAttribute) ||
                attr.AttributeType == typeof(JSONDynamicTypeAllowedAttribute)))
            {
                type = type.BaseType;
            }

            return ThriveJsonConverter.Instance.SerializeObject(value, type);
        }

        throw new NotSupportedException();
    }
}

/// <summary>
///   Attribute for marking a class compatible with ThriveTypeConverter
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class UseThriveConverterAttribute : Attribute
{
}
