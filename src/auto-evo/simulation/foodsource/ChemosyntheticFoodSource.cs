using System.Linq;

public class ChemosyntheticFoodSource : IFoodSource
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private BiomeConditions biomeConditions;
    private Compound compound;
    private float totalCompound;

    public ChemosyntheticFoodSource(Patch patch, Compound compound)
    {
        biomeConditions = patch.Biome;
        this.compound = compound;
        if (patch.Biome.Compounds.ContainsKey(compound))
        {
            totalCompound = patch.Biome.Compounds[compound].Density * patch.Biome.Compounds[compound].Amount;
        }
        else
        {
            totalCompound = 0.0f;
        }
    }

    public float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var compoundUseScore = 0.0f;
        foreach (var organelle in microbeSpecies.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.ContainsKey(compound))
                {
                    if (process.Process.Outputs.ContainsKey(glucose))
                    {
                        compoundUseScore += process.Process.Outputs[glucose]
                            / process.Process.Inputs[compound] / Constants.AUTO_EVO_GLUCOSE_USE_SCORE_DIVISOR;
                    }

                    if (process.Process.Outputs.ContainsKey(atp))
                    {
                        compoundUseScore += process.Process.Outputs[atp]
                            / process.Process.Inputs[compound] / Constants.AUTO_EVO_ATP_USE_SCORE_DIVISOR;
                    }
                }
            }
        }

        var energyCost = ProcessSystem.ComputeEnergyBalance(microbeSpecies.Organelles.Organelles.Select(organelle => organelle.Definition),
            biomeConditions, microbeSpecies.MembraneType).FinalBalanceStationary;

        return compoundUseScore / energyCost;
    }

    public float TotalEnergyAvailable()
    {
        return totalCompound * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;
    }
}
