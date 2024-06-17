using Newtonsoft.Json;

/// <summary>
///   Finalized endosymbiosis action for <see cref="EndosymbiosisData"/> on a <see cref="Species"/>
/// </summary>
public class Endosymbiont
{
    public Endosymbiont(OrganelleDefinition resultingOrganelle, Species originallyFromSpecies)
    {
        ResultingOrganelle = resultingOrganelle;
        OriginallyFromSpecies = originallyFromSpecies;
    }

    [JsonProperty]
    public OrganelleDefinition ResultingOrganelle { get; private set; }

    [JsonProperty]
    public Species OriginallyFromSpecies { get; private set; }

    public Endosymbiont Clone()
    {
        return new Endosymbiont(ResultingOrganelle, OriginallyFromSpecies);
    }
}
