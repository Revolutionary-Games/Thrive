using System.Collections.Generic;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

/// <summary>
///   Makes sure nitrogen is between a defined safe limits and attempts to correct things if not (as pure processes
///   don't result in nitrogen balance)
/// </summary>
[JSONDynamicTypeAllowed]
public class NitrogenControlEffect : IWorldEffect
{
    /// <summary>
    ///   This doesn't add any clouds with sizes so this is just a permanently empty dictionary
    /// </summary>
    private readonly Dictionary<Compound, float> cloudSizesDummy = new();

    [JsonProperty]
    private readonly XoShiRo256starstar random = new();

    [JsonProperty]
    private GameWorld targetWorld;

    public NitrogenControlEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        HandleNitrogenLevels();
    }

    private void HandleNitrogenLevels()
    {
        // TODO: having like a world specific configuration for the limits would be pretty nice
        float maxLevel = Constants.MAX_NITROGEN_LEVEL;
        float minLevel = Constants.SOFT_MIN_NITROGEN_LEVEL;

        var nitrogenModification = new Dictionary<Compound, float>();

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            // Add the min level if missing entirely (shouldn't happen unless the biomes JSON file is wrong)
            if (!patch.Biome.TryGetCompound(Compound.Nitrogen, CompoundAmountType.Biome, out var amount))
            {
                nitrogenModification[Compound.Nitrogen] = minLevel;

                patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, nitrogenModification, cloudSizesDummy);
                continue;
            }

            // Adjust nitrogen amount if it is outside the allowed limits
            if (amount.Ambient > maxLevel)
            {
                var excess = amount.Ambient - maxLevel;

                // Lower a bit below the ceiling so that it is not as easy to tell what the ceiling is
                nitrogenModification[Compound.Nitrogen] = -excess - random.NextFloat() * 0.07f;
            }
            else if (amount.Ambient < minLevel)
            {
                var halfAmount = (minLevel - amount.Ambient) * 0.5f;

                // Add a bit of randomness to not look as "clipped" result
                nitrogenModification[Compound.Nitrogen] = halfAmount + halfAmount * random.NextFloat();
            }
            else
            {
                continue;
            }

            patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, nitrogenModification, cloudSizesDummy);
        }
    }
}
