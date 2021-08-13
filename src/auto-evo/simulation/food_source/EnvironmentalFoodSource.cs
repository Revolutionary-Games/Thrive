public class EnvironmentalFoodSource : IFoodSource
{
    private readonly Compound compound;
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private BiomeConditions biomeConditions;
    private float totalEnvironmentalEnergySource;

    public EnvironmentalFoodSource(Patch patch, string compound, float foodCapacityMultiplier)
    {
        biomeConditions = patch.Biome;
        this.compound = SimulationParameters.Instance.GetCompound(compound);
        totalEnvironmentalEnergySource = patch.Biome.Compounds[this.compound].Dissolved * foodCapacityMultiplier;
    }

    public float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var energyCreationScore = 0.0f;
        foreach (var organelle in microbeSpecies.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.ContainsKey(compound))
                {
                    if (process.Process.Outputs.ContainsKey(glucose))
                    {
                        energyCreationScore += process.Process.Outputs[glucose]
                            / process.Process.Inputs[compound] * Constants.AUTO_EVO_GLUCOSE_USE_SCORE_MULTIPLIER;
                    }
                }
            }
        }

        var energyCost = ProcessSystem.ComputeEnergyBalance(
            microbeSpecies.Organelles.Organelles,
            biomeConditions, microbeSpecies.MembraneType).FinalBalanceStationary;

        return energyCreationScore / energyCost;
    }

    public float TotalEnergyAvailable()
    {
        return totalEnvironmentalEnergySource;
    }
}
