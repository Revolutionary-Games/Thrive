using System;

public class ChemosyntheticNiche : INiche
{
    private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

    private Compound compound;
    private float totalCompound;

    public ChemosyntheticNiche(Patch patch, Compound compound)
    {
        this.compound = compound;
        if (patch.Biome.Compounds.ContainsKey(compound))
        {
            totalCompound = patch.Biome.Compounds[compound].Density * patch.Biome.Compounds[compound].Amount;
        }
        else
        {
            Console.Error.WriteLine("Compound " + compound.Name + "Not found!");
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
                    if (process.Process.Outputs.ContainsKey(Glucose))
                    {
                        compoundUseScore += process.Process.Outputs[Glucose]
                            / process.Process.Inputs[compound] / Constants.AUTO_EVO_GLUCOSE_USE_SCORE_DIVISOR;
                    }

                    if (process.Process.Outputs.ContainsKey(ATP))
                    {
                        compoundUseScore += process.Process.Outputs[ATP]
                            / process.Process.Inputs[compound] / Constants.AUTO_EVO_ATP_USE_SCORE_DIVISOR;
                    }
                }
            }
        }

        var energyCost = microbeSpecies.BaseOsmoregulationCost();

        return compoundUseScore / energyCost;
    }

    public float TotalEnergyAvailable()
    {
        return totalCompound * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;
    }
}
