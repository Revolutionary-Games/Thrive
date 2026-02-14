namespace AutoEvo;

using SharedBase.Archive;

public class MetabolicStabilityPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_METABOLIC_STABILITY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public MetabolicStabilityPressure(float weight) : base(weight, [
        RemoveOrganelle.ThatUseCompound(Compound.ATP),
        new UpgradeOrganelle(organelle => organelle.HasMovementComponent, new FlagellumUpgrades(-1.0f)),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, -150.0f),
    ])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MetabolicStabilityPressure;

    public static MetabolicStabilityPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MetabolicStabilityPressure(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        if (cache.GetSpeedForSpecies(microbeSpecies) == 0)
        {
            return 0.0f;
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

        if (energyBalance.FinalBalance > 0)
        {
            return 1.0f;
        }

        if (energyBalance.FinalBalanceStationary > 0)
        {
            // Only punish non-sessile species for not being able to move continuously
            return 1.0f - microbeSpecies.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;
        }

        return 0.0f;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
