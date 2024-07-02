namespace AutoEvo;

using System;
using Godot;

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
        var totalATP = cache.GetEnergyBalanceForSpecies(species, patch.Biome).TotalConsumption;

        // Ensure that a predator actually needs the glucose from prey
        return Mathf.Min(atpFromGlucose / totalATP, 1);
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
