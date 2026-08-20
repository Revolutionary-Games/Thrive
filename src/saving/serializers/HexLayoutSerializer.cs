namespace Saving.Serializers;

using System;
using System.Collections;
using System.Reflection;
using SharedBase.Archive;

public static class HexLayoutSerializer
{
    public const ushort SERIALIZATION_VERSION = 2;

    public static object ReadFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var genericTypeArguments = typeFromArchive.GetGenericArguments();

        // We need to support various layout types, and the default type resolving seems good enough right now
        // var layoutClass = typeof(OrganelleLayout<>).MakeGenericType(genericTypeArguments);
        // var layoutClass = typeFromArchive.GetGenericTypeDefinition().MakeGenericType(genericTypeArguments);
        var layoutClass = typeFromArchive;

        var delegateType = typeof(Action<>).MakeGenericType(genericTypeArguments);

        var instance = Activator.CreateInstance(typeFromArchive)
            ?? throw new InvalidOperationException($"No parameterless constructor found for {layoutClass.Name}");

        // We need to report the constructor done before we read the data, because the data might contain references
        // to the object
        reader.ReportObjectConstructorDone(instance, referenceId);

        // Now we can read the data
        var data = reader.ReadObject<IList>();
        var addedDelegate = reader.ReadDelegate(delegateType);
        var deletedDelegate = reader.ReadDelegate(delegateType);

        // And override the list contents with the actual data
        ((IArchiveLayoutInitializer)instance).InitializeFromArchive(data, addedDelegate, deletedDelegate);

        return instance;
    }

    public static object ReadIndividualHexLayoutFromArchive(ISArchiveReader reader, Type typeFromArchive,
        ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var genericTypeArguments = typeFromArchive.GetGenericArguments();
        var layoutClass = typeof(IndividualHexLayout<>).MakeGenericType(genericTypeArguments);

        var wrapper = typeof(HexWithData<>).MakeGenericType(genericTypeArguments);
        var delegateType = typeof(Action<>).MakeGenericType(wrapper);

        var instance = Activator.CreateInstance(typeFromArchive)
            ?? throw new InvalidOperationException($"No parameterless constructor found for {layoutClass.Name}");

        reader.ReportObjectConstructorDone(instance, referenceId);

        var data = reader.ReadObject<IList>();
        var addedDelegate = reader.ReadDelegate(delegateType);
        var deletedDelegate = reader.ReadDelegate(delegateType);
        ((IArchiveLayoutInitializer)instance).InitializeFromArchive(data, addedDelegate, deletedDelegate);

        return instance;
    }

    public static void WriteHexWithDataToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.HexWithData)
            throw new NotSupportedException();

        writer.WriteObject((IArchivable)obj);
    }

    public static object ReadHexWithDataFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var data = reader.ReadObject<IArchivable>();

        var genericTypeArguments = typeFromArchive.GetGenericArguments();
        var layoutClass = typeof(HexWithData<>).MakeGenericType(genericTypeArguments);

        var constructor = typeFromArchive.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [data.GetType(), typeof(Hex), typeof(int)]);
        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {layoutClass.Name}");

        object instance;
        if (version < 2)
        {
            instance = constructor.Invoke([data, reader.ReadHex(), 0]);
        }
        else
        {
            instance = constructor.Invoke([data, reader.ReadHex(), reader.ReadInt32()]);
        }

        return instance;
    }
}
