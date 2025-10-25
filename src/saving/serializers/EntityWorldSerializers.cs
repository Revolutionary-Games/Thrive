namespace Saving.Serializers;

using System;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using SharedBase.Archive;

public static class EntityWorldSerializers
{
    public const ushort SERIALIZATION_VERSION = 1;

    public static void WriteEntityReferenceToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.Entity)
            throw new NotSupportedException();

        // As this is a custom-writing callback, we need to put in a header here
        writer.WriteObjectHeader((ArchiveObjectType)ThriveArchiveObjectType.Entity, false, false, false, false,
            SERIALIZATION_VERSION);

        var entity = (Entity)obj;

        WriteEntityReferenceToArchive(writer, entity);
    }

    public static void WriteEntityReferenceToArchive(ISArchiveWriter writer, Entity entity)
    {
        var manager = (ISaveContext)writer.WriteManager;

        // Don't write non-alive entities or entities that no longer want to be saved.
        // Also, we need to explicitly check for 0-initialised entities as they are not valid to check for alive.
        if (manager.SkipSavingEntity(entity) || entity == Entity.Null || entity.IsAllZero() || !entity.IsAlive())
        {
            entity = Entity.Null;
        }

        writer.Write(entity.Id);
        writer.Write(entity.WorldId);
        writer.Write(entity.Version);
    }

    public static object ReadEntityReferenceFromArchiveBoxed(ISArchiveReader reader, ushort version, int referenceId)
    {
        return ReadEntityReferenceFromArchive(reader, version);
    }

    public static Entity ReadEntityReferenceFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var id = reader.ReadInt32();
        var worldId = reader.ReadInt32();
        var entityVersion = reader.ReadInt32();

        var manager = (ISaveContext)reader.ReadManager;

        // TODO: this could actually "buffer" stuff before a world is created by creating worlds in the context and
        // then those are popped based on the ID by WriteEntityWorldToArchive
        if (manager.ProcessedEntityWorld == null)
            throw new FormatException("Cannot load an Entity reference without a currently being loaded entity World");

        // Force create an old reference
        var old = Entity.MakeHackedEntity(id, worldId, entityVersion);

        // Null doesn't need mapping
        var nullEntity = Entity.Null;
        if (old == nullEntity || old == default(Entity))
            return Entity.Null;

        // If already loaded returns the entity
        if (manager.OldToNewEntityMapping.TryGetValue(old, out var existing))
            return existing;

        // This could do some verification of old mappings matching the ID
        // if (worldId != manager.ActiveProcessedOldWorldId)

        // Otherwise create a new mapping and remember it
        var newValue = manager.ProcessedEntityWorld.Create(Signature.Null);
        manager.OldToNewEntityMapping[old] = newValue;

        return newValue;
    }

    public static void WriteEntityWorldToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.EntityWorld)
            throw new NotSupportedException();

        // Must write the header first and handle referencing
        writer.WriteObjectHeader((ArchiveObjectType)ThriveArchiveObjectType.EntityWorld, true, false,
            writer.WriteManager.IsReferencedAlready(obj), false, SERIALIZATION_VERSION);

        if (writer.WriteManager.MarkStartOfReferenceObject(writer, obj))
        {
            // Object was already written
            return;
        }

        var manager = (ISaveContext)writer.WriteManager;

        var world = (World)obj;

        writer.Write(world.Id);
        writer.Write(world.Size);

        foreach (var archetype in world)
        {
            foreach (var chunk in archetype)
            {
                int count = chunk.Count;
                for (int i = 0; i < count; ++i)
                {
                    var entity = chunk.Entities[i];

                    if (manager.SkipSavingEntity(entity))
                        continue;

                    // Writing more, so mark used
                    writer.Write((byte)1);

                    // The API here is such that we need to allocate memory, but we can't really get around that
                    var components = entity.GetAllComponents();

                    WriteEntityReferenceToArchive(writer, entity);

                    if (components.Length > byte.MaxValue)
                        throw new FormatException($"Too many components to save in entity {entity}");

                    writer.Write((byte)components.Length);

                    foreach (var component in components)
                    {
                        if (component == null)
                            throw new FormatException("Entity may not have null components");

                        // All components must implement IArchivableComponent
                        WriteEntityComponent(writer, (IArchivableComponent)component);
                    }
                }
            }
        }

        // Write a 0 to indicate the end of the chunk list
        writer.Write((byte)0);
    }

    public static object ReadEntityWorldFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var manager = (ISaveContext)reader.ReadManager;

        // Start the new world
        var world = World.Create();

        if (manager.ProcessedEntityWorld != null)
            throw new FormatException("Entity World deserialization is not re-entrant");

        // And register it to the manager to let all entities refer to each other
        manager.ProcessedEntityWorld = world;
        manager.OldToNewEntityMapping.Clear();

        // Read the old world ID and register it
        var oldId = reader.ReadInt32();

        // TODO: use this for pre-allocation?
        var oldSize = reader.ReadInt32();

        manager.ActiveProcessedOldWorldId = oldId;

        // Read entities while there are some
        while (reader.ReadInt8() == 1)
        {
            /*reader.ReadObjectHeader(out var type, out _, out bool isNull, out bool referencesEarlier,
                out bool extendedType, out var entityVersion);

            if (type != (ArchiveObjectType)ThriveArchiveObjectType.Entity || isNull || referencesEarlier ||
                extendedType)
            {
                throw new FormatException("Expected an entity header at this point in the archive");
            }*/

            var entity = ReadEntityReferenceFromArchive(reader, version);

            var componentCount = reader.ReadInt8();

            for (int i = 0; i < componentCount; ++i)
            {
                // It is somewhat inefficient to read and add one component at a time as that causes many archetype
                // changes. However, this is just a one-time thing on a load, and constructing the entity archetype
                // beforehand would be quite complicated.
                ReadEntityComponent(reader, entity);
            }
        }

        if (manager.ProcessedEntityWorld != world)
            throw new FormatException("Something has messed with our deserializing world");

        manager.ProcessedEntityWorld = null;

        // World size may change due to ignore saved entities, so we don't worry too much about it
        if (world.Size != oldSize)
        {
            GD.Print(
                $"Loaded {world.Size} entities, but save had {oldSize} (likely due to soon dying ignored entities)");
        }

        return world;
    }

    private static void WriteEntityComponent(ISArchiveWriter writer, IArchivableComponent component)
    {
        writer.Write(PackLightComponentHeader(component.ArchiveObjectType, component.CurrentArchiveVersion));

        component.WriteToArchive(writer);
    }

    private static void ReadEntityComponent(ISArchiveReader reader, Entity entity)
    {
        UnPackLightHeader(reader.ReadUInt32(), out ThriveArchiveObjectType objectType, out var version);

        if (ComponentDeserializers.ReadComponentToEntity(reader, entity, objectType, version))
            return;

        throw new FormatException($"Unknown component type to read: {objectType} (version: {version})");
    }

    private static uint PackLightComponentHeader(ThriveArchiveObjectType type, ushort version)
    {
        if (version >= byte.MaxValue - 1)
            throw new FormatException("Only 8 bits are reserved for component versions");

        // Type is at most 24 bits, so we can fit the both variables into the same int
        return (uint)type & 0xFFFFFF | (uint)version << 24;
    }

    private static void UnPackLightHeader(uint data, out ThriveArchiveObjectType type, out ushort version)
    {
        type = (ThriveArchiveObjectType)(data & 0xFFFFFF);
        version = (ushort)(data >> 24);
    }
}
