using System;
using Newtonsoft.Json;

/// <summary>
///   An effect reducing the glucose amount (for the microbe stage to make early game easier, and the late game harder)
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
        var glucoseDefinition = SimulationParameters.GetCompound(Compound.Glucose);

        var totalAmount = 0.0f;
        var totalChunkAmount = 0.0f;

        var initialTotalDensity = 0.0f;
        var finalTotalDensity = 0.0f;

        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            if (!patch.Biome.ChangeableCompounds.TryGetValue(Compound.Glucose,
                    out BiomeCompoundProperties glucoseValue))
            {
                return;
            }

            totalAmount += glucoseValue.Amount;
            initialTotalDensity += glucoseValue.Density;
            totalChunkAmount += patch.GetTotalChunkCompoundAmount(Compound.Glucose);

            // If there are microbes to be eating up the primordial soup, reduce the milk
            if (patch.SpeciesInPatch.Count > 0)
            {
                var initialGlucose = Math.Round(glucoseValue.Density * glucoseValue.Amount +
                    patch.GetTotalChunkCompoundAmount(Compound.Glucose), 3);

                glucoseValue.Density = Math.Max(glucoseValue.Density * targetWorld.WorldSettings.GlucoseDecay,
                    Constants.GLUCOSE_MIN);

                patch.Biome.ModifyLongTermCondition(Compound.Glucose, glucoseValue);

                var finalGlucose = Math.Round(glucoseValue.Density * glucoseValue.Amount +
                    patch.GetTotalChunkCompoundAmount(Compound.Glucose), 3);

                var localReduction = Math.Round((initialGlucose - finalGlucose) / initialGlucose * 100, 1);

                // TODO: improve how the glucose reduction is shown for the patch the player is in
                // as right now the reduction percentages aren't super drastic anymore (like under 1%) rather than
                // the 20% it used to say.
                patch.LogEvent(new LocalizedString("COMPOUND_CONCENTRATIONS_DECREASED",
                        glucoseDefinition.Name, new LocalizedString("PERCENTAGE_VALUE", localReduction)), false, false,
                    "glucoseDown.png");
            }

            finalTotalDensity += patch.Biome.ChangeableCompounds[Compound.Glucose].Density;
        }

        var initialTotalGlucose = Math.Round(initialTotalDensity * totalAmount + totalChunkAmount, 3);

        // Prevent a division by zero
        if (initialTotalGlucose == 0)
            return;

        var finalTotalGlucose = Math.Round(finalTotalDensity * totalAmount + totalChunkAmount, 3);
        var globalReduction = Math.Round((initialTotalGlucose - finalTotalGlucose) / initialTotalGlucose * 100, 1);

        if (globalReduction >= 50)
        {
            targetWorld.LogEvent(new LocalizedString("GLUCOSE_CONCENTRATIONS_DRASTICALLY_DROPPED"),
                false, true, "glucoseDown.png");
        }
        else if (globalReduction > 0)
        {
            targetWorld.LogEvent(new LocalizedString("COMPOUND_CONCENTRATIONS_DECREASED",
                    glucoseDefinition.Name, new LocalizedString("PERCENTAGE_VALUE", globalReduction)), false,
                true, "glucoseDown.png");
        }
    }
}
