namespace AutoEvo;

using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public class AvoidPredationSelectionPressure : SelectionPressure
{
    [JsonProperty]
    public readonly Species Predator;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString =
        new LocalizedString("MICHE_AVOID_PREDATION_SELECTION_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public AvoidPredationSelectionPressure(Species predator, float weight) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound(Compound.Oxytoxy),
        new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent,
            CommonMutationFunctions.Direction.Front),
        new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
            CommonMutationFunctions.Direction.Rear),
        new AddOrganelleAnywhere(organelle => organelle.HasSlimeJetComponent,
            CommonMutationFunctions.Direction.Rear),
        new MoveOrganelleBack(organelle => organelle.HasSlimeJetComponent),
        new MoveOrganelleBack(organelle => organelle.HasMovementComponent),
        new UpgradeOrganelle(organelle => organelle.HasSlimeJetComponent, SlimeJetComponent.MUCOCYST),
        new MoveOrganelleBack(organelle => organelle.HasSlimeJetComponent),
        new MoveOrganelleBack(organelle => organelle.HasMovementComponent),
        new ChangeMembraneType("double"),
        new ChangeMembraneType("cellulose"),
        new ChangeMembraneType("chitin"),
        new ChangeMembraneType("calciumCarbonate"),
        new ChangeMembraneType("silica"),
        new ChangeMembraneRigidity(true),
        new ChangeMembraneRigidity(false),
    ])
    {
        Predator = predator;
    }

    [JsonIgnore]
    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        var predationScore = cache.GetPredationScore(Predator, species, patch.Biome);

        if (predationScore <= 1)
        {
            return 2.0f - predationScore;
        }

        return 1 / predationScore;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }

    public override string ToString()
    {
        return $"{Name} ({Predator.FormattedName})";
    }
}
