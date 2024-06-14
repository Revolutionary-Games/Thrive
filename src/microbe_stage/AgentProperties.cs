using Components;
using Newtonsoft.Json;

/// <summary>
///   Properties of an agent. Mainly used currently to block friendly fire
/// </summary>
public class AgentProperties
{
    private const string DamageTypeName = "oxytoxy";

    public AgentProperties(Species species, Compound compound, ToxinType toxinSubType)
    {
        Species = species;
        Compound = compound;
        ToxinSubType = toxinSubType;
    }

    [JsonConstructor]
    public AgentProperties(Species species, Compound compound)
    {
        Species = species;
        Compound = compound;
    }

    public Species Species { get; set; }
    public Compound Compound { get; set; }

    /// <summary>
    ///   On top of the <see cref="Compound"/> there can be a toxin subtype that adjusts the toxin effects
    /// </summary>
    public ToxinType ToxinSubType { get; set; }

    /// <summary>
    ///   True if this toxin has a special effect (instead of / in addition to) dealing damage
    /// </summary>
    [JsonIgnore]
    public bool HasSpecialEffect => ToxinSubType is ToxinType.Macrolide or ToxinType.ChannelInhibitor;

    // TODO: subtypes (not high priority as it is pretty hard to hover over toxins in the game)
    // This has to be used like this to ensure the translation extractor sees this
    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    [JsonIgnore]
    public LocalizedString Name =>
        new LocalizedString("AGENT_NAME", new LocalizedString(Compound.GetUntranslatedName()));

    public void DealDamage(ref Health health, ref CellProperties hitCellProperties, float toxinAmount)
    {
        var damage = CalculateBaseDamage(toxinAmount);

        health.DealMicrobeDamage(ref hitCellProperties, damage, DamageTypeName);
    }

    public void DealDamage(ref Health health, float toxinAmount)
    {
        var damage = CalculateBaseDamage(toxinAmount);

        health.DealDamage(damage, DamageTypeName);
    }

    /// <summary>
    ///   Gets visuals for this agent based on the type
    /// </summary>
    /// <returns>The visual ID to use</returns>
    public VisualResourceIdentifier GetVisualResource()
    {
        switch (ToxinSubType)
        {
            case ToxinType.Cytotoxin:
                return VisualResourceIdentifier.AgentProjectileCytotoxin;
            case ToxinType.Macrolide:
                return VisualResourceIdentifier.AgentProjectileMacrolide;
            case ToxinType.ChannelInhibitor:
                return VisualResourceIdentifier.AgentProjectileChannelInhibitor;
            case ToxinType.OxygenMetabolismInhibitor:
                return VisualResourceIdentifier.AgentProjectileCyanide;
        }

        return VisualResourceIdentifier.AgentProjectile;
    }

    public override string ToString()
    {
        return Name.ToString();
    }

    private float CalculateBaseDamage(float toxinAmount)
    {
        switch (ToxinSubType)
        {
            case ToxinType.Cytotoxin:
                return Constants.CYTOTOXIN_DAMAGE * toxinAmount;
            case ToxinType.Macrolide:
            case ToxinType.ChannelInhibitor:
                return 0;
            case ToxinType.OxygenMetabolismInhibitor:
                return Constants.OXYGEN_INHIBITOR_DAMAGE * toxinAmount;
        }

        return Constants.OXYTOXY_DAMAGE * toxinAmount;
    }
}
