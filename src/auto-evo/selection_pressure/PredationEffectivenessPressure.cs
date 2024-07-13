namespace AutoEvo;

using System;

public class PredationEffectivenessPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("PREDATION_EFFECTIVENESS_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    public readonly MicrobeSpecies Prey;
    private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
    private readonly Patch patch;
    private readonly float totalEnergy;

    public PredationEffectivenessPressure(MicrobeSpecies prey, Patch patch, float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
            AddOrganelleAnywhere.ThatCreateCompound(Oxytoxy),
            new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent, CommonMutationFunctions.Direction.Front),
            new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
                CommonMutationFunctions.Direction.Rear),
            new MoveOrganelleBack(organelle => organelle.HasMovementComponent),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Aggression, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Opportunism, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Fear, -150.0f),
            new ChangeMembraneRigidity(true),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
        ])
    {
        this.patch = patch;

        patch.SpeciesInPatch.TryGetValue(prey, out long population);
        totalEnergy = population * prey.Organelles.Count * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;

        Prey = prey;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        // No Cannibalism
        if (species == Prey)
        {
            return 0.0f;
        }

        var microbeSpecies = species;
        var microbePrey = Prey;

        var predatorScore = cache.GetPredationScore(microbeSpecies, microbePrey, patch.Biome);
        var reversePredatorScore = cache.GetPredationScore(microbePrey, microbeSpecies, patch.Biome);

        // Explicitly prohibit circular predation relationships
        if (reversePredatorScore > predatorScore)
        {
            return 0.0f;
        }

        return predatorScore;
    }

    public override float GetEnergy()
    {
        return totalEnergy;
    }

    public override IFormattable GetDescription()
    {
        return new LocalizedString("PREDATION_FOOD_SOURCE", Prey.FormattedNameBbCode);
    }

    public override string ToString()
    {
        return $"{Name} ({Prey.FormattedName})";
    }
}
