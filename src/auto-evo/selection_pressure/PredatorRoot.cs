namespace AutoEvo;

public class PredatorRoot : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_PREDATOR_ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly CompoundDefinition atp = SimulationParameters.GetCompound(Compound.ATP);
    private readonly CompoundDefinition glucose = SimulationParameters.GetCompound(Compound.Glucose);

    public PredatorRoot(float weight) : base(weight, [
        AddOrganelleAnywhere.ThatConvertBetweenCompounds(Compound.Glucose, Compound.ATP),
    ])
    {
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var atpFromGlucose = cache.GetCompoundGeneratedFrom(glucose, atp, microbeSpecies, patch.Biome);

        // Ensure that a predator actually needs the glucose from prey
        if (atpFromGlucose >= cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome).TotalConsumption)
        {
            return 1;
        }

        if (atpFromGlucose >= cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome)
                .TotalConsumptionStationary)
        {
            return 0.5f;
        }

        // For now, we strictly forbid predators that need another food source to live
        return 0;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
