namespace AutoEvo;

using SharedBase.Archive;

public class PredatorRoot : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_PREDATOR_ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly CompoundDefinition atp = SimulationParameters.GetCompound(Compound.ATP);
    private readonly CompoundDefinition glucose = SimulationParameters.GetCompound(Compound.Glucose);

    public PredatorRoot(float weight) : base(weight, [
        RemoveOrganelle.ThatCreateCompound(Compound.Glucose),
        RemoveOrganelle.ThatCreateCompound(Compound.ATP),
        AddOrganelleAnywhere.ThatConvertBetweenCompounds(Compound.Glucose, Compound.ATP),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Aggression, 10.0f),
    ])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.PredatorRoot;

    public static PredatorRoot ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new PredatorRoot(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        float atpFromGlucose;
        EnergyBalanceInfoSimple energyBalance;

        if (species is MicrobeSpecies microbeSpecies)
        {
            atpFromGlucose = cache.GetCompoundGeneratedFrom(glucose, atp, microbeSpecies, patch.Biome);
            energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            atpFromGlucose = cache.GetCompoundGeneratedFrom(glucose, atp, multicellularSpecies, patch.Biome);
            energyBalance = cache.GetEnergyBalanceForSpecies(multicellularSpecies, patch.Biome);
        }
        else
        {
            return 0;
        }

        // ensure that the predator is at least slightly willing to hunt
        if (species.Behaviour.Aggression == 0)
        {
            return 0;
        }

        // Ensure that a predator can actually survive off of only glucose
        if (atpFromGlucose >= energyBalance.TotalConsumption)
        {
            return 1;
        }

        if (atpFromGlucose >= energyBalance.TotalConsumptionStationary)
        {
            return 0.5f;
        }

        // For now, we strictly forbid predators that need another food source to live
        return 0;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
