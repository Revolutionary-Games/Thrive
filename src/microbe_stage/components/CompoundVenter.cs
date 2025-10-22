namespace Components;

using SharedBase.Archive;

/// <summary>
///   An entity that constantly leaks compounds into the environment. Requires <see cref="CompoundStorage"/>.
/// </summary>
public struct CompoundVenter : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   How much of each compound is vented per second
    /// </summary>
    public float VentEachCompoundPerSecond;

    /// <summary>
    ///   When true venting is prevented (used, for example, when a chunk is engulfed)
    /// </summary>
    public bool VentingPrevented;

    public bool DestroyOnEmpty;

    /// <inheritdoc cref="DamageOnTouch.UsesMicrobialDissolveEffect"/>
    public bool UsesMicrobialDissolveEffect;

    /// <summary>
    ///   Internal flag, don't touch
    /// </summary>
    public bool RanOutOfVentableCompounds;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCompoundVenter;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(A PROPERTY);
        writer.WriteObject(A PROPERTY OF COMPLEX TYPE);
    }
}

public static class CompoundVenterHelpers
{
    public static CompoundVenter ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CompoundVenter.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CompoundVenter.SERIALIZATION_VERSION);

        return new CompoundVenter
        {
            AProperty = reader.ReadFloat(),
            AnotherProperty = reader.ReadObject<PropertyTypeGoesHere>(),
        };
    }

    public static void PopImmediately(this ref CompoundVenter venter, ref CompoundStorage compoundStorage,
        ref WorldPosition position, CompoundCloudSystem compoundClouds)
    {
        compoundStorage.VentAllCompounds(position.Position, compoundClouds);

        // For now nothing else except immediately venting everything happens
        _ = venter;
    }
}
