using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

/// <summary>
/// Changes patches' temperature and sunlight based on active patch events as those values modifications
/// can be more tricky than just a simple addition as it's the case of other compounds
/// </summary>
[JSONDynamicTypeAllowed]
public class PatchEventsManager : IWorldEffect
{
    [JsonProperty]
    private readonly XoShiRo256starstar random;

    [JsonProperty]
    private GameWorld targetWorld;

    public PatchEventsManager(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    [JsonConstructor]
    public PatchEventsManager(GameWorld targetWorld, XoShiRo256starstar random)
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
            var totalTemperatureChange = 0.0f;
            var totalSunlightMultiplication = 1.0f;
            float? fixedTemperatureValue = null;
            foreach (var eventProperties in patch.ActivePatchEvents.Values)
            {
                totalTemperatureChange += eventProperties.TemperatureAmbientChange;
                totalSunlightMultiplication *= eventProperties.SunlightAmbientMultiplier;
                if (eventProperties.TemperatureAmbientFixedValue != null)
                    fixedTemperatureValue = eventProperties.TemperatureAmbientFixedValue;
            }

            bool hasTemperature =
                patch.Biome.ChangeableCompounds.TryGetValue(Compound.Temperature, out var currentTemperature);
            bool hasSunlight = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Sunlight, out var currentSunlight);

            if (!hasTemperature)
            {
                GD.PrintErr("Patch event manager encountered patch with unexpectedly no temperature.");
                return;
            }

            if (!hasSunlight)
            {
                GD.PrintErr("Patch event manager encountered patch with unexpectedly no sunlight.");
                return;
            }

            currentTemperature.Ambient =
                fixedTemperatureValue != null ?
                    fixedTemperatureValue.Value :
                    patch.Biome.StartingTemperatureValue + totalTemperatureChange;
            currentSunlight.Ambient = patch.Biome.StartingSunlightValue * totalSunlightMultiplication;

            patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
            patch.Biome.ModifyLongTermCondition(Compound.Temperature, currentTemperature, true);
        }
    }
}
