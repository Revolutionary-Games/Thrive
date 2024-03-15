using System;
using System.Collections.Generic;
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

    private readonly Dictionary<Type, IComponentForwarder> componentForwarders = new();

    private readonly SaveContext context;

    public EntityWorldConverter(SaveContext context)
    {
        this.context = context;
    }

    /// <summary>
    ///   Hacky way to go from object to the real type being set on the world for an entity component
    /// </summary>
    private interface IComponentForwarder
    {
        public void Set(object component, in Entity entity);
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
            // Skip entities that should not be saved
            if (context.SkipSavingEntity(entity))
                continue;

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

        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new JsonException("Unexpected data");
        }

        reader.Read();

        if (reader.TokenType != JsonToken.PropertyName)
            throw new JsonException("Expected property name");

        var name = reader.Value as string;

        if (name == EntitiesPropertyName)
        {
            reader.Read();

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected object start");

            reader.Read();

            // Processing the entities
            while (reader.TokenType != JsonToken.EndObject)
            {
                var entity = serializer.Deserialize<Entity>(reader);

                reader.Read();

                if (reader.TokenType != JsonToken.StartArray)
                    throw new JsonException("Expected array start");

                // Move into the array
                reader.Read();

                while (reader.TokenType != JsonToken.EndArray)
                {
                    var component = serializer.Deserialize(reader) ??
                        throw new JsonException("Deserialized component should not be null");

                    GetComponentForwarder(component.GetType()).Set(component, entity);

                    if (!reader.Read())
                        throw new JsonException("Expected to see end of component array before end of data");
                }

                // Move past the end of the array
                if (!reader.Read())
                    throw new JsonException("Ran out of data when looking for end of entity world");
            }
        }
        else
        {
            throw new JsonException("Unexpected property in World");
        }

        // Read past the end of the world data to allow the next serializer to work
        reader.Read();

        context.ProcessedEntityWorld = null;

        // Entities referencing each other should all work at this point as when converting the strings to entity
        // references the new entities must have been created
        // TODO: should entities that are just referenced but don't have component be deleted?

        // See the TODO comment on this property
        context.OldToNewEntityMapping.Clear();
        return world;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == WorldType;
    }

    /// <summary>
    ///   The other part of the component forwarding to the right container
    /// </summary>
    /// <param name="type">The type to get the forwarder for</param>
    /// <returns>A forwarder</returns>
    private IComponentForwarder GetComponentForwarder(Type type)
    {
        if (componentForwarders.TryGetValue(type, out var existing))
            return existing;

        var forwarder =
            (IComponentForwarder?)Activator.CreateInstance(typeof(ComponentForwarder<>).MakeGenericType(type));

        componentForwarders[type] = forwarder ??
            throw new InvalidOperationException("Failed to create instance of component forwarder for type");
        return forwarder;
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

    private class ComponentForwarder<T> : IComponentForwarder
    {
        public void Set(object component, in Entity entity)
        {
            entity.Set<T>((T)component);
        }
    }
}
