namespace AutoEvo;

public class AvoidPredationSelectionPressure : SelectionPressure
{
    public readonly Species Predator;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString =
        new LocalizedString("MICHE_AVOID_PREDATION_SELECTION_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public AvoidPredationSelectionPressure(Species predator, float weight) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound("oxytoxy"),
        new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent,
            CommonMutationFunctions.Direction.Front),
        new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
            CommonMutationFunctions.Direction.Rear),
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

    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        var predationScore = cache.GetPredationScore(Predator, species, patch.Biome);

        if (predationScore <= 0)
        {
            return 1.0f;
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
