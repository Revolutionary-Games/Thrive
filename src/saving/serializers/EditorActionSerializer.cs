namespace Saving.Serializers;

using System;
using System.Reflection;
using SharedBase.Archive;

public static class EditorActionSerializer
{
    public const ushort SERIALIZATION_VERSION = 1;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        writer.WriteObject((IArchivable)obj);
    }

    public static EditorAction ReadFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var typeParameters = typeFromArchive.GetGenericArguments();
        var delegateType = typeof(Action<>).MakeGenericType(typeParameters[0]);

        var redo = reader.ReadDelegate(delegateType);
        var undo = reader.ReadDelegate(delegateType);
        var data = reader.ReadObject(out _);

        if (!typeParameters[0].IsInstanceOfType(data))
        {
            throw new FormatException($"Unexpected type read for single editor action. Got: {data.GetType()}, " +
                $"but wanted: {typeParameters[0]}");
        }

        var constructor = typeFromArchive.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [delegateType, delegateType, typeParameters[0]]);

        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {typeFromArchive.Name}");

        var instance = (EditorAction)constructor.Invoke([redo, undo, data]);

        // This automatically reads the base version and finishes reading the attributes
        instance.OnDeserializerFirstPartComplete(reader);
        return instance;
    }
}
