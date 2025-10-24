namespace Saving.Serializers;

using System;
using System.Collections;
using System.Reflection;
using SharedBase.Archive;

public static class HexLayoutSerializer
{
    public const ushort SERIALIZATION_VERSION = 1;

    public static object ReadFromArchive(ISArchiveReader reader, Type typeFromArchive, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var data = reader.ReadObject<IList>();

        var genericTypeArguments = typeFromArchive.GetGenericArguments();
        var layoutClass = typeof(OrganelleLayout<>).MakeGenericType(genericTypeArguments);

        var delegateType = typeof(Action<>).MakeGenericType(genericTypeArguments);

        var addedDelegate = reader.ReadDelegate(delegateType);
        var deletedDelegate = reader.ReadDelegate(delegateType);

        var constructor = typeFromArchive.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            [data.GetType(), delegateType, delegateType]);
        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {layoutClass.Name}");

        var instance = constructor.Invoke([data, addedDelegate, deletedDelegate]);
        return instance;
    }
}
