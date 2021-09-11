using System.Linq;

public class HeterotrophicFoodSource : FoodSource
{
    private readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

    private MicrobeSpecies prey;
    private Patch patch;
    private float preySpeed;
    private float preySize;
    private float totalEnergy;

    public HeterotrophicFoodSource(Patch patch, MicrobeSpecies prey)
    {
        this.prey = prey;
        this.patch = patch;
        preySpeed = prey.BaseSpeed;
        preySize = prey.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);
        patch.SpeciesInPatch.TryGetValue(prey, out long population);
        totalEnergy = population * prey.Organelles.Count * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
    }

    public override float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        // No cannibalism
        if (species == prey)
        {
            return 0.0f;
        }

        var behaviorScore = microbeSpecies.Aggression / Constants.MAX_SPECIES_AGGRESSION;

        var predatorSize = microbeSpecies.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);
        var predatorSpeed = microbeSpecies.BaseSpeed;
        predatorSpeed += ProcessSystem
            .ComputeEnergyBalance(microbeSpecies.Organelles.Organelles, patch.Biome,
                microbeSpecies.MembraneType).FinalBalance;

        // It's great if you can engulf this prey, but only if you can catch it
        var engulfScore = 0.0f;
        if (predatorSize / preySize > Constants.ENGULF_SIZE_RATIO_REQ && !microbeSpecies.MembraneType.CellWall)
        {
            engulfScore = Constants.AUTO_EVO_ENGULF_PREDATION_SCORE;
        }

        engulfScore *= predatorSpeed > preySpeed ? 1.0f : Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

        var pilusScore = 0.0f;
        var oxytoxyScore = 0.0f;
        foreach (var organelle in microbeSpecies.Organelles)
        {
            if (organelle.Definition.HasComponentFactory<PilusComponentFactory>())
            {
                pilusScore += Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
                continue;
            }

            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Outputs.ContainsKey(oxytoxy))
                {
                    oxytoxyScore += process.Process.Outputs[oxytoxy] * Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                }
            }
        }

        // Pili are much more useful if the microbe can close to melee
        pilusScore *= predatorSpeed;

        // predators are less likely to use toxin against larger prey, unless they are opportunistic
        if (preySize > predatorSize)
        {
            oxytoxyScore *= microbeSpecies.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
        }

        // Intentionally don't penalize for osmoregulation cost to encourage larger monsters
        return behaviorScore * (pilusScore + engulfScore + predatorSize + oxytoxyScore);
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnergy;
    }
}
