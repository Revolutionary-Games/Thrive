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

        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            // If there are microbes to be eating up the primordial soup, reduce the milk
            if (patch.SpeciesInPatch.Count > 0)
            {
                if (patch.Biome.Compounds.TryGetValue(glucose, out EnvironmentalCompoundProperties glucoseValue))
                {
                    glucoseValue.Density = Math.Max(glucoseValue.Density * Constants.GLUCOSE_REDUCTION_RATE,
                        Constants.GLUCOSE_MIN);
                    patch.Biome.Compounds[glucose] = glucoseValue;
                }
            }
        }
    }
}
