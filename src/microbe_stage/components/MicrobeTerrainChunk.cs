namespace Components;

using Systems;

/// <summary>
///   Part of microbe terrain. Handled by <see cref="MicrobeTerrainSystem"/>
/// </summary>
[JSONDynamicTypeAllowed]
public struct MicrobeTerrainChunk
{
    /// <summary>
    ///   Used to fetch spawned chunks to specific terrain groups
    /// </summary>
    public uint TerrainGroupId;
}
