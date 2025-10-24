﻿namespace ThriveScriptsShared;

using SharedBase.Archive;

public interface IRegistryType : IRegistryAssignable
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Instead of writing the string name multiple times, we write object references
    // TODO: determine which is better
    public const bool REGISTRY_USE_REFERENCES = true;

    /// <summary>
    ///   The name referred to this registry object in JSON (and saves)
    /// </summary>
    public string InternalName { get; set; }

    /// <summary>
    ///   Checks that values are valid. Throws InvalidRegistryData if not good.
    /// </summary>
    /// <param name="name">Name of the current object for easier reporting.</param>
    /// <remarks>
    ///   <para>
    ///     Some registry types also process their initial data and create derived data here
    ///   </para>
    /// </remarks>
    public void Check(string name);

    /// <summary>
    ///   Fetch translations (if needed) for this object
    /// </summary>
    public void ApplyTranslations();
}

/// <summary>
///   Helper base implementing archive support for registry types
/// </summary>
public abstract class RegistryType : IRegistryType, IArchivable
{
    public ushort CurrentArchiveVersion => IRegistryType.SERIALIZATION_VERSION;

    public abstract ArchiveObjectType ArchiveObjectType { get; }

    public bool CanBeReferencedInArchive => IRegistryType.REGISTRY_USE_REFERENCES;

    public string InternalName { get; set; } = null!;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        // This must match what the WriteToArchive method does. And the simplest thing is to write just the internal
        // name and let the archive machinery map to the right read method
        writer.Write(((IRegistryType)obj).InternalName);
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        if (string.IsNullOrEmpty(InternalName))
        {
            throw new InvalidOperationException(
                $"This registry type cannot be saved as internal name is not initialized, type: {GetType()}");
        }

        // Write as the internal name which will be looked up in fresh registry data on a load
        writer.Write(InternalName);
    }

    public abstract void Check(string name);

    public abstract void ApplyTranslations();

    public override string ToString()
    {
        return InternalName;
    }

    protected static string ReadInternalName(ISArchiveReader reader, ushort version)
    {
        if (version is > IRegistryType.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, IRegistryType.SERIALIZATION_VERSION);

        return reader.ReadString() ?? throw new NullArchiveObjectException();
    }
}
