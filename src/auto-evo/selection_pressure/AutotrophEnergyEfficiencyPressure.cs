namespace AutoEvo;

public class AutotrophEnergyEfficiencyPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("AUTOTROPH_ENERGY_EFFICIENCY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public readonly Patch Patch;
    public readonly Compound Compound;
    public readonly Compound OutCompound;
    private readonly float weight;

    public AutotrophEnergyEfficiencyPressure(Patch patch, Compound compound, Compound outCompound, float weight) :
        base(weight,
            [
                AddOrganelleAnywhere.ThatUseCompound(compound),
                new RemoveAnyOrganelle(),
            ],
            40000)
    {
        Patch = patch;
        Compound = compound;
        OutCompound = outCompound;
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

    public override string ToString()
    {
        return $"{Name} ({Compound.Name})";
    }
}
