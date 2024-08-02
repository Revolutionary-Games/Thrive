namespace AutoEvo;

public class AvoidPredationSelectionPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("MICHE_AVOID_PREDATION_SELECTION_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    public readonly Species Predator;
    public readonly Patch Patch;

    private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
    private static readonly MembraneType DoubleMembrane = SimulationParameters.Instance.GetMembrane("double");
    private static readonly MembraneType CelluloseMembrane = SimulationParameters.Instance.GetMembrane("cellulose");
    private static readonly MembraneType ChitinMembrane = SimulationParameters.Instance.GetMembrane("chitin");

    private static readonly MembraneType CalciumCarbonateMembrane =
        SimulationParameters.Instance.GetMembrane("calciumCarbonate");

    private static readonly MembraneType SilicaMembrane = SimulationParameters.Instance.GetMembrane("silica");

    public AvoidPredationSelectionPressure(Species predator, float weight, Patch patch) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound(Oxytoxy),
        new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent,
            CommonMutationFunctions.Direction.Front),
        new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
            CommonMutationFunctions.Direction.Rear),
        new ChangeMembraneType(DoubleMembrane),
        new ChangeMembraneType(CelluloseMembrane),
        new ChangeMembraneType(ChitinMembrane),
        new ChangeMembraneType(CalciumCarbonateMembrane),
        new ChangeMembraneType(SilicaMembrane),
        new ChangeMembraneRigidity(true),
        new ChangeMembraneRigidity(false),
    ])
    {
        Patch = patch;
        Predator = predator;
    }

    public override float Score(Species species, SimulationCache cache)
    {
        var predationScore = cache.GetPredationScore(Predator, species, Patch.Biome);

        if (predationScore <= 0)
        {
            return 1.0f;
        }

        return 1 / predationScore;
    }

    public override float GetEnergy()
    {
        return 0;
    }

    public override LocalizedString GetDescription()
    {
        return Name;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
