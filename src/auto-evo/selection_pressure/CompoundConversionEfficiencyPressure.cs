namespace AutoEvo;

using System;
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

    [JsonProperty]
    private readonly bool usedForSurvival;

    public CompoundConversionEfficiencyPressure(Compound compound, Compound outCompound, float weight,
        bool usedForSurvival) :
        base(weight, [
            RemoveOrganelle.ThatCreateCompound(outCompound),
            AddOrganelleAnywhere.ThatConvertBetweenCompounds(compound, outCompound),
        ])
    {
        this.compound = compound;
        this.outCompound = outCompound;

        FromCompound = SimulationParameters.GetCompound(compound);
        ToCompound = SimulationParameters.GetCompound(outCompound);
        this.usedForSurvival = usedForSurvival;
    }

    [JsonIgnore]
    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var score = cache.GetCompoundConversionScoreForSpecies(FromCompound, ToCompound, microbeSpecies, patch.Biome);

        // we need to factor in both conversion from source to output, and energy expenditure time
        if (usedForSurvival)
        {
            score /=
                MathF.Sqrt(cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome).TotalConsumptionStationary);
        }

        return score;
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
