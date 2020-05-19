using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Serializer for Patch
/// </summary>
public class PatchConverter : SingleTypeConverter<Patch>
{
    public PatchConverter(SimulationParameters simulation) : base(simulation)
    {
    }

    protected override bool WriteDerivedJson(JsonWriter writer, Patch value, JsonSerializer serializer)
    {
        return false;
    }
}
