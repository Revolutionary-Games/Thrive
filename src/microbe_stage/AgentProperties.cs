using Newtonsoft.Json;

/// <summary>
///   Properties of an agent. Mainly used currently to block friendly fire
/// </summary>
public class AgentProperties
{
    public AgentProperties(Species species, Compound compound)
    {
        Species = species;
        Compound = compound;
    }

    public Species Species { get; set; }
    public string AgentType { get; set; } = "oxytoxy";
    public Compound Compound { get; set; }

    [JsonIgnore]
    public LocalizedString Name =>
        new("AGENT_NAME", new LocalizedString(Compound.GetUntranslatedName()));

    public override string ToString()
    {
        return Name.ToString();
    }
}
