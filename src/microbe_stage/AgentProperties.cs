using Components;
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

    // This has to be used like this to ensure the translation extractor sees this
    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    [JsonIgnore]
    public LocalizedString Name =>
        new LocalizedString("AGENT_NAME", new LocalizedString(Compound.GetUntranslatedName()));

    public void DealDamage(ref Health health, ref CellProperties hitCellProperties, float toxinAmount)
    {
        var damage = Constants.OXYTOXY_DAMAGE * toxinAmount;

        health.DealMicrobeDamage(ref hitCellProperties, damage, AgentType);
    }

    public void DealDamage(ref Health health, float toxinAmount)
    {
        var damage = Constants.OXYTOXY_DAMAGE * toxinAmount;

        health.DealDamage(damage, AgentType);
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
