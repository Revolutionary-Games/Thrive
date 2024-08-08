namespace AutoEvo;

using System;
using Godot;

public class EnvironmentalCompoundPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_ENVIRONMENTAL_COMPOUND_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

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

        if (createdCompound != atp && createdCompound != glucose)
        {
            throw new ArgumentException("Unhandled created compound");
        }

        this.compound = compound;
        this.createdCompound = createdCompound;
        this.patch = patch;

        totalEnergy = patch.Biome.AverageCompounds[compound].Ambient * energyMultiplier;
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var amountCreated = cache.GetCompoundGeneratedFrom(compound, createdCompound, microbeSpecies, patch.Biome);

        if (createdCompound == glucose)
        {
            amountCreated *= cache.GetCompoundConversionScoreForSpecies(glucose, atp, microbeSpecies, patch.Biome);
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

        // Penalize Species that do not rely on this compound
        return Mathf.Min(amountCreated / energyBalance.TotalConsumption, 1);
    }

    public override float GetEnergy()
    {
        return totalEnergy;
    }

    public override LocalizedString GetDescription()
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
