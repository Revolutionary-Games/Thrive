namespace Saving.Serializers;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Converter for <see cref="NodePath"/> that stores them as strings
/// </summary>
public class NodePathConverter : JsonConverter<NodePath>
{
    public override void WriteJson(JsonWriter writer, NodePath? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(value.ToString());
        }
    }

    public override NodePath? ReadJson(JsonReader reader, Type objectType, NodePath? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.Value is not string raw)
            throw new JsonException("Failed to read NodePath value as a string");

        return new NodePath(raw);
    }
}
