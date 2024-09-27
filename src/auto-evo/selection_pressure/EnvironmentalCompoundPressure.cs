namespace AutoEvo;

using System;

public class EnvironmentalCompoundPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_ENVIRONMENTAL_COMPOUND_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly CompoundDefinition atp = SimulationParameters.GetCompound(Compound.ATP);

    private readonly CompoundDefinition createdCompound;
    private readonly CompoundDefinition compound;
    private readonly float energyMultiplier;

    public EnvironmentalCompoundPressure(Compound compound, Compound createdCompound, float energyMultiplier,
        float weight) :
        base(weight, [
            AddOrganelleAnywhere.ThatUseCompound(compound),
        ])
    {
        this.compound = SimulationParameters.GetCompound(compound);

        if (this.compound.IsCloud)
            throw new ArgumentException("Given compound to environmental pressure is a cloud type");

        if (createdCompound != Compound.ATP && createdCompound != Compound.Glucose)
        {
            throw new ArgumentException("Unhandled created compound");
        }

        this.createdCompound = SimulationParameters.GetCompound(createdCompound);
        this.energyMultiplier = energyMultiplier;
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var amountCreated = cache.GetCompoundGeneratedFrom(compound, createdCompound, microbeSpecies, patch.Biome);

        if (createdCompound.ID == Compound.Glucose)
        {
            amountCreated *=
                cache.GetCompoundConversionScoreForSpecies(createdCompound, atp, microbeSpecies, patch.Biome);
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

        // Penalize Species that do not rely on this compound
        return MathF.Min(amountCreated / energyBalance.TotalConsumption, 1);
    }

    public override float GetEnergy(Patch patch)
    {
        return patch.Biome.AverageCompounds[compound.ID].Ambient * energyMultiplier;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("DISSOLVED_COMPOUND_FOOD_SOURCE",
            new LocalizedString(compound.GetUntranslatedName()));
    }

    public Compound GetUsedCompoundType()
    {
        return compound.ID;
    }

    public override string ToString()
    {
        return $"{Name} ({compound.Name})";
    }
}
