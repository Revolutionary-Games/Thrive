namespace AutoEvo;

public class PredationEffectivenessPressure : SelectionPressure
{
    public readonly Species Prey;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_PREDATION_EFFECTIVENESS_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public PredationEffectivenessPressure(Species prey, float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
            AddOrganelleAnywhere.ThatCreateCompound("oxytoxy"),
            new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent, CommonMutationFunctions.Direction.Front),
            new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
                CommonMutationFunctions.Direction.Rear),
            new MoveOrganelleBack(organelle => organelle.HasMovementComponent),
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

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        // No Cannibalism
        if (species == Prey)
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
        patch.SpeciesInPatch.TryGetValue(Prey, out long population);

        return population * CommonPressureFunctions.GetOrganelleCount(Prey) *
            Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
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
