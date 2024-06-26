namespace AutoEvo;

using System;

public class AutotrophEnergyEfficiencyPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("AUTOTROPH_ENERGY_EFFICIENCY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public readonly Patch Patch;
    public readonly Compound Compound;
    public readonly Compound OutCompound;

    public AutotrophEnergyEfficiencyPressure(Patch patch, Compound compound, Compound outCompound, float weight) :
        base(weight, [
            AddOrganelleAnywhere.ThatUseCompound(compound),
            RemoveOrganelle.ThatCreateCompound(compound),
        ])
    {
        Patch = patch;
        Compound = compound;
        OutCompound = outCompound;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var compoundIn = 0.0f;
        var compoundOut = 0.0f;

        foreach (var organelle in species.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.TryGetValue(Compound, out var inputAmount))
                {
                    if (process.Process.Outputs.TryGetValue(OutCompound, out var outputAmount))
                    {
                        compoundIn += inputAmount;
                        compoundOut += outputAmount;
                    }
                }
                else if (process.Process.Outputs.TryGetValue(OutCompound, out var outputAmount))
                {
                    // Penalize other ways to generate the compound to encourage specialization
                    // TODO: Make constant
                    compoundOut -= outputAmount * 0.3f;
                }
            }
        }

        if (compoundIn <= 0)
            return 0;

        return compoundOut / compoundIn;
    }

    public override float GetEnergy()
    {
        return 0;
    }

    public override IFormattable GetDescription()
    {
        // This shouldn't be called on 0 energy pressures
        return Name;
    }

    public override string ToString()
    {
        return $"{Name} ({Compound.Name})";
    }
}
