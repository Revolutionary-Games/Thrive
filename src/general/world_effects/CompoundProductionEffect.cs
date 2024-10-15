using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Removes and adds compounds to patches based on alive cells binding up resources and also producing compounds
/// </summary>
/// <remarks>
///   <para>
///     This is currently only experimentally enabled as this requires tweaking. And also the GUI saying that the
///     amount of glucose in the editor has been reduced must be disabled when this is properly enabled.
///   </para>
/// </remarks>
[JSONDynamicTypeAllowed]
public class CompoundProductionEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    public CompoundProductionEffect(GameWorld targetWorld)
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
        var environmentalModifier = 100.0f;
        var modifier = 0.0000005f;

        var totalAdded = new Dictionary<Compound, float>();

        foreach (var patchKeyValue in targetWorld.Map.Patches)
        {
            totalAdded.Clear();

            var patch = patchKeyValue.Value;

            foreach (var species in patch.SpeciesInPatch)
            {
                // TODO: this should take the actual reproduction cost into account, not just the base cost which
                // doesn't include the organelle costs
                foreach (var reproductionCompound in species.Key.BaseReproductionCost)
                {
                    var add = -species.Value * modifier * reproductionCompound.Value *
                        patch.Biome.AverageCompounds[reproductionCompound.Key].Density;

                    totalAdded.TryGetValue(reproductionCompound.Key, out var existing);
                    totalAdded[reproductionCompound.Key] = existing + add;
                }

                // Microbe species handling
                if (species.Key is MicrobeSpecies microbeSpecies)
                {
                    var organelles = microbeSpecies.Organelles.Organelles;
                    int count = organelles.Count;

                    for (int i = 0; i < count; ++i)
                    {
                        var organelle = organelles[i];

                        // TODO: processes from different organelles should be all combined using
                        // ProcessSystem.ComputeActiveProcessList
                        // (that also takes special stuff into account, so the below code has a subtle bug)
                        foreach (var process in organelle.Definition.RunnableProcesses)
                        {
                            var rate = ProcessSystem.CalculateProcessMaximumSpeed(process,
                                patch.Biome, CompoundAmountType.Biome, true);

                            // Inputs
                            foreach (var input in process.Process.Inputs)
                            {
                                // TODO: why would this remove sunlight? (and not ignore it here)
                                if (input.Key.ID == Compound.Temperature)
                                    continue;

                                float add;

                                // TODO: is it really the case that this should use the day-averaged compound values?
                                if (patch.Biome.TryGetCompound(input.Key.ID, CompoundAmountType.Average,
                                        out var patchAmount))
                                {
                                    if (patchAmount.Ambient > 0)
                                    {
                                        add = -species.Value * modifier * input.Value * environmentalModifier
                                            * rate.CurrentSpeed;
                                    }
                                    else
                                    {
                                        add = -species.Value * modifier * input.Value * inputModifier *
                                            patchAmount.Density * rate.CurrentSpeed;
                                    }
                                }
                                else
                                {
                                    add = -species.Value * modifier * input.Value * inputModifier;
                                }

                                totalAdded.TryGetValue(input.Key.ID, out var existing);
                                totalAdded[input.Key.ID] = existing + add;
                            }

                            // Outputs
                            foreach (var output in process.Process.Outputs)
                            {
                                if (output.Key.IsAgent || output.Key.ID is Compound.ATP or Compound.Temperature)
                                {
                                    continue;
                                }

                                float add;

                                if (output.Key.IsGas)
                                {
                                    add = species.Value * modifier * output.Value * environmentalModifier
                                        * rate.CurrentSpeed;
                                }
                                else
                                {
                                    add = species.Value * modifier * output.Value * outputModifier
                                        * patch.Biome.AverageCompounds[output.Key.ID].Density * rate.CurrentSpeed;
                                }

                                totalAdded.TryGetValue(output.Key.ID, out var existing);
                                totalAdded[output.Key.ID] = existing + add;
                            }
                        }
                    }
                }
            }

            // Apply results
            // TODO: this should probably take parts from PhotosynthesisProductionEffect
            foreach (var entry in totalAdded)
            {
                if (patch.Biome.TryGetCompound(entry.Key, CompoundAmountType.Biome, out var tweakedBiomeConditions))
                {
                    if (SimulationParameters.Instance.GetCompoundDefinition(entry.Key).IsEnvironmental)
                    {
                        tweakedBiomeConditions.Ambient =
                            Math.Clamp(tweakedBiomeConditions.Ambient + totalAdded[entry.Key], 0, 1);
                    }
                    else
                    {
                        tweakedBiomeConditions.Density =
                            Math.Clamp(tweakedBiomeConditions.Density + totalAdded[entry.Key], 0, 1);
                    }
                }
                else
                {
                    // TODO: it is pretty critical that the amount in each spawned cloud is tweaked to be good
                    var singleCloudAmount = 125000;

                    // New compound produced in this patch
                    if (SimulationParameters.Instance.GetCompoundDefinition(entry.Key).IsEnvironmental)
                    {
                        tweakedBiomeConditions.Ambient = Math.Clamp(totalAdded[entry.Key], 0, 1);
                    }
                    else
                    {
                        tweakedBiomeConditions.Amount = singleCloudAmount;

                        tweakedBiomeConditions.Density = Math.Clamp(totalAdded[entry.Key], 0, 1);
                    }
                }

                patch.Biome.ModifyLongTermCondition(entry.Key, tweakedBiomeConditions);
            }
        }
    }
}
