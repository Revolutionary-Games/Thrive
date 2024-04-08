namespace Components;

/// <summary>
///   Group of entities that are limited. Each group has a separately counted limit.
/// </summary>
/// <remarks>
///   <para>
///     Don't reorder the values here otherwise saving will break
///   </para>
/// </remarks>
public enum LimitGroup
{
    General = 0,

    Chunk,

    /// <summary>
    ///   Chunks spawned by the spawn system
    /// </summary>
    ChunkSpawned,
}
