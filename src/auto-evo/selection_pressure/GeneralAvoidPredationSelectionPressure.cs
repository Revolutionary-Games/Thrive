namespace AutoEvo;

using SharedBase.Archive;

public class GeneralAvoidPredationSelectionPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString =
        new LocalizedString("MICHE_AVOID_PREDATION_SELECTION_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public GeneralAvoidPredationSelectionPressure(float weight) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound(Compound.Oxytoxy),
        new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent,
            CommonMutationFunctions.Direction.Rear),
        new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
            CommonMutationFunctions.Direction.Rear),
        new AddOrganelleAnywhere(organelle => organelle.HasSlimeJetComponent,
            CommonMutationFunctions.Direction.Rear),
        new AddOrganelleAnywhere(organelle => organelle.HasCiliaComponent),
        new MoveOrganelleBack(organelle => organelle.HasPilusComponent),
        new MoveOrganelleBack(organelle => organelle.HasSlimeJetComponent),
        new MoveOrganelleBack(organelle => organelle.HasMovementComponent),
        new UpgradeOrganelle(organelle => organelle.HasPilusComponent, "injectisome", true),
        new UpgradeOrganelle(organelle => organelle.HasSlimeJetComponent, SlimeJetComponent.MUCOCYST_UPGRADE_NAME,
            true),
        new UpgradeOrganelle(organelle => organelle.HasMovementComponent, new FlagellumUpgrades(0.5f)),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Opportunism, -150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Fear, 150.0f),
        new ChangeMembraneType("double"),
        new ChangeMembraneType("cellulose"),
        new ChangeMembraneType("chitin"),
        new ChangeMembraneType("calciumCarbonate"),
        new ChangeMembraneType("silica"),
        new ChangeMembraneRigidity(true),
        new ChangeMembraneRigidity(false),
    ])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.GeneralAvoidPredationSelectionPressure;

    public static GeneralAvoidPredationSelectionPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new GeneralAvoidPredationSelectionPressure(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        var score = 1.0f;
        foreach (var predator in patch.SpeciesInPatch)
        {
            // No Cannibalism
            // Compared by ID here to make sure temporary species variants are not allowed to predate themselves
            if (species.ID == predator.Key.ID)
            {
                continue;
            }

            var predationScore = cache.GetPredationScore(predator.Key, species, patch.Biome);

            if (predationScore <= 1)
            {
                score += 3.0f - predationScore;
                continue;
            }

            score += 1 / predationScore;
        }

        return score;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
