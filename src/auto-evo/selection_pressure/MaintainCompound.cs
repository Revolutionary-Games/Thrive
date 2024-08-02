namespace AutoEvo;

using Godot;

public class MaintainCompound : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_MAINTAIN_COMPOUND_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    private readonly Patch patch;
    private readonly Compound compound;

    public MaintainCompound(Patch patch, float weight, Compound compound) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound(compound),
        RemoveOrganelle.ThatUseCompound(compound),
    ])
    {
        this.patch = patch;
        this.compound = compound;
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var compoundUsed = 0.0f;
        var compoundCreated = 0.0f;

        foreach (var organelle in microbeSpecies.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                {
                    var processSpeed = cache.GetProcessMaximumSpeed(process, patch.Biome).CurrentSpeed;

                    compoundUsed += inputAmount * processSpeed;
                }

                if (process.Process.Outputs.TryGetValue(compound, out var outputAmount))
                {
                    var processSpeed = cache.GetProcessMaximumSpeed(process, patch.Biome).CurrentSpeed;

                    compoundCreated += outputAmount * processSpeed;
                }
            }
        }

        if (compoundCreated <= 0 || compoundUsed <= 0)
            return 0.0f;

        return Mathf.Min(compoundCreated / compoundUsed, 1);
    }

    public override float GetEnergy()
    {
        return 0;
    }

    public override LocalizedString GetDescription()
    {
        return Name;
    }
}
