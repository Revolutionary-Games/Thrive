using System.Linq;

public class EnvironmentalFoodSource : IFoodSource
{
    private readonly Compound compound;
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private BiomeConditions biomeConditions;
    private float totalEnvironmentalEnergySource;

    public EnvironmentalFoodSource(Patch patch, string compound, float constant)
    {
        biomeConditions = patch.Biome;
        this.compound = SimulationParameters.Instance.GetCompound(compound);
        totalEnvironmentalEnergySource = patch.Biome.Compounds[this.compound].Dissolved * constant;
    }

    public float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var photosynthesisingScore = 0.0f;
        foreach (var organelle in microbeSpecies.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.ContainsKey(compound))
                {
                    if (process.Process.Outputs.ContainsKey(glucose))
                    {
                        photosynthesisingScore += process.Process.Outputs[glucose]
                            / process.Process.Inputs[compound] / Constants.AUTO_EVO_GLUCOSE_USE_SCORE_DIVISOR;
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
        return totalEnvironmentalEnergySource;
    }
}
