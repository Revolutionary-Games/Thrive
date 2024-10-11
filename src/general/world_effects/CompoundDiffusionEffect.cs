using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   An effect diffusing specially marked compounds between patches (and also takes ocean depth into account) in
///   contrast to <see cref="AllCompoundDiffusionEffect"/>
/// </summary>
[JSONDynamicTypeAllowed]
public class CompoundDiffusionEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    public CompoundDiffusionEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        HandlePatchCompoundDiffusion();
    }

    private void HandlePatchCompoundDiffusion()
    {
        // Tweak variable for how fast compounds diffuse between patches
        const float baseMoveAmount = 1;
        const float baseDistance = 5;

        var simulationParameters = SimulationParameters.Instance;

        var movedAmounts = new Dictionary<Patch, Dictionary<Compound, BiomeCompoundProperties>>();

        // Calculate compound amounts that are moving
        foreach (var patch in targetWorld.Map.Patches)
        {
            // Each patch receives (potentially) some compound from its adjacent ones
            foreach (var adjacent in patch.Value.Adjacent)
            {
                // Apply patch distance to diminish how much to move (to make ocean bottoms receive less surface
                // resources like oxygen)
                float moveModifier = baseMoveAmount /
                    MathF.Sqrt(baseDistance + Math.Abs(patch.Value.Depth[0] - adjacent.Depth[0]));

                // Share the move "bandwidth" equally with all nearby patches to not drain resources below zero as a
                // total result
                moveModifier /= adjacent.Adjacent.Count;

                foreach (var compound in adjacent.Biome.Compounds)
                {
                    var definition = simulationParameters.GetCompoundDefinition(compound.Key);

                    // If not diffusible or there's nothing to move, then skip
                    if (!definition.Diffusible || (compound.Value.Ambient <= 0 && compound.Value.Density <= 0))
                        continue;

                    BiomeCompoundProperties changes = default;

                    patch.Value.Biome.TryGetCompound(compound.Key, CompoundAmountType.Biome, out var patchCompound);

                    // Move speed depends on the relative amount of the compounds (and only high concentrations move
                    // to lower ones)
                    // And the above multiplier
                    if (compound.Value.Ambient > patchCompound.Ambient)
                    {
                        changes.Ambient = (compound.Value.Ambient - patchCompound.Ambient) / compound.Value.Ambient *
                            moveModifier;
                    }

                    if (compound.Value.Density > patchCompound.Density)
                    {
                        changes.Density = (compound.Value.Density - patchCompound.Density) / compound.Value.Density *
                            moveModifier;
                    }

                    // If there's nothing really to move, then skip
                    if (changes.Ambient < MathUtils.EPSILON && changes.Density < MathUtils.EPSILON)
                        continue;

                    AddMove(compound.Key, patch.Value, changes, movedAmounts);

                    // Negate for the source patch to keep the same total amount of compounds but just to move it
                    if (changes.Ambient != 0)
                        changes.Ambient = -changes.Ambient;

                    if (changes.Density != 0)
                        changes.Density = -changes.Density;

                    AddMove(compound.Key, adjacent, changes, movedAmounts);
                }
            }
        }

        // Apply all results at once
        foreach (var patch in targetWorld.Map.Patches)
        {
            if (!movedAmounts.TryGetValue(patch.Value, out var moved))
                continue;

            foreach (var entry in moved)
            {
                // TODO: switch gas compound handling to work in absolute values and then convert back to percentages?

                if (patch.Value.Biome.TryGetCompound(entry.Key, CompoundAmountType.Biome, out var existing))
                {
                    existing.Density += entry.Value.Density;
                    existing.Ambient += entry.Value.Ambient;

                    if (simulationParameters.GetCompoundDefinition(entry.Key).IsGas)
                        existing.Clamp(0, 1);

                    patch.Value.Biome.ModifyLongTermCondition(entry.Key, existing);
                }
                else
                {
                    patch.Value.Biome.ModifyLongTermCondition(entry.Key, entry.Value);
                }
            }
        }
    }

    private void AddMove(Compound compound, Patch patch, BiomeCompoundProperties amount,
        Dictionary<Patch, Dictionary<Compound, BiomeCompoundProperties>> result)
    {
        if (!result.TryGetValue(patch, out var patchData))
        {
            patchData = new Dictionary<Compound, BiomeCompoundProperties>();
            result.Add(patch, patchData);
        }

        if (!patchData.TryGetValue(compound, out var existingCompoundProperties))
        {
            patchData.Add(compound, amount);
        }
        else
        {
            existingCompoundProperties.Ambient += amount.Ambient;
            existingCompoundProperties.Density += amount.Density;

            patchData[compound] = existingCompoundProperties;
        }
    }
}
