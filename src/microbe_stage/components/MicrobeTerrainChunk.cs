namespace Components;

using SharedBase.Archive;
using Systems;

/// <summary>
///   Part of microbe terrain. Handled by <see cref="MicrobeTerrainSystem"/>
/// </summary>
public struct MicrobeTerrainChunk : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Used to fetch spawned chunks to specific terrain groups
    /// </summary>
    public uint TerrainGroupId;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeTerrainChunk;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(TerrainGroupId);
    }
}

public static class MicrobeTerrainChunkHelpers
{
    public static MicrobeTerrainChunk ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeTerrainChunk.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeTerrainChunk.SERIALIZATION_VERSION);

        return new MicrobeTerrainChunk
        {
            TerrainGroupId = reader.ReadUInt32(),
        };
    }
}
