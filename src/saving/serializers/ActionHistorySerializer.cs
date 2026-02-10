namespace Saving.Serializers;

using System;
using System.Collections;
using System.Reflection;
using SharedBase.Archive;

public class ActionHistorySerializer
{
    public const ushort SERIALIZATION_VERSION = 1;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        writer.WriteObject((IArchivable)obj);
    }

    public static object ReadFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        int index = reader.ReadInt32();
        var data = reader.ReadObject<IList>();

        var constructor = typeFromArchive.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [data.GetType(), typeof(int)]);

        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {typeFromArchive.Name}");

        var instance = constructor.Invoke([data, index]);
        return instance;
    }
}
