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
        AddOrganelleAnywhere.ThatConvertBetweenCompounds(Compound.Glucose, Compound.ATP),
        new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
        new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent, CommonMutationFunctions.Direction.Front),
        new AddOrganelleAnywhere(organelle => organelle.HasLysosomeComponent),
        new UpgradeToxinOrganelle(organelle => organelle.HasAgentVacuoleComponent, "oxytoxy", false,
            UpgradeToxinOrganelle.MutationDirection.Both),
        new UpgradeToxinOrganelle(organelle => organelle.HasAgentVacuoleComponent, "none", true,
            UpgradeToxinOrganelle.MutationDirection.Both),
        new UpgradeToxinOrganelle(organelle => organelle.HasAgentVacuoleComponent, "macrolide", false,
            UpgradeToxinOrganelle.MutationDirection.Both),
        new UpgradeToxinOrganelle(organelle => organelle.HasAgentVacuoleComponent, "channel", false,
            UpgradeToxinOrganelle.MutationDirection.Both),
        new UpgradeToxinOrganelle(organelle => organelle.HasAgentVacuoleComponent,
            "oxygen_inhibitor", false, UpgradeToxinOrganelle.MutationDirection.Both),
        new UpgradeOrganelle(organelle => organelle.HasCiliaComponent, "pull", true),
        new UpgradeOrganelle(organelle => organelle.HasLysosomeComponent,
            new LysosomeUpgrades(SimulationParameters.Instance.GetEnzyme(Constants.CHITINASE_ENZYME))),
        new UpgradeOrganelle(organelle => organelle.HasLysosomeComponent,
            new LysosomeUpgrades(SimulationParameters.Instance.GetEnzyme(Constants.CELLULASE_ENZYME))),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, 150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Aggression, 150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Opportunism, 150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Fear, -150.0f),
        new ChangeMembraneType("single"),
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
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var atpFromGlucose = cache.GetCompoundGeneratedFrom(glucose, atp, microbeSpecies, patch.Biome);
        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

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
