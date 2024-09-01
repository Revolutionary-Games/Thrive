using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Time dependent effects running on a world
/// </summary>
public interface IWorldEffect
{
    /// <summary>
    ///   Called when added to a world. The best time to do dynamic casts
    /// </summary>
    public void OnRegisterToWorld();

    public void OnTimePassed(double elapsed, double totalTimePassed);
}

/// <summary>
///   An effect reducing the glucose amount
/// </summary>
[JSONDynamicTypeAllowed]
public class GlucoseReductionEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    public GlucoseReductionEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        ApplyCompoundsAddition();
        HandlePatchCompoundDiffusion();
    }

    private void ApplyCompoundsAddition()
    {
        var atp = SimulationParameters.Instance.GetCompound("atp");
        var temperature = SimulationParameters.Instance.GetCompound("temperature");

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

                            var rate = bioProcess.GetRateWithConditions(patch.Biome);

                            // Inputs
                            foreach (var input in bioProcess.Inputs)
                            {
                                if (input.Key == temperature)
                                    continue;

                                float add;

                                if (patch.Biome.AverageCompounds.ContainsKey(input.Key))
                                {
                                    if (patch.Biome.AverageCompounds[input.Key].Ambient > 0)
                                    {
                                        add = -species.Value * modifier * input.Value * environmentalModifier
                                            * rate;
                                    }
                                    else
                                    {
                                        add = -species.Value * modifier * input.Value * inputModifier *
                                            patch.Biome.AverageCompounds[input.Key].Density * rate;
                                    }
                                }
                                else
                                {
                                    add = -species.Value * modifier * input.Value * inputModifier;
                                }

                                if (totalAdded.ContainsKey(input.Key))
                                {
                                    totalAdded[input.Key] += add;
                                }
                                else
                                {
                                    totalAdded.Add(input.Key, add);
                                }
                            }

                            // Outputs
                            foreach (var output in bioProcess.Outputs)
                            {
                                if (output.Key.IsAgent || output.Key == atp || output.Key == temperature)
                                    continue;

                                float add;

                                if (output.Key.IsGas)
                                {
                                    add = species.Value * modifier * output.Value * environmentalModifier
                                        * rate;
                                }
                                else
                                {
                                    add = species.Value * modifier * output.Value * outputModifier
                                        * patch.Biome.AverageCompounds[output.Key].Density * rate;
                                }

                                if (totalAdded.ContainsKey(output.Key))
                                {
                                    totalAdded[output.Key] += add;
                                }
                                else
                                {
                                    totalAdded.Add(output.Key, add);
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
                    if (compound.Key.IsEnvironmental)
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

    private void HandlePatchCompoundDiffusion()
    {
        foreach (var patch in targetWorld.Map.Patches)
        {
            foreach (var adjacent in patch.Value.Adjacent)
            {
                foreach (var compound in patch.Value.Biome.Compounds)
                {
                    var newConditions = compound.Value;

                    var fractionDensity =
                        (compound.Value.Density -
                        adjacent.Biome.Compounds[compound.Key].Density) / (patch.Value.Adjacent.Count + 1);
                    var fractionAmbient =
                        (compound.Value.Ambient 
                        adjacent.Biome.Compounds[compound.Key].Ambient) / (patch.Value.Adjacent.Count + 1);

                    newConditions.Density -= fractionDensity;
                    newConditions.Ambient -= fractionAmbient;

                    targetWorld.Map.Patches[patch.Key].Biome.ModifyLongTermCondition(compound.Key, newConditions);
                }
            }
        }
    }
}
