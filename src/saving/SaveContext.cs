using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DefaultEcs;

/// <summary>
///   Object providing access for converters as well as loading finalizing for the current game context
/// </summary>
public class SaveContext : ISaveContext
{
    public SaveContext(SimulationParameters simulation)
    {
        Simulation = simulation;
    }

    public SaveContext() : this(SimulationParameters.Instance)
    {
    }

    public SimulationParameters Simulation { get; }

    public GameWorld? World { get; set; }

    public World? ProcessedEntityWorld { get; set; }

    /// <summary>
    ///   List of entities to not save when writing a world to a save
    /// </summary>
    public HashSet<Entity> UnsavedEntities { get; } = new();

    // TODO: should game stages be allowed to keep their player references with this? This is currently cleared after
    // an entity world is finished loading
    public Dictionary<string, Entity> OldToNewEntityMapping { get; } = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SkipSavingEntity(in Entity value)
    {
        return UnsavedEntities.Contains(value);
    }

    internal void Reset()
    {
        World = null;
        ProcessedEntityWorld = null;
        UnsavedEntities.Clear();
        OldToNewEntityMapping.Clear();
    }
}
