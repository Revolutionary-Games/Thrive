namespace AutoEvo;

using System;

public class CompoundConversionEfficiencyPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("AUTOTROPH_ENERGY_EFFICIENCY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public readonly Patch Patch;
    public readonly Compound FromCompound;
    public readonly Compound ToCompound;

    public CompoundConversionEfficiencyPressure(Patch patch, Compound compound, Compound outCompound, float weight) :
        base(weight, [
            AddOrganelleAnywhere.ThatConvertBetweenCompounds(compound, outCompound),
            RemoveOrganelle.ThatCreateCompound(compound),
        ])
    {
        Patch = patch;
        FromCompound = compound;
        ToCompound = outCompound;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return cache.GetCompoundConversionScoreForSpecies(FromCompound, ToCompound, species, Patch.Biome);
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
        return $"{Name} ({FromCompound.Name})";
    }
}
