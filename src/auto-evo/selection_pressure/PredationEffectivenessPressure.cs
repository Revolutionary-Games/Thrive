﻿namespace AutoEvo;

using System;

public class PredationEffectivenessPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("PREDATION_EFFECTIVENESS_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    public readonly Species Prey;
    private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
    private readonly Patch patch;
    private readonly float totalEnergy;

    public PredationEffectivenessPressure(Species prey, Patch patch, float weight) :
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

        totalEnergy = population * CommonPressureFunctions.GetOrganelleCount(prey) *
            Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;

        Prey = prey;
    }

    public override float Score(Species species, SimulationCache cache)
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
