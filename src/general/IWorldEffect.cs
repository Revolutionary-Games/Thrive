using Newtonsoft.Json;
using System;

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
        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            // If there are microbes to be eating up the primordial soup, reduce the milk
            if (patch.SpeciesInPatch.Keys.Count > 0)
            {
                Compound glucose = null;

                foreach (var compound in patch.Biome.Compounds.Keys)
                {
                    if (compound.InternalName == "glucose")
                    {
                        glucose = compound;
                    }
                }

                if (glucose != null)
                {
                    var toMod = patch.Biome.Compounds[glucose];
                    toMod.Density = Math.Max(toMod.Density * Constants.GLUCOSE_REDUCTION_RATE, Constants.GLUCOSE_MIN);
                    patch.Biome.Compounds[glucose] = toMod;
                }
            }
        }
    }
}
