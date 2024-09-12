namespace Saving.Serializers;

using System;
using Newtonsoft.Json;

/// <summary>
///   Handles converting between <see cref="Compound"/> and string names of compounds in serialized JSON
/// </summary>
public class CompoundConverter : JsonConverter
{
    private readonly Type handledCompoundType = typeof(Compound);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (value is not Compound compound)
            throw new JsonException($"This converter only supports compound types (got: {value.GetType()})");

        var compoundDefinition = SimulationParameters.GetCompound(compound);
        writer.WriteValue(compoundDefinition.InternalName);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return Compound.Invalid;

        var asText = reader.Value as string;

        if (asText == null)
            throw new JsonException("Expected compound value to be stored as a string, instead got: " + reader.Value);

        if (string.IsNullOrEmpty(asText))
            return Compound.Invalid;

        return SimulationParameters.Instance.GetCompound(asText);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == handledCompoundType;
    }
}
