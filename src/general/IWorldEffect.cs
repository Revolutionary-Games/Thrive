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
        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            foreach (var compound in patch.Biome.Compounds.Keys)
            {
                if (compound.InternalName == "glucose")
                {
                    var data = patch.Biome.Compounds[compound];

                    data.Density *= Constants.GLUCOSE_REDUCTION_RATE;
                }
            }
        }
    }
}
