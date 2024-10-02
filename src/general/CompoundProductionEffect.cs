using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   An effect reducing the glucose amount
/// </summary>
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

        foreach (var patchKeyValue in targetWorld.Map.Patches)
        {
            var totalAdded = new Dictionary<Compound, float>();

            var patch = patchKeyValue.Value;

            foreach (var species in patch.SpeciesInPatch)
            {
                foreach (var reproductionCompound in species.Key.BaseReproductionCost)
                {
                    var add = -species.Value * modifier * reproductionCompound.Value *
                        patch.Biome.AverageCompounds[reproductionCompound.Key].Density;
                    if (totalAdded.ContainsKey(reproductionCompound.Key))
                    {
                        totalAdded[reproductionCompound.Key] += add;
                    }
                    else
                    {
                        totalAdded.Add(reproductionCompound.Key, add);
                    }
                }

                // Microbe species handling
                if (species.Key is MicrobeSpecies microbeSpecies)
                {
                    foreach (var organelle in microbeSpecies.Organelles.Organelles)
                    {
                        if (organelle.Definition.Processes == null)
                            continue;

                        foreach (var process in organelle.Definition.Processes!)
                        {
                            var bioProcess = SimulationParameters.Instance.GetBioProcess(process.Key);

                            var rate = ProcessSystem.CalculateProcessMaximumSpeed(new TweakedProcess(bioProcess),
                                patch.Biome, CompoundAmountType.Biome, true);

                            // Inputs
                            foreach (var input in bioProcess.Inputs)
                            {
                                if (input.Key.ID == Compound.Temperature)
                                    continue;

                                float add;

                                if (patch.Biome.AverageCompounds.ContainsKey(input.Key.ID))
                                {
                                    if (patch.Biome.AverageCompounds[input.Key.ID].Ambient > 0)
                                    {
                                        add = -species.Value * modifier * input.Value * environmentalModifier
                                            * rate.CurrentSpeed;
                                    }
                                    else
                                    {
                                        add = -species.Value * modifier * input.Value * inputModifier *
                                            patch.Biome.AverageCompounds[input.Key.ID].Density * rate.CurrentSpeed;
                                    }
                                }
                                else
                                {
                                    add = -species.Value * modifier * input.Value * inputModifier;
                                }

                                if (totalAdded.ContainsKey(input.Key.ID))
                                {
                                    totalAdded[input.Key.ID] += add;
                                }
                                else
                                {
                                    totalAdded.Add(input.Key.ID, add);
                                }
                            }

                            // Outputs
                            foreach (var output in bioProcess.Outputs)
                            {
                                if (output.Key.IsAgent || output.Key.ID == Compound.ATP
                                    || output.Key.ID == Compound.Temperature)
                                    continue;

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

                                if (totalAdded.ContainsKey(output.Key.ID))
                                {
                                    totalAdded[output.Key.ID] += add;
                                }
                                else
                                {
                                    totalAdded.Add(output.Key.ID, add);
                                }
                            }
                        }
                    }
                }
            }

            // Apply results
            foreach (var compound in patch.Biome.ChangeableCompounds)
            {
                var tweakedBiomeConditions = compound.Value;

                if (totalAdded.ContainsKey(compound.Key))
                {
                    if (SimulationParameters.Instance.GetCompoundDefinition(compound.Key).IsEnvironmental)
                    {
                        tweakedBiomeConditions.Ambient = Math.Clamp(
                            patch.BiomeTemplate.Conditions.ChangeableCompounds[compound.Key].Ambient
                            + totalAdded[compound.Key], 0, 1);
                    }
                    else
                    {
                        tweakedBiomeConditions.Density = Math.Clamp(
                            patch.BiomeTemplate.Conditions.ChangeableCompounds[compound.Key].Density
                            + totalAdded[compound.Key], 0, 1);
                    }
                }

                targetWorld.Map.Patches[patchKeyValue.Key].Biome.ModifyLongTermCondition(compound.Key,
                    tweakedBiomeConditions);
            }
        }
    }
}
