namespace AutoEvo;

using System;

public class EnvironmentalCompoundPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("REACH_COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly float totalEnergy;
    private readonly Compound compound;

    public EnvironmentalCompoundPressure(Patch patch, float weight, Compound compound) : base(weight, [])
    {
        if (compound.IsCloud)
            throw new ArgumentException("Given compound to environmental pressure is a cloud type");

        this.compound = compound;

        // TODO: Make num a constant
        totalEnergy = patch.Biome.AverageCompounds[compound].Ambient * 100;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return 1;
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
