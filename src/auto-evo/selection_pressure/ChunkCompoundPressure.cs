namespace AutoEvo;

using System;
using Godot;

public class ChunkCompoundPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("REACH_COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly float totalEnergy;
    private readonly ChunkConfiguration chunk;

    // private readonly Compound compound;

    public ChunkCompoundPressure(Patch patch, float weight, string chunkType, Compound compound) : base(weight, [])
    {
        if (!patch.Biome.Chunks.TryGetValue(chunkType, out var chunkData))
            throw new ArgumentException("Chunk does not exsist in patch");

        chunk = chunkData;

        if (chunk.Compounds?.TryGetValue(compound, out var compoundAmount) != true)
            throw new ArgumentException("Chunk does not exsist in patch");

        // This computation nerfs big chunks with a large amount,
        // by adding an "accessibility" component to total energy.
        // Since most cells will rely on bigger chunks by exploiting the venting,
        // this technically makes it a less efficient food source than small chunks, despite a larger amount.
        // We thus account for venting also in the total energy from the source,
        // by adding a volume-to-surface radius exponent ratio (e.g. 2/3 for a sphere).
        // This logic doesn't match with the rest of auto-evo (which doesn't account for accessibility).
        // TODO: extend this approach or find another nerf.
        var ventedEnergy = Mathf.Pow(compoundAmount.Amount, Constants.AUTO_EVO_CHUNK_AMOUNT_NERF);
        totalEnergy = ventedEnergy * chunk.Density * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var score = 1.0f;

        // Speed is not too important to chunk microbes
        // But all else being the same faster is better than slower
        score += cache.GetBaseSpeedForSpecies(species) * 0.1f;

        // Diminishing returns on storage
        score += (Mathf.Pow(species.StorageCapacities.Nominal + 1, 0.8f) - 1) / 0.8f;

        // If the species can't engulf, then they are dependent on only eating the runoff compounds
        if (!species.CanEngulf ||
            cache.GetBaseHexSizeForSpecies(species) < chunk.Size * Constants.ENGULF_SIZE_RATIO_REQ)
        {
            score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        return score;
    }

    public override IFormattable GetDescription()
    {
        return new LocalizedString("CHUNK_FOOD_SOURCE",
            string.IsNullOrEmpty(chunk.Name) ?
                new LocalizedString("NOT_FOUND_CHUNK") :
                new LocalizedString(chunk.Name));
    }

    public override float GetEnergy()
    {
        return totalEnergy;
    }

    public override string ToString()
    {
        var chunkName = string.IsNullOrEmpty(chunk.Name) ?
            new LocalizedString("NOT_FOUND_CHUNK") :
            new LocalizedString(chunk.Name);

        return $"{Name} ({chunkName})";
    }
}
