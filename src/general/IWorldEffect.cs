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
    void OnRegisterToWorld();

    void OnTimePassed(double elapsed, double totalTimePassed);
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

        var initialDensity = 0.0f;
        var finalDensity = 0.0f;

        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            if (!patch.Biome.Compounds.TryGetValue(glucose, out EnvironmentalCompoundProperties glucoseValue))
                return;

            totalAmount += glucoseValue.Amount;
            initialDensity += glucoseValue.Density;
            totalChunkAmount += patch.GetTotalChunkCompoundAmount(glucose);

            // If there are microbes to be eating up the primordial soup, reduce the milk
            if (patch.SpeciesInPatch.Count > 0)
            {
                glucoseValue.Density = Math.Max(glucoseValue.Density * Constants.GLUCOSE_REDUCTION_RATE,
                    Constants.GLUCOSE_MIN);
                patch.Biome.Compounds[glucose] = glucoseValue;
            }

            finalDensity += patch.Biome.Compounds[glucose].Density;
        }

        if (finalDensity > 0)
        {
            var initialGlucose = Math.Round(initialDensity * totalAmount + totalChunkAmount, 3);
            var finalGlucose = Math.Round(finalDensity * totalAmount + totalChunkAmount, 3);
            var percentage = Math.Round(((initialGlucose - finalGlucose) / initialGlucose) * 100, 1);

            targetWorld.LogWorldEvent(glucose.Name + " concentrations have decreased by " + percentage + "%",
                "res://assets/textures/gui/bevel/decrease.png");
        }
    }
}
