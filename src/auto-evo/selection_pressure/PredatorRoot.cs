namespace AutoEvo;

public class PredatorRoot : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_PREDATOR_ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private readonly Patch patch;

    public PredatorRoot(Patch patch, float weight) : base(weight, [
        AddOrganelleAnywhere.ThatConvertBetweenCompounds("glucose", "atp"),
    ])
    {
        this.patch = patch;
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, SimulationCache cache)
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

        // for now we strictly forbid predators that need another food source to live
        return 0;
    }

    public override float GetEnergy()
    {
        return 0;
    }
}
