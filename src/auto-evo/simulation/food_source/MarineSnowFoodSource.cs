using AutoEvo;

public class MarineSnowFoodSource : FoodSource
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private readonly Patch patch;
    private readonly float totalEnergy;
    private readonly float chunkSize;

    public MarineSnowFoodSource(Patch patch)
    {
        this.patch = patch;

        if (patch.Biome.Chunks.TryGetValue("marineSnow", out ChunkConfiguration chunk))
        {
            chunkSize = chunk.Size;
            totalEnergy = chunk.Compounds[glucose].Amount * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
        }
    }

    public override float FitnessScore(Species species, SimulationCache simulationCache)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var energyBalance = simulationCache.GetEnergyBalanceForSpecies(microbeSpecies, patch);

        var predatorSpeed = microbeSpecies.BaseSpeed + energyBalance.FinalBalance;

        var score = predatorSpeed * species.Behaviour.Activity;

        // If the species can't engulf, then they are dependent on only eating the runoff compounds
        if (microbeSpecies.MembraneType.CellWall ||
            microbeSpecies.BaseHexSize < chunkSize * Constants.ENGULF_SIZE_RATIO_REQ)
        {
            score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        score /= energyBalance.FinalBalanceStationary;

        return score;
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnergy;
    }
}
