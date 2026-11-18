namespace AutoEvo;

using System.Collections.Generic;

/// <summary>
///   The ecology used to evaluate a patch, paired with the world patch that owns its results.
/// </summary>
internal readonly struct AutoEvoPatch(Patch key, Patch ecology)
{
    public Patch Key { get; } = key;

    public BiomeConditions Biome => ecology.Biome;
    public Dictionary<Species, long> Residents => ecology.SpeciesInPatch;

    public static AutoEvoPatch Live(Patch patch)
    {
        return new AutoEvoPatch(patch, patch);
    }

    public long PopulationOf(Species species)
    {
        return ecology.GetSpeciesSimulationPopulation(species);
    }

    public float Score(SelectionPressure pressure, Species species, SimulationCache cache)
    {
        return pressure.Score(species, ecology, cache);
    }

    public float GetEnergy(SelectionPressure pressure)
    {
        return pressure.GetEnergy(ecology);
    }
}
