public class EnvironmentalFoodSource : FoodSource
{
    private readonly Compound compound;
    private BiomeConditions biomeConditions;
    private float totalEnvironmentalEnergySource;

    public EnvironmentalFoodSource(Patch patch, string compound, float foodCapacityMultiplier)
    {
        biomeConditions = patch.Biome;
        this.compound = SimulationParameters.Instance.GetCompound(compound);
        totalEnvironmentalEnergySource = patch.Biome.Compounds[this.compound].Dissolved * foodCapacityMultiplier;
    }

    public override float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var energyCreationScore = EnergyGenerationScore(microbeSpecies, compound);

        var energyCost = ProcessSystem.ComputeEnergyBalance(
            microbeSpecies.Organelles.Organelles,
            biomeConditions, microbeSpecies.MembraneType).FinalBalanceStationary;

        return energyCreationScore / energyCost;
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnvironmentalEnergySource;
    }
}
