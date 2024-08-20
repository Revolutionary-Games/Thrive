namespace AutoEvo;

public class CompoundConversionEfficiencyPressure : SelectionPressure
{
    public readonly Compound FromCompound;
    public readonly Compound ToCompound;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_EFFICIENCY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public CompoundConversionEfficiencyPressure(Compound compound, Compound outCompound, float weight) :
        base(weight, [
            AddOrganelleAnywhere.ThatConvertBetweenCompounds(compound, outCompound),
            RemoveOrganelle.ThatCreateCompound(outCompound),
        ])
    {
        FromCompound = compound;
        ToCompound = outCompound;
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        return cache.GetCompoundConversionScoreForSpecies(FromCompound, ToCompound, microbeSpecies, patch.Biome);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }

    public override string ToString()
    {
        return $"{Name} ({FromCompound.Name})";
    }
}
