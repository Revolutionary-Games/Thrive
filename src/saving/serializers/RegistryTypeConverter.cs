using System;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Serializes registry types as strings instead of full objects
/// </summary>
public class RegistryTypeConverter : BaseThriveConverter
{
    public RegistryTypeConverter(ISaveContext context) : base(context)
    {
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

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

        throw new Exception("a registry type is missing from RegistryTypeConverter");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        serializer.Serialize(writer, ((IRegistryType)value).InternalName);
    }

    public override bool CanConvert(Type objectType)
    {
        // Registry types are supported
        return objectType.GetInterfaces().Contains(typeof(IRegistryType));
    }
}
