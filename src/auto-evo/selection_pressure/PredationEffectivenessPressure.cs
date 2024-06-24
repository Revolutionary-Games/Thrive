namespace AutoEvo;

using System;

public class PredationEffectivenessPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("PREDATION_EFFECTIVENESS_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    public readonly MicrobeSpecies Prey;
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

    private readonly SimulationCache cache;
    private readonly Patch patch;
    private readonly float totalEnergy;

    public PredationEffectivenessPressure(MicrobeSpecies prey, Patch patch, float weight, SimulationCache cache) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
            AddOrganelleAnywhere.ThatCreateCompound(Oxytoxy),
            new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent,
                AddOrganelleAnywhere.Direction.Front),
            new AddMultipleOrganelles([
                new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
                    AddOrganelleAnywhere.Direction.Rear),
                AddOrganelleAnywhere.ThatCreateCompound(ATP),
            ]),

            // new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.AGGRESSION, 150.0f),
            // new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.OPPORTUNISM, 150.0f),
            // new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.FEAR, -150.0f),
            new RemoveOrganelle(_ => true),
            new LowerRigidity(),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
        ])
    {
        this.cache = cache;
        this.patch = patch;

        patch.SpeciesInPatch.TryGetValue(prey, out long population);
        totalEnergy = population * prey.Organelles.Count * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;

        Prey = prey;
    }

    public float FitnessScore(MicrobeSpecies microbeSpecies, MicrobeSpecies prey)
    {
        // No cannibalism
        if (microbeSpecies == prey)
        {
            return 0.0f;
        }

        var preyHexSize = cache.GetBaseHexSizeForSpecies(prey);
        var preySpeed = cache.GetBaseSpeedForSpecies(prey);

        var behaviourScore = microbeSpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;

        // TODO: if these two methods were combined it might result in better performance with needing just
        // one dictionary lookup
        var microbeSpeciesHexSize = cache.GetBaseHexSizeForSpecies(microbeSpecies);
        var predatorSpeed = cache.GetBaseSpeedForSpecies(microbeSpecies);

        predatorSpeed += cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome).FinalBalance;

        // Only assign engulf score if one can actually engulf
        var engulfScore = 0.0f;
        if (microbeSpeciesHexSize / preyHexSize >
            Constants.ENGULF_SIZE_RATIO_REQ && microbeSpecies.CanEngulf)
        {
            // Catch scores grossly accounts for how many preys you catch in a run;
            var catchScore = 0.0f;

            // First, you may hunt individual preys, but only if you are fast enough...
            if (predatorSpeed > preySpeed)
            {
                // You catch more preys if you are fast, and if they are slow.
                // This incentivizes engulfment strategies in these cases.
                catchScore += predatorSpeed / preySpeed;
            }

            // ... but you may also catch them by luck (e.g. when they run into you),
            // and this is especially easy if you're huge.
            // This is also used to incentivize size in microbe species.
            catchScore += Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY * microbeSpeciesHexSize;

            // Allow for some degree of lucky engulfment
            engulfScore = catchScore * Constants.AUTO_EVO_ENGULF_PREDATION_SCORE;
        }

        var (pilusScore, oxytoxyScore, mucilageScore) = cache.GetPredationToolsRawScores(microbeSpecies);

        // Pili are much more useful if the microbe can close to melee
        pilusScore *= predatorSpeed > preySpeed ? 1.0f : Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

        // predators are less likely to use toxin against larger prey, unless they are opportunistic
        if (preyHexSize > microbeSpeciesHexSize)
        {
            oxytoxyScore *= microbeSpecies.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
        }

        // Intentionally don't penalize for osmoregulation cost to encourage larger monsters
        return behaviourScore * (pilusScore + engulfScore + oxytoxyScore + mucilageScore);
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        // No Canabalism
        if (species == Prey)
        {
            return 0.0f;
        }

        var microbeSpecies = species;
        var microbePrey = Prey;

        var predatorScore = FitnessScore(microbeSpecies, microbePrey);
        var reversePredatorScore = FitnessScore(microbePrey, microbeSpecies);

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
