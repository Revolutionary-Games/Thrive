using Components;
using Newtonsoft.Json;

/// <summary>
///   Properties of an agent. Mainly used currently to block friendly fire
/// </summary>
public class AgentProperties
{
    private const string DamageTypeName = "oxytoxy";

    public AgentProperties(Species species, Compound compound)
    {
        Species = species;
        Compound = compound;
    }

    public AgentProperties(Species species, Compound compound, ToxinType toxinSubType)
    {
        Species = species;
        Compound = compound;
        ToxinSubType = toxinSubType;
    }

    public Species Species { get; set; }
    public Compound Compound { get; set; }

    /// <summary>
    ///   On top of the <see cref="Compound"/> there can be a toxin subtype that adjusts the toxin effects
    /// </summary>
    public ToxinType ToxinSubType { get; set; }

    // TODO: subtypes (not high priority as it is pretty hard to hover over toxins in the game)
    // This has to be used like this to ensure the translation extractor sees this
    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    [JsonIgnore]
    public LocalizedString Name =>
        new LocalizedString("AGENT_NAME", new LocalizedString(Compound.GetUntranslatedName()));

    public void DealDamage(ref Health health, ref CellProperties hitCellProperties, float toxinAmount)
    {
        var damage = Constants.OXYTOXY_DAMAGE * toxinAmount;

        health.DealMicrobeDamage(ref hitCellProperties, damage, DamageTypeName);
    }

    public void DealDamage(ref Health health, float toxinAmount)
    {
        var damage = Constants.OXYTOXY_DAMAGE * toxinAmount;

        health.DealDamage(damage, DamageTypeName);
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
