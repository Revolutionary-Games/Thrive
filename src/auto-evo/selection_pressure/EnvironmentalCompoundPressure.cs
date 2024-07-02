namespace AutoEvo;

using System;
using Godot;

public class EnvironmentalCompoundPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("ENVIRONMENTAL_COMPOUND_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");

    private readonly float totalEnergy;
    private readonly Compound createdCompound;
    private readonly Compound compound;
    private readonly Patch patch;

    public EnvironmentalCompoundPressure(Patch patch, float weight, Compound compound, Compound createdCompound,
        float energyMultiplier) :
        base(weight, [
            AddOrganelleAnywhere.ThatUseCompound(compound),
        ])
    {
        if (compound.IsCloud)
            throw new ArgumentException("Given compound to environmental pressure is a cloud type");

        this.compound = compound;
        this.createdCompound = createdCompound;
        this.patch = patch;

        totalEnergy = patch.Biome.AverageCompounds[compound].Ambient * energyMultiplier;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var amountCreated = cache.GetCompoundGeneratedFrom(compound, createdCompound, species, patch.Biome);

        if (createdCompound == Glucose)
            amountCreated *= cache.GetCompoundConversionScoreForSpecies(Glucose, ATP, species, patch.Biome);

        var energyBalance = cache.GetEnergyBalanceForSpecies(species, patch.Biome);

        return Mathf.Min(amountCreated / energyBalance.TotalConsumption, 1);
    }

    public override float GetEnergy()
    {
        return totalEnergy;
    }

    public override IFormattable GetDescription()
    {
        // TODO: somehow allow the compound name to translate properly. We now have custom BBCode to refer to
        // compounds so this should be doable
        return new LocalizedString("DISSOLVED_COMPOUND_FOOD_SOURCE", compound.Name);
    }

    public override string ToString()
    {
        return $"{Name} ({compound.Name})";
    }
}
