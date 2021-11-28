using AutoEvo;

public abstract class FoodSource
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    public abstract float TotalEnergyAvailable();

    /// <summary>
    ///   Provides a fitness metric to determine population adjustments for species in a patch.
    /// </summary>
    /// <param name="microbe">The species to be evaluated.</param>
    /// <param name="simulationCache">
    ///   Cache that should be used to reduce amount of times expensive computations are ran
    /// </param>
    /// <returns>
    ///   A float to represent score. Scores are only compared against other scores from the same FoodSource,
    ///   so different implementations do not need to worry about scale.
    /// </returns>
    public abstract float FitnessScore(Species microbe, SimulationCache simulationCache);

    protected float EnergyGenerationScore(MicrobeSpecies species, Compound compound)
    {
        var energyCreationScore = 0.0f;
        foreach (var organelle in species.Organelles)
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

                    if (process.Process.Outputs.ContainsKey(atp))
                    {
                        energyCreationScore += process.Process.Outputs[atp]
                            / process.Process.Inputs[compound] * Constants.AUTO_EVO_ATP_USE_SCORE_MULTIPLIER;
                    }
                }
            }
        }

        return energyCreationScore;
    }
}
