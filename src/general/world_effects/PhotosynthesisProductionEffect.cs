using System;
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
        var outputModifier = 1.5f;
        var inputModifier = 1.0f;

        // This affects how fast the conditions change, but also the final balance somewhat
        var modifier = 0.00015f;

        List<TweakedProcess> microbeProcesses = [];

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

            // TODO: add patch volumes, calculate absolute values and after that go back to fractional values

            if (oxygenBalance != 0)
                ApplyCompoundChanges(patch, Compound.Oxygen, oxygenBalance);

            if (co2Balance != 0)
                ApplyCompoundChanges(patch, Compound.Carbondioxide, co2Balance);
        }
    }

    private void ApplyCompoundChanges(Patch patch, Compound compound, float change)
    {
        if (patch.Biome.TryGetCompound(compound, CompoundAmountType.Biome, out var tweakedBiomeConditions))
        {
            if (SimulationParameters.Instance.GetCompoundDefinition(compound).IsEnvironmental)
            {
                tweakedBiomeConditions.Ambient = Math.Clamp(tweakedBiomeConditions.Ambient + change, 0, 1);
            }
            else
            {
                tweakedBiomeConditions.Density = Math.Clamp(tweakedBiomeConditions.Density + change, 0, 1);
            }
        }
        else
        {
            // New compound added to this patch
            if (change <= 0)
            {
                // If trying to add negative compound balance initially, it doesn't make sense so this is just skipped
                return;
            }

            if (SimulationParameters.Instance.GetCompoundDefinition(compound).IsEnvironmental)
            {
                tweakedBiomeConditions.Ambient = Math.Clamp(change, 0, 1);
            }
            else
            {
                GD.PrintErr("This effect doesn't handle adding new non-environmental compounds");
            }
        }

        patch.Biome.ModifyLongTermCondition(compound, tweakedBiomeConditions);
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
