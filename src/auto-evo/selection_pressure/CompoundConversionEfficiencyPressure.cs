namespace AutoEvo;

using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public class CompoundConversionEfficiencyPressure : SelectionPressure
{
    [JsonIgnore]
    public readonly CompoundDefinition FromCompound;

    [JsonIgnore]
    public readonly CompoundDefinition ToCompound;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_EFFICIENCY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    // These two are needed purely for saving to work
    [JsonProperty]
    private readonly Compound compound;

    [JsonProperty]
    private readonly Compound outCompound;

    public CompoundConversionEfficiencyPressure(Compound compound, Compound outCompound, float weight) :
        base(weight, [
            AddOrganelleAnywhere.ThatConvertBetweenCompounds(compound, outCompound),
            RemoveOrganelle.ThatCreateCompound(outCompound),
        ])
    {
        this.compound = compound;
        this.outCompound = outCompound;

        FromCompound = SimulationParameters.GetCompound(compound);
        ToCompound = SimulationParameters.GetCompound(outCompound);
    }

    [JsonIgnore]
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
