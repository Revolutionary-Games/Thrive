using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

/// <summary>
///   Underwater vents only event producing hydrogen sulfide and carbon dioxide
/// </summary>
[JSONDynamicTypeAllowed]
public class UnderwaterVentEruptionEffect : IWorldEffect
{
    [JsonProperty]
    private readonly XoShiRo256starstar random;

    [JsonProperty]
    private GameWorld targetWorld;

    public UnderwaterVentEruptionEffect(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    [JsonConstructor]
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
        var changes = new Dictionary<Compound, float>();
        var cloudSizes = new Dictionary<Compound, float>();

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (patch.BiomeType != BiomeType.Vents)
                continue;

            if (random.Next(100) > Constants.VENT_ERUPTION_CHANCE)
                continue;

            var hasHydrogenSulfide = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Hydrogensulfide,
                out var currentHydrogenSulfide);
            var hasCarbonDioxide = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Carbondioxide,
                out var currentCarbonDioxide);

            // TODO: shouldn't the eruption work even with the compounds not present initially?
            if (!hasHydrogenSulfide || !hasCarbonDioxide)
                continue;

            currentHydrogenSulfide.Density += Constants.VENT_ERUPTION_HYDROGEN_SULFIDE_INCREASE;
            currentCarbonDioxide.Ambient += Constants.VENT_ERUPTION_CARBON_DIOXIDE_INCREASE;

            // Percentage is density times amount, so clamp to the inversed amount (times 100)
            currentHydrogenSulfide.Density = Math.Clamp(currentHydrogenSulfide.Density, 0, 1
                / currentHydrogenSulfide.Amount * 100);
            currentCarbonDioxide.Ambient = Math.Clamp(currentCarbonDioxide.Ambient, 0, 1);

            // Intelligently apply the changes taking total gas percentages into account
            changes[Compound.Hydrogensulfide] = currentHydrogenSulfide.Density;
            changes[Compound.Carbondioxide] = currentCarbonDioxide.Ambient;
            cloudSizes[Compound.Hydrogensulfide] = currentHydrogenSulfide.Amount;

            patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changes, cloudSizes);

            // Patch specific log
            // TODO: should these events be highlighted always? It'll get busy when there are a lot of events.
            patch.LogEvent(new LocalizedString("UNDERWATER_VENT_ERUPTION"),
                true, true, "EruptionEvent.svg");

            if (patch.Visibility == MapElementVisibility.Shown)
            {
                // Global log, but only if patch is known to the player
                targetWorld.LogEvent(new LocalizedString("UNDERWATER_VENT_ERUPTION_IN", patch.Name),
                    true, true, "EruptionEvent.svg");
            }

            patch.AddPatchEventRecord(WorldEffectVisuals.UnderwaterVentEruptionEvent, totalTimePassed);
        }
    }
}
