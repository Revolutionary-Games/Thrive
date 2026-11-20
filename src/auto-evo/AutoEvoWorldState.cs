using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Domain state at a generation boundary. This is a report, not a restorable world save.
/// </summary>
public sealed class AutoEvoWorldState
{
    public int Generation { get; init; }
    public double WorldTime { get; init; }
    public int HistoryEntries { get; init; }
    public required Dictionary<uint, long> SpeciesPopulations { get; init; }
    public required Dictionary<int, Dictionary<uint, long>> PatchPopulations { get; init; }

    public static AutoEvoWorldState Capture(GameWorld world)
    {
        return new AutoEvoWorldState
        {
            Generation = world.PlayerSpecies.Generation,
            WorldTime = world.TotalPassedTime,
            HistoryEntries = world.GenerationHistory.Count,
            SpeciesPopulations = world.Species.ToDictionary(s => s.Key, s => s.Value.Population),
            PatchPopulations = world.Map.Patches.ToDictionary(p => p.Key,
                p => p.Value.SpeciesInPatch.ToDictionary(s => s.Key.ID, s => s.Value)),
        };
    }
}
