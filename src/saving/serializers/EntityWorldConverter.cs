using System;
using DefaultEcs;
using DefaultEcs.Serialization;
using Newtonsoft.Json;

/// <summary>
///   Handles saving / loading of entity worlds. Note that all entity component types must be added to the internal
///   write handler class inside this class.
/// </summary>
public class EntityWorldConverter : JsonConverter
{
    private const string EntitiesPropertyName = "Entities";

    private static readonly Type WorldType = typeof(World);

    private readonly SaveContext context;

    public EntityWorldConverter(SaveContext context)
    {
        this.context = context;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // It's probably fine to use a single temporary object here as a forwarder
        var componentWriter = new ComponentReadHandler(writer, serializer);

        var world = (World)value;

        writer.WriteStartObject();

        writer.WritePropertyName(EntitiesPropertyName);

        writer.WriteStartObject();

        foreach (var entity in world)
        {
            writer.WritePropertyName(entity.ToString());

            writer.WriteStartArray();
            entity.ReadAllComponents(componentWriter);
            writer.WriteEndArray();
        }

        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (context.ProcessedEntityWorld != null)
            throw new JsonException("Entity World deserialization is not re-entrant");

        var world = (World?)existingValue ?? new World();

        context.ProcessedEntityWorld = world;

        if (reader.TokenType != JsonToken.PropertyName)
        {
            throw new JsonException("Unexpected data");
        }

        var name = reader.ReadAsString();

        if (name == EntitiesPropertyName)
        {
            reader.Read();

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected object start");

            // Processing the entities
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                var entity = serializer.Deserialize<Entity>(reader);

                reader.Read();
                if (reader.TokenType != JsonToken.StartArray)
                    throw new JsonException("Expected array start");

                while (reader.TokenType != JsonToken.EndArray)
                {
                    var component = serializer.Deserialize(reader);

                    entity.Set(component);

                    if (!reader.Read())
                        throw new JsonException("Expected to see end of component array before end of data");
                }

                reader.Read();
                if (reader.TokenType != JsonToken.EndObject)
                    throw new JsonException("Expected end of object");
            }

            // Read the end array
            // TODO: should this be done
            reader.Read();
        }
        else
        {
            throw new JsonException("Unexpected property in World");
        }

        context.ProcessedEntityWorld = null;

        // See the TODO comment on this property
        context.OldToNewEntityMapping.Clear();
        return world;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == WorldType;
    }

    private class ComponentReadHandler : IComponentReader
    {
        /// <summary>
        ///   Used to force objects to write their dynamic types
        /// </summary>
        private static readonly Type ObjectType = typeof(object);

        private readonly JsonWriter writer;
        private readonly JsonSerializer jsonSerializer;

        public ComponentReadHandler(JsonWriter writer, JsonSerializer jsonSerializer)
        {
            this.writer = writer;
            this.jsonSerializer = jsonSerializer;
        }

        public void OnRead<T>(in T component, in Entity componentOwner)
        {
            // This probably boxes the struct, but should be fine for saving purposes, anything else would be really
            // hard to implement
            jsonSerializer.Serialize(writer, component, ObjectType);
        }
    }
}
