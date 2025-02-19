namespace AutoEvo;

using System;
using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public class MaintainCompoundPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_MAINTAIN_COMPOUND_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly CompoundDefinition compound;

    // Needed for saving to work
    [JsonProperty(nameof(compound))]
    private readonly Compound compoundRaw;

    public MaintainCompoundPressure(Compound compound, float weight) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound(compound),
        RemoveOrganelle.ThatUseCompound(compound),
    ])
    {
        compoundRaw = compound;
        this.compound = SimulationParameters.GetCompound(compound);
    }

    [JsonIgnore]
    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var compoundUsed = 0.0f;
        var compoundCreated = 0.0f;

        var biomeConditions = patch.Biome;
        var resolvedTolerances = cache.GetEnvironmentalTolerances(microbeSpecies, biomeConditions);

        for (var i = 0; i < microbeSpecies.Organelles.Count; ++i)
        {
            var organelle = microbeSpecies.Organelles[i];
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                {
                    var processSpeed = cache
                        .GetProcessMaximumSpeed(process, resolvedTolerances.ProcessSpeedModifier, biomeConditions)
                        .CurrentSpeed;

                    compoundUsed += inputAmount * processSpeed;
                }

                if (process.Process.Outputs.TryGetValue(compound, out var outputAmount))
                {
                    var processSpeed = cache
                        .GetProcessMaximumSpeed(process, resolvedTolerances.ProcessSpeedModifier, biomeConditions)
                        .CurrentSpeed;

                    compoundCreated += outputAmount * processSpeed;
                }
            }
        }

        if (compoundCreated <= 0 || compoundUsed <= 0)
            return 0.0f;

        return MathF.Min(compoundCreated / compoundUsed, 1);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
