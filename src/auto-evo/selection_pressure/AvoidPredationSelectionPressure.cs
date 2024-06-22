namespace AutoEvo;

public class AvoidPredationSelectionPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("AVOID_PREDATION_SELECTION_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    public readonly Species Predator;
    public readonly Patch Patch;

    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

    private readonly float weight;

    public AvoidPredationSelectionPressure(Species predator, float weight, Patch patch) : base(weight,
        [
            AddOrganelleAnywhere.ThatCreateCompound(Oxytoxy),
            new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent,
                AddOrganelleAnywhere.Direction.Front),
            new AddMultipleOrganelles([
                new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
                    AddOrganelleAnywhere.Direction.Rear),
                AddOrganelleAnywhere.ThatCreateCompound(ATP),
            ]),
        ],
        0)
    {
        Patch = patch;
        Predator = predator;
        this.weight = weight;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var predationScore = new PredationEffectivenessPressure(species, Patch, 1, cache)
            .FitnessScore((MicrobeSpecies)Predator, species);

        if (predationScore == 0)
        {
            return 1.0f * weight;
        }

        return 1 / predationScore * weight;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
