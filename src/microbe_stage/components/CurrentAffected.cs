namespace Components;

using SharedBase.Archive;
using Systems;

/// <summary>
///   Marks entity as being affected by <see cref="FluidCurrentsSystem"/>. Additionally
///   <see cref="ManualPhysicsControl"/> and <see cref="WorldPosition"/> are required components.
///   This exists as currents need to be skipped for microbes for now as we don't have visualisations for the
///   currents.
/// </summary>
public struct CurrentAffected : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Currents' effect on this entity. Note that 0 means the same as 1, being the default constructor value,
    ///   while -1 disables currents' effect.
    /// </summary>
    public float EffectStrength;

    public CurrentAffected(float effectStrength)
    {
        EffectStrength = effectStrength;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCurrentAffected;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(EffectStrength);
    }
}

public static class CurrentAffectedHelpers
{
    public static CurrentAffected ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CurrentAffected.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CurrentAffected.SERIALIZATION_VERSION);

        return new CurrentAffected
        {
            EffectStrength = reader.ReadFloat(),
        };
    }
}
