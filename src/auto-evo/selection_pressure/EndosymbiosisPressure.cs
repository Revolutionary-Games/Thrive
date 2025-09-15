namespace AutoEvo;

using Newtonsoft.Json;

[JSONDynamicTypeAllowed]

public class EndosymbiosisPressure : SelectionPressure
{
    [JsonProperty]
    public readonly Species Endosymbiont;

    [JsonProperty]
    public readonly Species Host;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString =
        new LocalizedString("MICHE_ENDOSYMBIOSIS_SELECTION_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public EndosymbiosisPressure(Species endosymbiont, Species host, float weight) : base(weight, [
    ])
    {
        Endosymbiont = endosymbiont;
        Host = host;
    }

    [JsonIgnore]
    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species.ID == Endosymbiont.ID)
        {
            return 1.0f;
        }

        return 0;
    }

    public override float GetEnergy(Patch patch)
    {
        if (!patch.SpeciesInPatch.TryGetValue(Host, out long population) || population <= 0)
            return 0;

        return population * Endosymbiont.GetPredationTargetSizeFactor();
    }

    public override string ToString()
    {
        return $"{Name} ({Endosymbiont.FormattedName})";
    }
}
