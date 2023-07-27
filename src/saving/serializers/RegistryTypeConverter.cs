using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

/// <summary>
///   Serializes registry types as strings or objects (for supported types)
/// </summary>
/// <remarks>
///   <para>
///     See the <see cref="SupportsCustomizedRegistryTypeAttribute"/> for info on the full object serialization this
///     supports.
///   </para>
/// </remarks>
public class RegistryTypeConverter : BaseThriveConverter
{
    private readonly Type registryAssignable = typeof(IRegistryAssignable);
    private readonly Type concreteCustomizedRegistryAttribute = typeof(CustomizedRegistryTypeAttribute);

    public RegistryTypeConverter(ISaveContext context) : base(context)
    {
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        // Some registry types support both the registry objects and compatible objects, so if there's an object here
        // we can handle it if it is of one of those types
        if (reader.TokenType == JsonToken.StartObject)
        {
            var targetTypeAttribute = objectType.GetCustomAttribute<SupportsCustomizedRegistryTypeAttribute>();

            if (targetTypeAttribute == null)
            {
                throw new JsonException(
                    $"{nameof(SupportsCustomizedRegistryTypeAttribute)} missing from registry assignable " +
                    "interface to specify concrete created type when deserializing an object. Or unexpected start " +
                    $"of object for registry type of {objectType.FullName}");
            }

            var actualType = targetTypeAttribute.TargetType;

            return serializer.Deserialize(reader, actualType);
        }

        var name = serializer.Deserialize<string>(reader);

        if (name == null)
            return null;

        if (Context == null)
            throw new InvalidOperationException("Registry type converter must have valid Context");

        if (objectType == typeof(OrganelleDefinition))
            return Context.Simulation.GetOrganelleType(name);

        if (objectType == typeof(BioProcess))
            return Context.Simulation.GetBioProcess(name);

        if (objectType == typeof(Biome))
            return Context.Simulation.GetBiome(name);

        if (objectType == typeof(Compound))
            return Context.Simulation.GetCompound(name);

        if (objectType == typeof(MembraneType))
            return Context.Simulation.GetMembrane(name);

        if (objectType == typeof(Enzyme))
            return Context.Simulation.GetEnzyme(name);

        if (objectType == typeof(WorldResource))
            return Context.Simulation.GetWorldResource(name);

        if (objectType == typeof(CraftingRecipe))
            return Context.Simulation.GetCraftingRecipe(name);

        if (objectType == typeof(EquipmentDefinition))
            return Context.Simulation.GetBaseEquipmentDefinition(name);

        if (objectType == typeof(Technology))
            return Context.Simulation.GetTechnology(name);

        if (typeof(DayNightConfiguration).IsAssignableFrom(objectType) &&
            name == SimulationParameters.DAY_NIGHT_CYCLE_NAME)
        {
            return Context.Simulation.GetDayNightCycleConfiguration();
        }

        if (typeof(IDifficulty).IsAssignableFrom(objectType))
            return Context.Simulation.GetDifficultyPreset(name);

        if (typeof(IAutoEvoConfiguration).IsAssignableFrom(objectType) &&
            name == SimulationParameters.AUTO_EVO_CONFIGURATION_NAME)
        {
            return Context.Simulation.AutoEvoConfiguration;
        }

        if (typeof(MultiplayerGameMode).IsAssignableFrom(objectType))
            return Context.Simulation.GetMultiplayerGameMode(name);

        throw new Exception(
            $"a registry type is missing from the RegistryTypeConverter's {nameof(ReadJson)} function.");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        if (value is IRegistryType registryType)
        {
            var name = registryType.InternalName;

            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException(
                    $"A registry type ({value.GetType().FullName}) object has missing internal name, can't be saved");
            }

            serializer.Serialize(writer, name);
        }
        else
        {
            // Support writing the customized versions of objects for the registry types
            // Note the type of value must have CustomizedRegistryTypeAttribute otherwise we run into a stackoverflow
            serializer.Serialize(writer, value, value.GetType());
        }
    }

    public override bool CanConvert(Type objectType)
    {
        // Anything deriving from the interface that denotes it supports registry and compatible other types,
        // can be used
        if (!registryAssignable.IsAssignableFrom(objectType))
            return false;

        // Except the concrete customized types of registry types. If we supported those we'd run into a stackoverflow
        // when serializing such customized variants of registry types.
        return objectType.CustomAttributes.All(a => a.AttributeType != concreteCustomizedRegistryAttribute);
    }
}

/// <summary>
///   Marks the real type that should be deserialized when a <see cref="IRegistryAssignable"/> type that has this
///   attribute is deserialized from an object structure (and not a string, in which case normal registry type loading
///   is used)
/// </summary>
/// <remarks>
///   <para>
///     Note that the concrete type this points to must have the <see cref="CustomizedRegistryTypeAttribute"/>
///     otherwise serializing that value will result in a stackoverflow.
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class SupportsCustomizedRegistryTypeAttribute : Attribute
{
    public SupportsCustomizedRegistryTypeAttribute(Type targetType)
    {
        TargetType = targetType;
    }

    public Type TargetType { get; }
}

/// <summary>
///   Marks a class that is a customized version of a registry type.
///   Must be set on classes that <see cref="SupportsCustomizedRegistryTypeAttribute"/> point to.
/// </summary>
/// <remarks>
///   <para>
///     As using this usually causes JSON dynamic type to be written out, this implies
///     <see cref="JSONDynamicTypeAllowedAttribute"/> in the deserialization type binder.
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class CustomizedRegistryTypeAttribute : Attribute
{
}
