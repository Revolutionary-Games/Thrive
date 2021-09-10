public class MarineSnowFoodSource : FoodSource
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private Patch patch;
    private BiomeConditions biomeConditions;
    private float totalEnergy;
    private float chunkSize;

    public MarineSnowFoodSource(Patch patch)
    {
        this.patch = patch;
        biomeConditions = patch.Biome;

        if (patch.Biome.Chunks.TryGetValue("marineSnow", out ChunkConfiguration chunk))
        {
            chunkSize = chunk.Size;
            totalEnergy = chunk.Compounds[glucose].Amount * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
        }
    }

    public override float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var predatorSpeed = microbeSpecies.BaseSpeed;
        predatorSpeed += ProcessSystem
            .ComputeEnergyBalance(microbeSpecies.Organelles.Organelles, patch.Biome,
                microbeSpecies.MembraneType).FinalBalance;

        var score = predatorSpeed * species.Activity;

        // If the species can't engulf, then they are dependent on only eating the runoff compounds
        if (microbeSpecies.MembraneType.CellWall ||
            microbeSpecies.BaseSize < chunkSize * Constants.ENGULF_SIZE_RATIO_REQ)
        {
            score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        score /= ProcessSystem.ComputeEnergyBalance(
            microbeSpecies.Organelles.Organelles,
            biomeConditions, microbeSpecies.MembraneType).FinalBalanceStationary;

        return score;
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnergy;
    }
}
