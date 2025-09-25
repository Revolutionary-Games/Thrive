using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   An effect diffusing specially marked compounds between patches (and also takes ocean depth into account). This
///   operates on specific compounds as it causes a bit of a mess and unintended effects if all compounds are always
///   allowed to move.
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

    /// <summary>
    ///   If true this uses a more complex move modifier formula based on the square root distance between patches
    /// </summary>
    public bool UseDistanceMoveModifier { get; set; }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        HandlePatchCompoundDiffusion();
    }

    private void HandlePatchCompoundDiffusion()
    {
        var simulationParameters = SimulationParameters.Instance;

        var movedAmounts = new Dictionary<Patch, Dictionary<Compound, BiomeCompoundProperties>>();

        var cloudSizes = new Dictionary<Compound, float>();
        var changesToApplyAtOnce = new Dictionary<Compound, float>();

        // Calculate compound amounts that are moving. This loop checks patch by patch how many compounds that patch
        // wants to send to its neighbours.
        foreach (var patch in targetWorld.Map.Patches)
        {
            foreach (var compound in patch.Value.Biome.Compounds)
            {
                // Skip processing compounds there isn't any of
                if (compound.Value is { Ambient: <= 0, Density: <= 0 })
                    continue;

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

                    // Negate for the source patch to keep the same total number of compounds but just to move it
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

            changesToApplyAtOnce.Clear();

            foreach (var entry in moved)
            {
                changesToApplyAtOnce[entry.Key] = entry.Value.Ambient + entry.Value.Density;

                if (entry.Value.Ambient != 0 && entry.Value.Density != 0)
                {
                    GD.PrintErr("A compound type shouldn't have both moving density and ambient, this will cause an " +
                        "incorrect result");
                }

                // Set up cloud size copying in case it ends up needed
                if (entry.Value.Amount > 0)
                    cloudSizes[entry.Key] = entry.Value.Amount;
            }

            patch.Value.Biome.ApplyLongTermCompoundChanges(patch.Value.BiomeTemplate, changesToApplyAtOnce, cloudSizes);
        }
    }

    private (float Ambient, float Density) CalculateWantedMoveAmounts(Patch sourcePatch, Patch adjacent,
        KeyValuePair<Compound, BiomeCompoundProperties> compound)
    {
        // Apply patch distance to diminish how much to move (to make ocean bottoms receive less surface
        // resources like oxygen)

        float moveModifier;
        if (UseDistanceMoveModifier)
        {
            // TODO: improve the formula here as sqrt isn't the best
            moveModifier = Constants.COMPOUND_DIFFUSE_BASE_MOVE_AMOUNT /
                MathF.Sqrt(
                    Constants.COMPOUND_DIFFUSE_BASE_DISTANCE + Math.Abs(sourcePatch.Depth[0] - adjacent.Depth[0]));
        }
        else
        {
            // TODO: as this is basically just a constraint on how many patches away something is, should cases where
            // patches are "skipped" in a vertical stack be divided by the number of skipped patches here to have the
            // same end result?
            moveModifier = Constants.COMPOUND_DIFFUSE_BASE_MOVE_AMOUNT_SIMPLE;
        }

        adjacent.Biome.TryGetCompound(compound.Key, CompoundAmountType.Biome,
            out var destinationAmount);

        // Calculate compound amounts to move
        // At most half of the surplus can move as otherwise the source patch may end up with fewer compounds than
        // the destination
        float ambient = (compound.Value.Ambient - destinationAmount.Ambient) * 0.5f;

        float density = (compound.Value.Density - destinationAmount.Density) * 0.5f;
        return (ambient * moveModifier, density * moveModifier);
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
