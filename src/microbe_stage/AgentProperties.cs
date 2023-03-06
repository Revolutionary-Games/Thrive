using Godot;

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

    public override string ToString()
    {
        return TranslationServer.Translate("AGENT_NAME").FormatSafe(Compound.Name);
    }
}
