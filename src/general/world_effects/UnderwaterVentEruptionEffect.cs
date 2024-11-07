using System;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

/// <summary>
///   Underwater vents only event producing hydrogen sulfide and carbon dioxide
/// </summary>
[JSONDynamicTypeAllowed]
public class UnderwaterVentEruptionEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    private XoShiRo256starstar random;

    public UnderwaterVentEruptionEffect(GameWorld targetWorld, XoShiRo256starstar random)
    {
        this.targetWorld = targetWorld;
        this.random = random;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (patch.BiomeType == BiomeType.Vents)
            {
                if (random.Next(100) > Constants.VENT_ERUPTION_CHANCE)
                    continue;

                var hasHydrogenSulfide = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Hydrogensulfide,
                    out var currentHydrogenSulfide);
                var hasCarbonDioxide = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Carbondioxide,
                    out var currentCarbonDioxide);

                if (!hasHydrogenSulfide || !hasCarbonDioxide)
                    continue;

                currentHydrogenSulfide.Density += Constants.VENT_ERUPTION_HYDROGEN_SULFIDE_INCREASE;
                currentCarbonDioxide.Ambient += Constants.VENT_ERUPTION_CARBON_DIOXIDE_INCREASE;

                // Percentage is density times amount, so clamp to the inversed amount (times 100)
                currentHydrogenSulfide.Density = Math.Clamp(currentHydrogenSulfide.Density, 0, 1
                    / currentHydrogenSulfide.Amount * 100);
                currentCarbonDioxide.Ambient = Math.Clamp(currentCarbonDioxide.Ambient, 0, 1);

                patch.Biome.ModifyLongTermCondition(Compound.Hydrogensulfide, currentHydrogenSulfide);
                patch.Biome.ModifyLongTermCondition(Compound.Carbondioxide, currentCarbonDioxide);

                patch.LogEvent(new LocalizedString("UNDERWATER_VENT_ERUPTION"),
                    true, true, "PatchVents.svg");

                if (patch.Visibility == MapElementVisibility.Shown)
                {
                    targetWorld.LogEvent(new LocalizedString("ERUPTION_IN", patch.Name),
                        true, true, "PatchVents.svg");
                }

                patch.ApplyPatchNodeVisuals(WorldEffectVisuals.UnderwaterVentEruption);
            }
        }
    }
}
