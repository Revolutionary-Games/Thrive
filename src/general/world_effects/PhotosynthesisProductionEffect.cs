using System;
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
        var outputModifier = 500.0f;
        var inputModifier = 500.0f;
        var modifier = 0.0000005f;

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

                var organelles = microbeSpecies.Organelles.Organelles;
                int count = organelles.Count;

                for (int i = 0; i < count; ++i)
                {
                    var organelle = organelles[i];

                    foreach (var process in organelle.Definition.RunnableProcesses)
                    {
                        // Only handle relevant processes
                        if (!IsProcessRelevant(process.Process))
                            continue;

                        var rate = ProcessSystem.CalculateProcessMaximumSpeed(process,
                            patch.Biome, CompoundAmountType.Biome, true);

                        // Skip checking processes that cannot run
                        if (rate.CurrentSpeed <= 0)
                            continue;

                        // Inputs take away compounds scaled by the speed and population of the species
                        foreach (var input in process.Process.Inputs)
                        {
                            if (input.Key.ID is Compound.Oxygen)
                            {
                                oxygenBalance -= input.Value * inputModifier * rate.CurrentSpeed * species.Value;
                            }
                            else if (input.Key.ID is Compound.Carbondioxide)
                            {
                                co2Balance -= input.Value * inputModifier * rate.CurrentSpeed * species.Value;
                            }
                        }

                        // Outputs generate the given compounds
                        foreach (var output in process.Process.Outputs)
                        {
                            if (output.Key.ID is Compound.Oxygen)
                            {
                                oxygenBalance += output.Value * outputModifier * rate.CurrentSpeed * species.Value;
                            }
                            else if (output.Key.ID is Compound.Carbondioxide)
                            {
                                co2Balance += output.Value * outputModifier * rate.CurrentSpeed * species.Value;
                            }
                        }
                    }
                }
            }

            // Scale the balances to make the changes less drastic
            oxygenBalance *= modifier;
            co2Balance *= modifier;

            if (oxygenBalance != 0)
                ApplyCompoundChanges(patch, Compound.Oxygen, oxygenBalance);

            if (oxygenBalance != 0)
                ApplyCompoundChanges(patch, Compound.Oxygen, co2Balance);
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

        foreach (var input in process.Outputs)
        {
            if (input.Key.ID is Compound.Oxygen or Compound.Carbondioxide)
                return true;
        }

        return false;
    }
}
