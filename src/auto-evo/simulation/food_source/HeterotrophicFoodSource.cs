namespace AutoEvo;

using System;

public class HeterotrophicFoodSource : RandomEncounterFoodSource
{
    private readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
    private readonly Compound mucilage = SimulationParameters.Instance.GetCompound("mucilage");

    private readonly MicrobeSpecies prey;
    private readonly Patch patch;
    private readonly float preyHexSize;
    private readonly float preySpeed;
    private readonly float totalEnergy;

    public HeterotrophicFoodSource(Patch patch, MicrobeSpecies prey, SimulationCache simulationCache)
    {
        this.prey = prey;
        this.patch = patch;
        preyHexSize = simulationCache.GetBaseHexSizeForSpecies(prey);
        preySpeed = simulationCache.GetBaseSpeedForSpecies(prey);
        patch.SpeciesInPatch.TryGetValue(prey, out long population);
        totalEnergy = population * prey.Organelles.Count * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
    }

    public override float FitnessScore(Species species, SimulationCache simulationCache,
        WorldGenerationSettings worldSettings)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        // No cannibalism
        if (microbeSpecies == prey)
        {
            return 0.0f;
        }

        var behaviourScore = microbeSpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;

        // TODO: if these two methods were combined it might result in better performance with needing just
        // one dictionary lookup
        var microbeSpeciesHexSize = simulationCache.GetBaseHexSizeForSpecies(microbeSpecies);
        var predatorSpeed = simulationCache.GetBaseSpeedForSpecies(microbeSpecies);

        predatorSpeed += simulationCache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome).FinalBalance;

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

        var predationToolsRawScores = simulationCache.GetPredationToolsRawScores(microbeSpecies);
        var pilusScore = predationToolsRawScores.PilusScore;
        var oxytoxyScore = predationToolsRawScores.OxytoxyScore;
        var mucilageScore = predationToolsRawScores.MucilageScore;

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

    public override IFormattable GetDescription()
    {
        return new LocalizedString("PREDATION_FOOD_SOURCE", prey.FormattedNameBbCode);
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnergy;
    }
}
