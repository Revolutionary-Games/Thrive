using System;
using Arch.Core;
using Components;
using SharedBase.Archive;

/// <summary>
///   Properties of an agent. Mainly used currently to block friendly fire
/// </summary>
public class AgentProperties : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private const string DamageTypeName = "oxytoxy";

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

    /// <summary>
    ///   True if this toxin has a special effect (instead of / in addition to) dealing damage
    /// </summary>
    public bool HasSpecialEffect => ToxinSubType is ToxinType.Macrolide or ToxinType.ChannelInhibitor;

    // TODO: subtypes (not high priority as it is pretty hard to hover over toxins in the game)
    // This has to be used like this to ensure the translation extractor sees this
    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    public LocalizedString Name =>
        new LocalizedString("AGENT_NAME",
            new LocalizedString(SimulationParameters.GetCompound(Compound).GetUntranslatedName()));

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.AgentProperties;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.AgentProperties)
            throw new NotSupportedException();

        writer.WriteObject((AgentProperties)obj);
    }

    public static AgentProperties ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new AgentProperties(reader.ReadObject<Species>(), (Compound)reader.ReadInt32(),
            (ToxinType)reader.ReadInt32());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Species);
        writer.Write((int)Compound);
        writer.Write((int)ToxinSubType);
    }

    public void DealDamage(ref Health health, ref CellProperties hitCellProperties, in Entity entity, float toxinAmount)
    {
        var damage = CalculateBaseDamage(toxinAmount);

        health.DealMicrobeDamage(ref hitCellProperties, entity, damage, DamageTypeName,
            HealthHelpers.GetInstantKillProtectionThreshold(entity));
    }

    public void DealDamage(ref Health health, in Entity entity, float toxinAmount)
    {
        var damage = CalculateBaseDamage(toxinAmount);

        health.DealDamage(entity, damage, DamageTypeName, HealthHelpers.GetInstantKillProtectionThreshold(entity));
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
