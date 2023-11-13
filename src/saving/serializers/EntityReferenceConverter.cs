using System;
using DefaultEcs;
using Newtonsoft.Json;

/// <summary>
///   Handles converting entity references to JSON and back into dummy references that
///   <see cref="EntityWorldConverter"/> resolves on load
/// </summary>
public class EntityReferenceConverter : JsonConverter<Entity>
{
    private const string AlwaysRemovedPart = "Entity ";
    private static readonly string DefaultEntityStr = default(Entity).ToString();

    private readonly SaveContext context;

    public EntityReferenceConverter(SaveContext context)
    {
        this.context = context;

#if DEBUG
        if (!DefaultEntityStr.Contains(AlwaysRemovedPart))
            throw new Exception("Text format for Entity ToString has changed");
#endif
    }

    public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    public override Entity ReadJson(JsonReader reader, Type objectType, Entity existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (context.ProcessedEntityWorld == null)
            throw new JsonException("Cannot load an entity reference without a currently being loaded entity world");

        var old = reader.ReadAsString() ??
            throw new Exception("Entity reference is null (should be default instead of null always)");

        // Need to remove the "Entity " part of the string
#if DEBUG
        if (!old.Contains(AlwaysRemovedPart))
            throw new Exception("Unexpected entity reference string format");
#endif

        // Remove extra quotes automatically
        if (old.StartsWith("\""))
        {
            old = old.Substring(1 + AlwaysRemovedPart.Length, old.Length - 2 - AlwaysRemovedPart.Length);
        }
        else
        {
            old = old.Substring(AlwaysRemovedPart.Length, old.Length - AlwaysRemovedPart.Length);
        }

        if (old == DefaultEntityStr)
            return default(Entity);

        // If already loaded return the entity
        if (context.OldToNewEntityMapping.TryGetValue(old, out var existing))
            return existing;

        var newValue = context.ProcessedEntityWorld.CreateEntity();
        context.OldToNewEntityMapping[old] = newValue;

        return newValue;
    }
}
