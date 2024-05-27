using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnlockConstraints;

/// <summary>
///   Converter for <see cref="ConditionSet"/>
/// </summary>
public class ConditionSetConverter : JsonConverter
{
    public override bool CanRead => true;

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new JsonException("Unexpected data");
        }

        reader.Read();

        return new ConditionSet(ReadConditionSet(reader, serializer).ToArray());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ConditionSet);
    }

    private IEnumerable<IUnlockCondition> ReadConditionSet(JsonReader reader, JsonSerializer serializer)
    {
        while (reader.TokenType != JsonToken.EndObject)
        {
            if (reader.TokenType != JsonToken.PropertyName)
                throw new JsonException("Expected property name");

            // Get the type from the key string

            var className = reader.Value as string ?? throw new JsonException("Expected string as key");

            var fullyQualifiedName = $"{nameof(UnlockConstraints)}.{className}";

            // When loading the registry types the used serialization binder here allows using any type, but as we
            // use a namespace above this should be perfectly safe
            var type = serializer.SerializationBinder.BindToType("Thrive", fullyQualifiedName) ??
                throw new JsonException("Invalid type");

            reader.Read();

            // Get the unlock condition

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected object start");

            var condition = serializer.Deserialize(reader, type) as IUnlockCondition;

            if (condition == null)
            {
                throw new JsonException("Expected object of type IUnlockCondition");
            }

            yield return condition;

            reader.Read();
        }
    }
}
