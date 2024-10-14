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
    // Tweak variable for how fast compounds diffuse between patches
    private const float BaseMoveAmount = 1;
    private const float BaseDistance = 5;

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

    private static (float Ambient, float Density) CalculateWantedMoveAmounts(Patch sourcePatch, Patch adjacent,
        KeyValuePair<Compound, BiomeCompoundProperties> compound)
    {
        // Apply patch distance to diminish how much to move (to make ocean bottoms receive less surface
        // resources like oxygen)
        // TODO: improve the formula here as sqrt isn't the best
        float moveModifier = BaseMoveAmount /
            MathF.Sqrt(BaseDistance + Math.Abs(sourcePatch.Depth[0] - adjacent.Depth[0]));

        adjacent.Biome.TryGetCompound(compound.Key, CompoundAmountType.Biome,
            out var destinationAmount);

        // Calculate compound amounts to move
        float ambient = (compound.Value.Ambient - destinationAmount.Ambient) * moveModifier;

        float density = (compound.Value.Density - destinationAmount.Density) * moveModifier;
        return (ambient, density);
    }

    private void HandlePatchCompoundDiffusion()
    {
        var simulationParameters = SimulationParameters.Instance;

        var movedAmounts = new Dictionary<Patch, Dictionary<Compound, BiomeCompoundProperties>>();

        // Calculate compound amounts that are moving. This loop checks patch by patch how many compounds that patch
        // wants to send to its neighbours.
        foreach (var patch in targetWorld.Map.Patches)
        {
            foreach (var compound in patch.Value.Biome.Compounds)
            {
                var definition = simulationParameters.GetCompoundDefinition(compound.Key);

                // If not diffusible, then skip
                if (!definition.Diffusible)
                    continue;

                // Calculate how many compounds would like to move
                int targetPatches = 0;

                foreach (var adjacent in patch.Value.Adjacent)
                {
                    var (ambient, density) = CalculateWantedMoveAmounts(patch.Value, adjacent, compound);

                    // If there's nothing really to move, then skip (or if negative as those moves are added by the
                    // other patch)
                    if (ambient < MathUtils.EPSILON && density < MathUtils.EPSILON)
                        continue;

                    ++targetPatches;
                }

                if (targetPatches < 1)
                    continue;

                // Then queue the move amounts
                BiomeCompoundProperties changes = default;

                // In case a new cloud type is created, copy the cloud spawn size
                changes.Amount = compound.Value.Amount;

                foreach (var adjacent in patch.Value.Adjacent)
                {
                    var (ambient, density) = CalculateWantedMoveAmounts(patch.Value, adjacent, compound);
                    if (ambient < MathUtils.EPSILON && density < MathUtils.EPSILON)
                        continue;

                    // Scale the move amount to give equal proportional move to each adjacent patch
                    if (ambient > 0)
                    {
                        changes.Ambient = ambient * (1.0f / targetPatches);
                    }
                    else
                    {
                        // Avoid divisions by zero
                        changes.Ambient = 0;
                    }

                    if (density > 0)
                    {
                        changes.Density = density * (1.0f / targetPatches);
                    }
                    else
                    {
                        changes.Density = 0;
                    }

                    AddMove(compound.Key, adjacent, changes, movedAmounts);

                    // Negate for the source patch to keep the same total amount of compounds but just to move it
                    if (changes.Ambient != 0)
                        changes.Ambient = -changes.Ambient;

                    if (changes.Density != 0)
                        changes.Density = -changes.Density;

                    AddMove(compound.Key, patch.Value, changes, movedAmounts);
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

            // Copy cloud spawn size in case a new thing of such type is created
            existingCompoundProperties.Amount = amount.Amount;

            patchData[compound] = existingCompoundProperties;
        }
    }
}
