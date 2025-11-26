namespace Saving.Serializers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using SharedBase.Archive;

public static class MetaballActionDataSerializer
{
    public const ushort SERIALIZATION_VERSION = 1;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        writer.WriteObject((IMetaballAction)obj);
    }

    public static IMetaballAction ReadMoveFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var metaball = reader.ReadObject<Metaball>();
        var oldPosition = reader.ReadVector3();
        var newPosition = reader.ReadVector3();
        var oldParent = reader.ReadObjectOrNull<Metaball>();
        var newParent = reader.ReadObjectOrNull<Metaball>();
        var movedChildMetaballs = reader.ReadObjectOrNull(out _);

        // Due to the nulls, it would be extremely hard to find the right constructor, so we just get the first one
        // var otherMetaballTypes = typeFromArchive.GetGenericArguments()[0];
        // var constructor = typeFromArchive.GetConstructor(
        //     BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
        //     [metaball.GetType(), typeof(Vector3), typeof(Vector3), otherMetaballTypes, otherMetaballTypes, ]);

        var constructor = typeFromArchive.GetConstructors()[0];

        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {typeFromArchive.Name}");

        var instance = (IMetaballAction)constructor.Invoke([
            metaball, oldPosition, newPosition, oldParent, newParent, movedChildMetaballs,
        ]);

        instance.FinishBaseLoad(reader, version);

        return instance;
    }

    public static IMetaballAction ReadPlacementFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var metaball = reader.ReadObject<Metaball>();
        var position = reader.ReadVector3();
        var size = reader.ReadFloat();
        var parent = reader.ReadObjectOrNull<Metaball>();

        var metaballType = typeFromArchive.GetGenericArguments()[0];

        var constructor = typeFromArchive.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [metaball.GetType(), typeof(Vector3), typeof(float), metaballType]);

        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {typeFromArchive.Name}");

        var instance = (IMetaballAction)constructor.Invoke([metaball, position, size, parent]);

        instance.FinishBaseLoad(reader, version);

        return instance;
    }

    public static IMetaballAction ReadRemoveFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var metaball = reader.ReadObject<Metaball>();
        var position = reader.ReadVector3();
        var parent = reader.ReadObjectOrNull<Metaball>();
        var reparented = reader.ReadObjectOrNull<IList>();

        var metaballType = typeFromArchive.GetGenericArguments()[0];

        var listType = reparented == null ?
            typeof(List<>).MakeGenericType(typeof(MetaballMoveActionData<>).MakeGenericType(metaballType)) :
            reparented.GetType();

        var constructor = typeFromArchive.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [metaball.GetType(), typeof(Vector3), metaballType, listType]);

        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {typeFromArchive.Name}");

        var instance = (IMetaballAction)constructor.Invoke([metaball, position, parent, reparented]);

        instance.FinishBaseLoad(reader, version);

        return instance;
    }

    public static IMetaballAction ReadResizeFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var metaball = reader.ReadObject<Metaball>();
        var oldSize = reader.ReadFloat();
        var newSize = reader.ReadFloat();

        var constructor = typeFromArchive.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [metaball.GetType(), typeof(float), typeof(float)]);

        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {typeFromArchive.Name}");

        var instance = (IMetaballAction)constructor.Invoke([metaball, oldSize, newSize]);

        instance.FinishBaseLoad(reader, version);

        return instance;
    }
}
