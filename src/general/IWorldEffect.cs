using System;
using System.Globalization;
using Godot;
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

        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            if (!patch.Biome.Compounds.TryGetValue(glucose, out EnvironmentalCompoundProperties glucoseValue))
                return;

            // If there are microbes to be eating up the primordial soup, reduce the milk
            if (patch.SpeciesInPatch.Count > 0)
            {
                var initialDensity = glucoseValue.Density;

                glucoseValue.Density = Math.Max(glucoseValue.Density * Constants.GLUCOSE_REDUCTION_RATE,
                    Constants.GLUCOSE_MIN);
                patch.Biome.Compounds[glucose] = glucoseValue;

                var initialGlucose = Math.Round(initialDensity * glucoseValue.Amount +
                    patch.GetTotalChunkCompoundAmount(glucose), 3);
                var finalGlucose = Math.Round(glucoseValue.Density * glucoseValue.Amount +
                    patch.GetTotalChunkCompoundAmount(glucose), 3);

                var reductionPercentage = Math.Round((initialGlucose - finalGlucose) / initialGlucose * 100, 1);

                // TODO: global glucose reduction
                patch.LogEvent(new LocalizedString("COMPOUND_CONCENTRATIONS_DECREASED",
                        glucose.Name, string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate(
                            "PERCENTAGE_VALUE"), reductionPercentage)), false,
                    "res://assets/textures/gui/bevel/glucoseDown.png");
            }
        }
    }
}
