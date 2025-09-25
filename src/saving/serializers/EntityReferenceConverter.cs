﻿using System;
using Arch.Core;
using Arch.Core.Extensions;
using Newtonsoft.Json;

/// <summary>
///   Handles converting entity references to JSON and back into newly created entities. Mapping from old to new is
///   stored in <see cref="SaveContext.OldToNewEntityMapping"/>
/// </summary>
public class EntityReferenceConverter : JsonConverter<Entity>
{
    private const string AlwaysRemovedPart = "Entity = ";
    private static readonly string DefaultEntityStr = Entity.Null.ToString().Substring(AlwaysRemovedPart.Length);

    private readonly SaveContext context;

    public EntityReferenceConverter(SaveContext context)
    {
        this.context = context;

#if DEBUG
        if (DefaultEntityStr != "{ Id = −1, WorldId = 0, Version = −1 }")
            throw new Exception("Text format for Entity ToString has changed");
#endif
    }

    public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer)
    {
        if (value == Entity.Null)
        {
            writer.WriteValue(value.ToString());
            return;
        }

        // Don't write non-alive entities or entities that no longer want to be saved
        if (context.SkipSavingEntity(value) || !value.IsAlive())
            value = Entity.Null;

        writer.WriteValue(value.ToString());
    }

    public override Entity ReadJson(JsonReader reader, Type objectType, Entity existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (context.ProcessedEntityWorld == null)
            throw new JsonException("Cannot load an entity reference without a currently being loaded entity world");

        var old = reader.Value as string ??
            throw new Exception("Entity reference is null, or not string (should be default instead of null always)");

        // Need to remove the `Entity ` prefix part of the string
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
            return Entity.Null;

        // If already loaded returns the entity
        if (context.OldToNewEntityMapping.TryGetValue(old, out var existing))
            return existing;

        var newValue = context.ProcessedEntityWorld.Create();
        context.OldToNewEntityMapping[old] = newValue;

        return newValue;
    }
}
