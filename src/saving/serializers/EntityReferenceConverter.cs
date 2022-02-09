using System;
using System.Reflection;
using Newtonsoft.Json;

public class EntityReferenceConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var objectType = value.GetType();

        var genericTypes = objectType.GenericTypeArguments;

        if (genericTypes.Length != 1)
            throw new JsonException("Invalid generic types for EntityReference");

        // Even though Microbe is used here, it shouldn't matter at all, as we just want to grab the property name
        var property = objectType.GetProperty(nameof(EntityReference<Microbe>.Value));

        if (property == null)
            throw new JsonException("Value property not found in EntityReference");

        var internalValue = property.GetValue(value);

        if (internalValue == null)
        {
            writer.WriteNull();
            return;
        }

        serializer.Serialize(writer, internalValue, genericTypes[0]);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        ConstructorInfo? constructor;
        if (reader.TokenType == JsonToken.Null)
        {
            constructor = objectType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null,
                CallingConventions.HasThis, Array.Empty<Type>(), null);

            if (constructor == null)
                throw new JsonException("could not find default constructor for EntityReference");

            return constructor.Invoke(Array.Empty<object>());
        }

        var genericTypes = objectType.GenericTypeArguments;

        if (genericTypes.Length != 1)
            throw new JsonException("Invalid generic types for EntityReference");

        constructor = objectType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null,
            CallingConventions.HasThis, new[] { genericTypes[0] }, null);

        if (constructor == null)
            throw new JsonException("could not find single argument constructor for EntityReference");

        return constructor.Invoke(new[] { serializer.Deserialize(reader, genericTypes[0]) });
    }

    public override bool CanConvert(Type objectType)
    {
        if (!objectType.IsGenericType)
            return false;

        return objectType.GetGenericTypeDefinition() == typeof(EntityReference<>);
    }
}
