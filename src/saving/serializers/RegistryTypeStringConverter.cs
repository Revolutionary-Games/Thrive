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
                    n => SimulationParameters.Instance.GetCompound(n))
            },
            {
                "enzyme",
                new SupportedRegistryType(typeof(Enzyme), "enzyme",
                    n => SimulationParameters.Instance.GetEnzyme(n))
            },
            {
                "worldResource",
                new SupportedRegistryType(typeof(WorldResource), "worldResource",
                    n => SimulationParameters.Instance.GetWorldResource(n))
            },
            {
                "equipment",
                new SupportedRegistryType(typeof(EquipmentDefinition), "equipment",
                    n => SimulationParameters.Instance.GetBaseEquipmentDefinition(n))
            },
            {
                "recipe",
                new SupportedRegistryType(typeof(CraftingRecipe), "recipe",
                    n => SimulationParameters.Instance.GetCraftingRecipe(n))
            },
            {
                "structure",
                new SupportedRegistryType(typeof(StructureDefinition), "structure",
                    n => SimulationParameters.Instance.GetStructure(n))
            },
            {
                "unitType",
                new SupportedRegistryType(typeof(UnitType), "unitType",
                    n => SimulationParameters.Instance.GetUnitType(n))
            },
            {
                "spaceStructure",
                new SupportedRegistryType(typeof(SpaceStructureDefinition), "spaceStructure",
                    n => SimulationParameters.Instance.GetSpaceStructure(n))
            },
            {
                "biome",
                new SupportedRegistryType(typeof(Biome), "biome",
                    n => SimulationParameters.Instance.GetBiome(n))
            },
            {
                "organelle",
                new SupportedRegistryType(typeof(OrganelleDefinition), "organelle",
                    n => SimulationParameters.Instance.GetOrganelleType(n))
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

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value == null)
            return null;

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

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value == null)
            return null;

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
///   Specific converter for <see cref="Compound"/>
/// </summary>
public class CompoundStringConverter : RegistryTypeStringSingleTypeConverter<Compound>
{
    protected override string TypeName => "compound";
}

/// <summary>
///   Specific converter for <see cref="Enzyme"/>
/// </summary>
public class EnzymeStringConverter : RegistryTypeStringSingleTypeConverter<Enzyme>
{
    protected override string TypeName => "enzyme";
}

/// <summary>
///   Specific converter for <see cref="WorldResource"/>
/// </summary>
public class WorldResourceStringConverter : RegistryTypeStringSingleTypeConverter<WorldResource>
{
    protected override string TypeName => "worldResource";
}

/// <summary>
///   Specific converter for <see cref="EquipmentDefinition"/>
/// </summary>
public class EquipmentDefinitionStringConverter : RegistryTypeStringSingleTypeConverter<EquipmentDefinition>
{
    protected override string TypeName => "equipment";
}

/// <summary>
///   Specific converter for <see cref="CraftingRecipe"/>
/// </summary>
public class CraftingRecipeStringConverter : RegistryTypeStringSingleTypeConverter<CraftingRecipe>
{
    protected override string TypeName => "recipe";
}

/// <summary>
///   Specific converter for <see cref="StructureDefinition"/>
/// </summary>
public class StructureStringConverter : RegistryTypeStringSingleTypeConverter<StructureDefinition>
{
    protected override string TypeName => "structure";
}

/// <summary>
///   Specific converter for <see cref="UnitType"/>
/// </summary>
public class UnitTypeStringConverter : RegistryTypeStringSingleTypeConverter<UnitType>
{
    protected override string TypeName => "unitType";
}

/// <summary>
///   Specific converter for <see cref="SpaceStructureDefinition"/>
/// </summary>
public class SpaceStructureStringConverter : RegistryTypeStringSingleTypeConverter<SpaceStructureDefinition>
{
    protected override string TypeName => "spaceStructure";
}

/// <summary>
///   Specific converter for <see cref="Biome"/>
/// </summary>
public class BiomeStringConverter : RegistryTypeStringSingleTypeConverter<Biome>
{
    protected override string TypeName => "biome";
}

/// <summary>
///   Specific converter for <see cref="OrganelleDefinition"/>
/// </summary>
public class OrganelleDefinitionStringConverter : RegistryTypeStringSingleTypeConverter<OrganelleDefinition>
{
    protected override string TypeName => "organelle";
}
