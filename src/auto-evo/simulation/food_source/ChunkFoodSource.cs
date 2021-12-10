using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

public class ChunkFoodSource : FoodSource
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound iron = SimulationParameters.Instance.GetCompound("iron");

    private readonly Patch patch;
    private readonly float totalEnergy;
    private readonly float chunkSize;
    private readonly string chunkName;
    private readonly Dictionary<Compound, float> energyCompounds;

    public ChunkFoodSource(Patch patch, string chunkType)
    {
        this.patch = patch;

        if (patch.Biome.Chunks.TryGetValue(chunkType, out ChunkConfiguration chunk))
        {
            chunkSize = chunk.Size;
            chunkName = chunk.Name;

            // NOTE: Here we use the heuristic that only iron and glucose are useful in chunks
            energyCompounds = chunk.Compounds.Where(c => c.Key == iron || c.Key == glucose).ToDictionary(
                c => c.Key, c => c.Value.Amount);
            totalEnergy = energyCompounds.Sum(c => c.Value) * chunk.Density * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
        }
    }

    public override float FitnessScore(Species species, SimulationCache simulationCache)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var energyBalance = simulationCache.GetEnergyBalanceForSpecies(microbeSpecies, patch);

        // Don't penalize species that can't move at full speed all the time as much here
        var chunkEaterSpeed = Math.Max(microbeSpecies.BaseSpeed + energyBalance.FinalBalance,
            microbeSpecies.BaseSpeed / 3);

        // We ponder the score for each compound by its amount, leading to pondering in proportion of total quantity,
        // with a constant factor that will be eliminated when making ratios of scores for this niche.
        var score = energyCompounds.Sum(c => EnergyGenerationScore(microbeSpecies, c.Key) * c.Value);

        score *= chunkEaterSpeed * species.Behaviour.Activity;

        // If the species can't engulf, then they are dependent on only eating the runoff compounds
        if (microbeSpecies.MembraneType.CellWall ||
            microbeSpecies.BaseHexSize < chunkSize * Constants.ENGULF_SIZE_RATIO_REQ)
        {
            score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        // Chunk (originally from marine snow) food source penalizes big creatures that try to rely on it
        score /= energyBalance.TotalConsumptionStationary;

        return score;
    }

    public override IFormattable GetDescription()
    {
        return new LocalizedString("CHUNK_FOOD_SOURCE", new LocalizedString(chunkName));
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnergy;
    }
}
