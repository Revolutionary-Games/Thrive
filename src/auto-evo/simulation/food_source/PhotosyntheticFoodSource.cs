using System.Linq;

public class PhotosyntheticFoodSource : IFoodSource
{
    private readonly Compound sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private BiomeConditions biomeConditions;
    private float totalSunlight;

    public PhotosyntheticFoodSource(Patch patch)
    {
        biomeConditions = patch.Biome;
        totalSunlight = patch.Biome.Compounds[sunlight].Dissolved * Constants.AUTO_EVO_SUNLIGHT_ENERGY_AMOUNT;
    }

    public float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var photosynthesisingScore = 0.0f;
        foreach (var organelle in microbeSpecies.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.ContainsKey(sunlight))
                {
                    if (process.Process.Outputs.ContainsKey(glucose))
                    {
                        photosynthesisingScore += process.Process.Outputs[glucose]
                            / process.Process.Inputs[sunlight] / Constants.AUTO_EVO_GLUCOSE_USE_SCORE_DIVISOR;
                    }
                }
            }
        }

        var energyCost = ProcessSystem.ComputeEnergyBalance(
            microbeSpecies.Organelles.Organelles.Select(organelle => organelle.Definition),
            biomeConditions, microbeSpecies.MembraneType).FinalBalanceStationary;

        return photosynthesisingScore / energyCost;
    }

    public float TotalEnergyAvailable()
    {
        return totalSunlight;
    }
}
