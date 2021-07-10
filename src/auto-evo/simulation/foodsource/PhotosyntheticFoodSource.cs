public class PhotosyntheticFoodSource : IFoodSource
{
    private readonly Compound sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private float totalSunlight;

    public PhotosyntheticFoodSource(Patch patch)
    {
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

                    if (process.Process.Outputs.ContainsKey(atp))
                    {
                        photosynthesisingScore += process.Process.Outputs[atp]
                            / process.Process.Inputs[sunlight] / Constants.AUTO_EVO_ATP_USE_SCORE_DIVISOR;
                    }
                }
            }
        }

        // Moving too much can be harmfull
        var energyCost = microbeSpecies.BaseOsmoregulationCost();
        energyCost *= 1 + (microbeSpecies.Activity / Constants.MAX_SPECIES_ACTIVITY);

        return photosynthesisingScore / energyCost;
    }

    public float TotalEnergyAvailable()
    {
        return totalSunlight;
    }
}
