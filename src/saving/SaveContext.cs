using System.Collections.Generic;
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

    // TODO: should game stages be allowed to keep their player references with this? This is currently cleared after
    // an entity world is finished loading
    public Dictionary<string, Entity> OldToNewEntityMapping { get; } = new();

    internal void Reset()
    {
        World = null;
        ProcessedEntityWorld = null;
        OldToNewEntityMapping.Clear();
    }
}
