using System.Collections.Generic;
using Arch.Core;

/// <summary>
///   Read-only context access to special code running when loading archives
/// </summary>
public interface ISaveContext
{
    // public SimulationParameters Simulation { get; }

    // public GameWorld? World { get; }

    public HashSet<Entity> UnsavedEntities { get; }

    public World? ProcessedEntityWorld { get; set; }

    public int ActiveProcessedWorldId { get; set; }

    public Dictionary<Entity, Entity> OldToNewEntityMapping { get; }

    public bool SkipSavingEntity(in Entity value);
}
