namespace Components;

using SharedBase.Archive;

public struct CellBurstEffect : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Radius of the effect, needs to be set before this gets initialized
    /// </summary>
    public float Radius;

    /// <summary>
    ///   Used by the burst system to detect which entities are not initialized yet
    /// </summary>
    public bool Initialized;

    public CellBurstEffect(float radius)
    {
        Radius = radius;
        Initialized = false;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCellBurstEffect;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Radius);
        writer.Write(Initialized);
    }
}

public static class CellBurstEffectHelpers
{
    public static CellBurstEffect ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CellBurstEffect.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CellBurstEffect.SERIALIZATION_VERSION);

        return new CellBurstEffect
        {
            Radius = reader.ReadFloat(),
            Initialized = reader.ReadBool(),
        };
    }
}
