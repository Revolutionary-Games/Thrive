using System.Collections.Generic;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Creates oxygen based on photosynthesizers (and removes carbon). And does the vice versa for oxygen consumption
///   to balance things out.
/// </summary>
[JSONDynamicTypeAllowed]
public class PhotosynthesisProductionEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    public PhotosynthesisProductionEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        ApplyCompoundsAddition();
    }

    private void ApplyCompoundsAddition()
    {
        // These affect the final balance
        var outputModifier = 1.5f;
        var inputModifier = 1.0f;

        // This affects how fast the conditions change, but also the final balance somewhat
        var modifier = 0.00015f;

        List<TweakedProcess> microbeProcesses = [];

        var cloudSizes = new Dictionary<Compound, float>();

        var changesToApply = new Dictionary<Compound, float>();

        foreach (var patchKeyValue in targetWorld.Map.Patches)
        {
            var patch = patchKeyValue.Value;

            // Skip empty patches as they don't need handling
            if (patch.SpeciesInPatch.Count < 1)
                continue;

            float oxygenBalance = 0;
            float co2Balance = 0;

            foreach (var species in patch.SpeciesInPatch)
            {
                var resolvedTolerances = new ResolvedMicrobeTolerances
                {
                    ProcessSpeedModifier = 1,
                    OsmoregulationModifier = 1,
                    HealthModifier = 1,
                };

                // TODO: multicellular environmental tolerances
                if (species.Key is MicrobeSpecies microbeSpecies)
                {
                    resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
                        MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(microbeSpecies, patch.Biome));
                }

                var balanceModifier = ProcessSystem.CalculateSpeciesActiveProcessListForEffect(species.Key,
                    microbeProcesses, patch.Biome, resolvedTolerances, targetWorld.WorldSettings);

                foreach (var process in microbeProcesses)
                {
                    // Only handle relevant processes
                    if (!IsProcessRelevant(process.Process))
                        continue;

                    var effectiveSpeed =
                        ProcessSystem.CalculateEffectiveProcessSpeedForEffect(process, balanceModifier, patch.Biome,
                            resolvedTolerances.ProcessSpeedModifier);

                    if (effectiveSpeed <= 0)
                        continue;

                    // TODO: maybe photosynthesis should also only try to reach glucose balance of +0?

                    // Inputs take away compounds scaled by the speed and population of the species
                    foreach (var input in process.Process.Inputs)
                    {
                        if (input.Key.ID is Compound.Oxygen)
                        {
                            oxygenBalance -= input.Value * inputModifier * effectiveSpeed * species.Value;
                        }
                        else if (input.Key.ID is Compound.Carbondioxide)
                        {
                            co2Balance -= input.Value * inputModifier * effectiveSpeed * species.Value;
                        }
                    }

                    // Outputs generate the given compounds
                    foreach (var output in process.Process.Outputs)
                    {
                        if (output.Key.ID is Compound.Oxygen)
                        {
                            oxygenBalance += output.Value * outputModifier * effectiveSpeed * species.Value;
                        }
                        else if (output.Key.ID is Compound.Carbondioxide)
                        {
                            co2Balance += output.Value * outputModifier * effectiveSpeed * species.Value;
                        }
                    }
                }
            }

            // Scale the balances to make the changes less drastic
            oxygenBalance *= modifier;
            co2Balance *= modifier;

            changesToApply.Clear();

            if (oxygenBalance != 0)
                changesToApply[Compound.Oxygen] = oxygenBalance;

            if (co2Balance != 0)
                changesToApply[Compound.Carbondioxide] = co2Balance;

            if (changesToApply.Count > 0)
                patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changesToApply, cloudSizes);
        }
    }

    private bool IsProcessRelevant(BioProcess process)
    {
        // For now only oxygen and co2 processes are handled by this simplified production effect
        foreach (var input in process.Inputs)
        {
            if (input.Key.ID is Compound.Oxygen or Compound.Carbondioxide)
                return true;
        }

        foreach (var output in process.Outputs)
        {
            if (output.Key.ID is Compound.Oxygen or Compound.Carbondioxide)
                return true;
        }

        return false;
    }
}
