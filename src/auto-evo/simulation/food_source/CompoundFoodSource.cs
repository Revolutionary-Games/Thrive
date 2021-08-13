public class CompoundFoodSource : FoodSource
{
    private BiomeConditions biomeConditions;
    private Compound compound;
    private float totalCompound;

    public CompoundFoodSource(Patch patch, Compound compound)
    {
        biomeConditions = patch.Biome;
        this.compound = compound;
        if (patch.Biome.Compounds.ContainsKey(compound))
        {
            totalCompound = patch.Biome.Compounds[compound].Density * patch.Biome.Compounds[compound].Amount;
        }
        else
        {
            totalCompound = 0.0f;
        }
    }

    public override float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var compoundUseScore = EnergyGenerationScore(microbeSpecies, compound);

        var energyCost = ProcessSystem.ComputeEnergyBalance(
            microbeSpecies.Organelles.Organelles,
            biomeConditions, microbeSpecies.MembraneType).FinalBalanceStationary;

        return compoundUseScore / energyCost;
    }

    public override float TotalEnergyAvailable()
    {
        return totalCompound * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;
    }
}
