using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Creates oxygen based on photosynthesizers (and removes carbon). And does the vice versa for oxygen consumption
///   to balance things out. This is kind of a simplified version of <see cref="CompoundProductionEffect"/>
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
        var outputModifier = 1.0f;
        var inputModifier = 1.0f;

        List<TweakedProcess> microbeProcesses = [];

        var cloudSizes = new Dictionary<Compound, float>();

        var changesToApply = new Dictionary<Compound, float>();

        foreach (var patchKeyValue in targetWorld.Map.Patches)
        {
            var patch = patchKeyValue.Value;

            // Skip empty patches as they don't need handling
            if (patch.SpeciesInPatch.Count < 1)
                continue;

            float oxygenConsumed = 0;
            float oxygenProduced = 0;
            float co2Consumed = 0;
            float co2Produced = 0;

            foreach (var species in patch.SpeciesInPatch)
            {
                // Only microbial photosynthesis and respiration are taken into account
                if (species.Key is not MicrobeSpecies microbeSpecies)
                    continue;

                var balance = ProcessSystem.ComputeEnergyBalance(microbeSpecies.Organelles, patch.Biome,
                    microbeSpecies.MembraneType, false, false, targetWorld.WorldSettings, CompoundAmountType.Average,
                    false);

                float balanceModifier = 1;

                // Scale processes to not consume excess oxygen than what is actually needed. Though, see below which
                // actual processes use this modifier.
                if (balance.TotalConsumption < balance.TotalProduction)
                    balanceModifier = balance.TotalConsumption / balance.TotalProduction;

                // Cleared for efficiency
                microbeProcesses.Clear();
                ProcessSystem.ComputeActiveProcessList(microbeSpecies.Organelles, ref microbeProcesses);

                // Iterate over each process and determine compounds produced and consumed
                foreach (var process in microbeProcesses)
                {
                    if (process.Process.InternalName == "protein_respiration")
                        _ = 1;

                    // Only handle relevant processes
                    if (!IsProcessRelevant(process.Process))
                        continue;

                    var rate = ProcessSystem.CalculateProcessMaximumSpeed(process,
                        patch.Biome, CompoundAmountType.Biome, true);

                    // Skip checking processes that cannot run
                    if (rate.CurrentSpeed <= 0)
                        continue;

                    // For metabolic processes the speed is at most to reach ATP equilibrium in order to not
                    // unnecessarily consume environmental oxygen
                    var effectiveSpeed =
                        (process.Process.IsMetabolismProcess ? balanceModifier : 1) * rate.CurrentSpeed;

                    // TODO: maybe photosynthesis should also only try to reach glucose balance of +0?

                    // Inputs take away compounds scaled by the speed and population of the species
                    foreach (var input in process.Process.Inputs)
                    {
                        if (input.Key.ID is Compound.Oxygen)
                        {
                            oxygenConsumed += input.Value * inputModifier * effectiveSpeed * species.Value;
                        }
                        else if (input.Key.ID is Compound.Carbondioxide)
                        {
                            co2Consumed += input.Value * inputModifier * effectiveSpeed * species.Value;
                        }
                    }

                    // Outputs generate the given compounds
                    foreach (var output in process.Process.Outputs)
                    {
                        if (output.Key.ID is Compound.Oxygen)
                        {
                            oxygenProduced += output.Value * outputModifier * effectiveSpeed * species.Value;
                        }
                        else if (output.Key.ID is Compound.Carbondioxide)
                        {
                            co2Produced += output.Value * outputModifier * effectiveSpeed * species.Value;
                        }
                    }
                }
            }

            patch.Biome.TryGetCompound(Compound.Oxygen, CompoundAmountType.Biome, out var existingOxygen);
            patch.Biome.TryGetCompound(Compound.Carbondioxide, CompoundAmountType.Biome, out var existingCo2);

            // Unless something else changes it, compound values stay the same
            float oxygenTarget = existingOxygen.Ambient;
            float co2Target = existingCo2.Ambient;

            float total = existingOxygen.Ambient + existingCo2.Ambient;

            // Special (but common) case where zero oxygen is being produced
            if (oxygenProduced == 0 && oxygenConsumed > 0)
            {
                oxygenTarget = 0.0f;

                if (co2Produced > 0)
                {
                    co2Target = total;
                }
            }

            // Special (but common) case where zero carbon dioxide is being produced
            if (co2Produced == 0 && co2Consumed > 0)
            {
                co2Target = 0.0f;

                if (oxygenProduced > 0)
                {
                    oxygenTarget = total;
                }
            }

            // if both compounds are being produced, calculate an aproximate steady state value
            if (oxygenProduced > 0 && co2Produced > 0)
            {
                // Calculate long-term equilibrium balances based on production and consumption ratio
                oxygenTarget = oxygenProduced / (oxygenConsumed + co2Consumed + MathUtils.EPSILON) * total;
                co2Target = co2Produced / (oxygenConsumed + co2Consumed + MathUtils.EPSILON) * total;
            }

            changesToApply[Compound.Oxygen] = (oxygenTarget - existingOxygen.Ambient) * 0.5f;
            changesToApply[Compound.Carbondioxide] = (co2Target - existingCo2.Ambient) * 0.5f;

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
