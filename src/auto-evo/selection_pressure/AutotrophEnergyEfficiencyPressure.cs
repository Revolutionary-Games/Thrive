namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Systems;

public class AutotrophEnergyEfficiencyPressure : SelectionPressure
{
    public Patch Patch;
    public Compound Compound;
    public Compound OutCompound;
    private readonly float weight;

    public AutotrophEnergyEfficiencyPressure(Patch patch, Compound compound, Compound outCompound, float weight) :
    base(
        weight,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            AddOrganelleAnywhere.ThatUseCompound(compound),
            new RemoveAnyOrganelle(),
        })
    {
        Patch = patch;
        Compound = compound;
        OutCompound = outCompound;
        EnergyProvided = 40000;
        this.weight = weight;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var compoundIn = 0.0f;
        var compoundOut = 0.0f;

        foreach (var organelle in species.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                // ... that uses the given compound (regardless of usage)
                if (process.Process.Inputs.TryGetValue(Compound, out var inputAmount))
                {
                    compoundIn += inputAmount;

                    if (process.Process.Outputs.TryGetValue(OutCompound, out var outputAmount))
                    {
                        compoundOut += outputAmount;
                    }
                }
            }
        }

        if (compoundOut <= 0)
            return -1;

        return compoundOut / compoundIn * weight;
    }
}
