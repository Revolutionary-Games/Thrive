namespace AutoEvo;

public class CompoundConversionEfficiencyPressure : SelectionPressure
{
    public readonly Patch Patch;
    public readonly Compound FromCompound;
    public readonly Compound ToCompound;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_EFFICIENCY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public CompoundConversionEfficiencyPressure(Patch patch, Compound compound, Compound outCompound, float weight) :
        base(weight, [
            AddOrganelleAnywhere.ThatConvertBetweenCompounds(compound, outCompound),
            RemoveOrganelle.ThatCreateCompound(outCompound),
        ])
    {
        Patch = patch;
        FromCompound = compound;
        ToCompound = outCompound;
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        return cache.GetCompoundConversionScoreForSpecies(FromCompound, ToCompound, microbeSpecies, Patch.Biome);
    }

    public override float GetEnergy()
    {
        return 0;
    }

    public override string ToString()
    {
        return $"{Name} ({FromCompound.Name})";
    }
}
