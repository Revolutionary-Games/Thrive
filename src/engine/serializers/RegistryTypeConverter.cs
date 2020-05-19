using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Serializes registry types as strings instead of full objects
/// </summary>
public class RegistryTypeConverter : BaseThriveConverter
{
    public RegistryTypeConverter(SimulationParameters simulation) : base(simulation)
    {
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var name = serializer.Deserialize<string>(reader);

        if (objectType == typeof(OrganelleDefinition))
            return Simulation.GetOrganelleType(name);

        if (objectType == typeof(BioProcess))
            return Simulation.GetBioProcess(name);

        if (objectType == typeof(Biome))
            return Simulation.GetBiome(name);

        if (objectType == typeof(Compound))
            return Simulation.GetCompound(name);

        if (objectType == typeof(MembraneType))
            return Simulation.GetMembrane(name);

        throw new Exception("a registry type is missing from RegistryTypeConverter");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
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
