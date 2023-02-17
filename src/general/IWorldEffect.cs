using System;
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
        var glucose = SimulationParameters.Instance.GetCompound("glucose");

        var totalAmount = 0.0f;
        var totalChunkAmount = 0.0f;

        var initialTotalDensity = 0.0f;
        var finalTotalDensity = 0.0f;

        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            if (!patch.Biome.ChangeableCompounds.TryGetValue(glucose, out BiomeCompoundProperties glucoseValue))
                return;

            totalAmount += glucoseValue.Amount;
            initialTotalDensity += glucoseValue.Density;
            totalChunkAmount += patch.GetTotalChunkCompoundAmount(glucose);

            // If there are microbes to be eating up the primordial soup, reduce the milk
            if (patch.SpeciesInPatch.Count > 0)
            {
                var initialGlucose = Math.Round(glucoseValue.Density * glucoseValue.Amount +
                    patch.GetTotalChunkCompoundAmount(glucose), 3);

                glucoseValue.Density = Math.Max(glucoseValue.Density * targetWorld.WorldSettings.GlucoseDecay,
                    Constants.GLUCOSE_MIN);
                patch.Biome.ChangeableCompounds[glucose] = glucoseValue;

                var finalGlucose = Math.Round(glucoseValue.Density * glucoseValue.Amount +
                    patch.GetTotalChunkCompoundAmount(glucose), 3);

                var localReduction = Math.Round((initialGlucose - finalGlucose) / initialGlucose * 100, 1);

                patch.LogEvent(new LocalizedString("COMPOUND_CONCENTRATIONS_DECREASED",
                        glucose.Name, new LocalizedString("PERCENTAGE_VALUE", localReduction)), false,
                    "glucoseDown.png");
            }

            finalTotalDensity += patch.Biome.ChangeableCompounds[glucose].Density;
        }

        var initialTotalGlucose = Math.Round(initialTotalDensity * totalAmount + totalChunkAmount, 3);
        var finalTotalGlucose = Math.Round(finalTotalDensity * totalAmount + totalChunkAmount, 3);
        var globalReduction = Math.Round((initialTotalGlucose - finalTotalGlucose) / initialTotalGlucose * 100, 1);

        if (globalReduction >= 50)
        {
            targetWorld.LogEvent(new LocalizedString("GLUCOSE_CONCENTRATIONS_DRASTICALLY_DROPPED"),
                false, "glucoseDown.png");
        }
        else
        {
            targetWorld.LogEvent(new LocalizedString("COMPOUND_CONCENTRATIONS_DECREASED",
                    glucose.Name, new LocalizedString("PERCENTAGE_VALUE", globalReduction)), false,
                "glucoseDown.png");
        }
    }
}
