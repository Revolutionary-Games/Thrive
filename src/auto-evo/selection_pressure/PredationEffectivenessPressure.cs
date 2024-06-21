namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public class PredationEffectivenessPressure : SelectionPressure
{
    public static readonly LocalizedString Name = new LocalizedString("PREDATION_EFFECTIVENESS_PRESSURE");
    public readonly MicrobeSpecies Prey;
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

    private readonly SimulationCache cache;
    private readonly Patch patch;
    private readonly float weight;

    public PredationEffectivenessPressure(MicrobeSpecies prey, Patch patch, float weight, SimulationCache cache) :
    base(
        weight,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
            AddOrganelleAnywhere.ThatCreateCompound(Oxytoxy),
            new AddOrganelleAnywhere(organelle => organelle.HasPilusComponent,
                AddOrganelleAnywhere.Direction.FRONT),
            new AddMultipleOrganelles(new List<AddOrganelleAnywhere>
            {
                new AddOrganelleAnywhere(organelle => organelle.HasMovementComponent,
                    AddOrganelleAnywhere.Direction.REAR),
                AddOrganelleAnywhere.ThatCreateCompound(ATP),
            }),

            // new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.AGGRESSION, 150.0f),
            // new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.OPPORTUNISM, 150.0f),
            // new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.FEAR, -150.0f),
            new RemoveAnyOrganelle(),
            new LowerRigidity(),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
        },
        1000)
    {
        this.cache = cache;
        this.patch = patch;
        this.weight = weight;

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
            return -1.0f;
        }

        var microbeSpecies = species;
        var microbePrey = Prey;

        var predatorScore = FitnessScore(microbeSpecies, microbePrey);
        var reversePredatorScore = FitnessScore(microbePrey, microbeSpecies);

        // Explicitly prohibit circular predation relationships
        if (reversePredatorScore > predatorScore)
        {
            return -1.0f;
        }

        return predatorScore * weight;
    }

    public override string ToString()
    {
        return $"{Name} ({Prey.FormattedName})";
    }
}
