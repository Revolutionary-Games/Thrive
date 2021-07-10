using System.Linq;

public class HeterotrophicFoodSource : IFoodSource
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
        preySpeed = prey.BaseSpeed();
        preySize = prey.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);
        patch.SpeciesInPatch.TryGetValue(prey, out long population);
        totalEnergy = population * prey.BaseOsmoregulationCost() * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
    }

    public float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        // No canibalism
        if (species == prey)
        {
            return 0.0f;
        }

        var behaviorScore = microbeSpecies.Aggression / Constants.MAX_SPECIES_AGGRESSION;

        var predatorSize = microbeSpecies.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);
        var predatorSpeed = microbeSpecies.BaseSpeed();
        predatorSpeed += ProcessSystem.ComputeEnergyBalance(microbeSpecies.Organelles.Organelles.Select(x => x.Definition), patch.Biome, microbeSpecies.MembraneType).FinalBalance;

        // It's great if you can engulf this prey, but only if you can catch it
        var engulfScore = predatorSize / preySize > Constants.ENGULF_SIZE_RATIO_REQ
            && !microbeSpecies.MembraneType.CellWall ?
                Constants.AUTO_EVO_ENGULF_PREDATION_SCORE :
            0.0f;
        engulfScore *= predatorSpeed > preySpeed ? 1.0f : 0.1f;

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
                    oxytoxyScore += Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                }
            }
        }

        // Piluses are much more usefull if the microbe can close to melee
        pilusScore *= predatorSpeed;

        // Intentionally don't penalize for osmoregulation cost to encourage larger monsters
        return behaviorScore * (pilusScore + engulfScore + predatorSize + oxytoxyScore);
    }

    public float TotalEnergyAvailable()
    {
        return totalEnergy;
    }
}
