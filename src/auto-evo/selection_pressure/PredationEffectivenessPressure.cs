namespace AutoEvo;

using SharedBase.Archive;

public class PredationEffectivenessPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly Species Prey;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_PREDATION_EFFECTIVENESS_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public PredationEffectivenessPressure(Species prey, float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
            AddOrganelleAnywhere.ThatCreateCompound(Compound.Oxytoxy),
            new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent, CommonMutationFunctions.Direction.Front),
            new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
                CommonMutationFunctions.Direction.Rear),
            new AddOrganelleAnywhere(organelle => organelle.HasSlimeJetComponent,
                CommonMutationFunctions.Direction.Rear),
            new AddOrganelleAnywhere(organelle => organelle.HasLysosomeComponent),
            new AddOrganelleAnywhere(organelle => organelle.HasChemoreceptorComponent),
            new AddOrganelleAnywhere(organelle => organelle.HasCiliaComponent),
            new MoveOrganelleBack(organelle => organelle.HasSlimeJetComponent),
            new MoveOrganelleBack(organelle => organelle.HasMovementComponent),
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
            new UpgradeOrganelle(organelle => organelle.HasPilusComponent, "injectisome", true),
            new UpgradeOrganelle(organelle => organelle.HasCiliaComponent, "pull", true),
            new UpgradeOrganelle(organelle => organelle.HasMovementComponent, new FlagellumUpgrades(0.5f)),
            new UpgradeOrganelle(organelle => organelle.HasLysosomeComponent,
                new LysosomeUpgrades(SimulationParameters.Instance.GetEnzyme(Constants.CHITINASE_ENZYME))),
            new UpgradeOrganelle(organelle => organelle.HasLysosomeComponent,
                new LysosomeUpgrades(SimulationParameters.Instance.GetEnzyme(Constants.CELLULASE_ENZYME))),
            new UpgradeOrganelle(organelle => organelle.HasChemoreceptorComponent,
                new ChemoreceptorUpgrades(Compound.Invalid, prey, Constants.CHEMORECEPTOR_RANGE_DEFAULT,
                    Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, prey.SpeciesColour)),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Aggression, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Opportunism, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Fear, -150.0f),
            new ChangeMembraneRigidity(true),
            new ChangeMembraneType("single"),
        ])
    {
        Prey = prey;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.PredationEffectivenessPressure;

    public static PredationEffectivenessPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new PredationEffectivenessPressure(reader.ReadObject<Species>(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Prey);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        // No Cannibalism
        // Compared by ID here to make sure temporary species variants are not allowed to predate themselves
        if (species.ID == Prey.ID)
        {
            return 0.0f;
        }

        var predatorScore = cache.GetPredationScore(species, Prey, patch.Biome);
        var reversePredatorScore = cache.GetPredationScore(Prey, species, patch.Biome);

        // Explicitly prohibit circular predation relationships
        if (reversePredatorScore > predatorScore)
        {
            return 0.0f;
        }

        return predatorScore;
    }

    public override float GetEnergy(Patch patch)
    {
        if (!patch.SpeciesInPatch.TryGetValue(Prey, out long population) || population <= 0)
            return 0;

        return population * Prey.GetPredationTargetSizeFactor() * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("PREDATION_FOOD_SOURCE", Prey.FormattedNameBbCode);
    }

    public override string ToString()
    {
        return $"{Name} ({Prey.FormattedName})";
    }
}
