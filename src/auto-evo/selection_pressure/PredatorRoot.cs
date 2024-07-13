namespace AutoEvo;

using System;

public class PredatorRoot : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("PREDATOR_ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");

    private Patch patch;

    public PredatorRoot(Patch patch, float weight) : base(weight, [
        AddOrganelleAnywhere.ThatConvertBetweenCompounds(Glucose, ATP),
    ])
    {
        this.patch = patch;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var atpFromGlucose = cache.GetCompoundGeneratedFrom(Glucose, ATP, species, patch.Biome);

        // Ensure that a predator actually needs the glucose from prey
        if (atpFromGlucose >= cache.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption)
        {
            return 1;
        }

        if (atpFromGlucose >= cache.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumptionStationary)
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

    public override IFormattable GetDescription()
    {
        // This shouldn't be called on 0 energy pressures
        return Name;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
