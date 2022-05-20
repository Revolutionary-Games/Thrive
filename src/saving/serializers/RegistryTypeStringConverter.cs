using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

/// <summary>
///   Converts between registry types and their internal names
/// </summary>
public class RegistryTypeStringConverter : TypeConverter
{
    /// <summary>
    ///   All of the supported registry types by this converter.
    ///   New entries need to be added when this converter is added as a class attribute
    /// </summary>
    protected static readonly Dictionary<string, SupportedRegistryType> SupportedRegistryTypes =
        new()
        {
            {
                "compound",
                new SupportedRegistryType(typeof(Compound), "compound",
                    name => SimulationParameters.Instance.GetCompound(name))
            },
        };

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return typeof(string) == sourceType;
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return typeof(string) == destinationType || GetRegistryByType(destinationType) != null;
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        var str = (string)value;

        var split = str.Split(':');

        if (split.Length != 2)
        {
            throw new FormatException("expected string in the form of 'registry:name'");
        }

        return SupportedRegistryTypes[split[0]].RetrieveInstance(split[1]);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            var type = GetRegistryByType(value.GetType());

            if (type == null)
            {
                throw new NotSupportedException("object type tried to be converted that is missing " +
                    "from supported registries");
            }

            return $"{type.Name}:{((IRegistryType)value).InternalName}";
        }

        throw new NotSupportedException();
    }

    protected SupportedRegistryType? GetRegistryByType(Type type)
    {
        foreach (var entry in SupportedRegistryTypes)
        {
            if (entry.Value.Type == type)
                return entry.Value;
        }

        return null;
    }

    protected class SupportedRegistryType
    {
        public SupportedRegistryType(Type type, string name, Func<string, object> retrieveInstance)
        {
            Type = type;
            Name = name;
            RetrieveInstance = retrieveInstance;
        }

        public Type Type { get; }
        public string Name { get; }
        public Func<string, object> RetrieveInstance { get; }
    }
}

public abstract class RegistryTypeStringSingleTypeConverter<TType> : RegistryTypeStringConverter
{
    protected abstract string TypeName { get; }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return typeof(string) == destinationType || typeof(TType) == destinationType;
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        return SupportedRegistryTypes[TypeName].RetrieveInstance((string)value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            var type = GetRegistryByType(typeof(TType));

            if (type == null || type.Name != TypeName)
                throw new NotSupportedException("RegistryTypeStringSingleTypeConverter configuration error");

            return ((IRegistryType)value).InternalName;
        }

        throw new NotSupportedException();
    }
}

/// <summary>
///   Specific converter for Compound
/// </summary>
public class CompoundStringConverter : RegistryTypeStringSingleTypeConverter<Compound>
{
    protected override string TypeName { get; } = "compound";
}
