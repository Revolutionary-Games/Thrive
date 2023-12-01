using System;
using System.Collections.Generic;
using DefaultEcs;
using Newtonsoft.Json;

/// <summary>
///   A generic converter of a <see cref="Dictionary{TKey,TValue}"/> where the key is automatically JSON converted
///   for compatibility with deserializing complex keys.
/// </summary>
public class DictionaryWithJSONKeysConverter<TKey, TValue> : JsonConverter
    where TKey : notnull
{
    private static readonly Type ConvertedType = typeof(Dictionary<TKey, TValue>);
    private static readonly Type KeyType = typeof(TKey);
    private static readonly Type ValueType = typeof(TValue);

    private readonly bool canStringifyKey = typeof(Entity) == KeyType;
    private readonly bool valueShouldNotBeNull = ValueType.IsValueType || Nullable.GetUnderlyingType(KeyType) != null;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var converted = (Dictionary<TKey, TValue>)value;

        writer.WriteStartObject();

        foreach (var entry in converted)
        {
            // TODO: test with this forced off
            if (canStringifyKey)
            {
                // Can use the much cheaper operation of just converting to string here (but need to wrap in quotes for
                // deserialization)
                writer.WritePropertyName($"\"{entry.Key}\"");
            }
            else
            {
                // Need to use this separate serializer to convert the value to a string at this point
                writer.WritePropertyName(ThriveJsonConverter.Instance.SerializeObject(entry.Key, KeyType));
            }

            serializer.Serialize(writer, entry.Value, ValueType);
        }

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.StartObject)
            throw new JsonException("Expected start of dictionary object");

        reader.Read();

        var result = new Dictionary<TKey, TValue>();

        while (reader.TokenType == JsonToken.PropertyName)
        {
            var name = reader.Value as string;

            if (name == null)
                throw new JsonException("Expected JSON property name at this point");

            var deserializedKey = ThriveJsonConverter.Instance.DeserializeObject<TKey>(name) ??
                throw new JsonException("Deserialized dictionary key is null");

            reader.Read();

            var value = serializer.Deserialize<TValue>(reader);

            if (valueShouldNotBeNull && ReferenceEquals(value, null))
            {
                throw new JsonException(
                    "Deserialized dictionary value is null, but this dictionary can't have null values");
            }

            result.Add(deserializedKey, value!);

            reader.Read();
        }

        return result;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == ConvertedType;
    }
}
