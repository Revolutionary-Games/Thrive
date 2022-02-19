using System;
using System.Collections.Generic;
using System.Linq;
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

        var initialTotalDensity = 0.0f;
        var finalTotalDensity = 0.0f;

        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            if (!patch.Biome.Compounds.TryGetValue(glucose, out EnvironmentalCompoundProperties glucoseValue))
                return;

            totalAmount += glucoseValue.Amount;
            initialTotalDensity += glucoseValue.Density;
            totalChunkAmount += patch.GetTotalChunkCompoundAmount(glucose);

            // If there are microbes to be eating up the primordial soup, reduce the milk
            if (patch.SpeciesInPatch.Count > 0)
            {
                var initialGlucose = Math.Round(glucoseValue.Density * glucoseValue.Amount +
                    patch.GetTotalChunkCompoundAmount(glucose), 3);

                glucoseValue.Density = Math.Max(glucoseValue.Density * Constants.GLUCOSE_REDUCTION_RATE,
                    Constants.GLUCOSE_MIN);
                patch.Biome.Compounds[glucose] = glucoseValue;

                var finalGlucose = Math.Round(glucoseValue.Density * glucoseValue.Amount +
                    patch.GetTotalChunkCompoundAmount(glucose), 3);

                var localReduction = Math.Round((initialGlucose - finalGlucose) / initialGlucose * 100, 1);

                patch.LogEvent(new LocalizedString("COMPOUND_CONCENTRATIONS_DECREASED",
                        glucose.Name, new LocalizedString("PERCENTAGE_VALUE", localReduction)), false,
                    "glucoseDown.png");
            }

            finalTotalDensity += patch.Biome.Compounds[glucose].Density;
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

[JSONDynamicTypeAllowed]
public class GasProductionEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    public GasProductionEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        List<Compound> gasCompounds = SimulationParameters.Instance.GetGasCompounds();

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            // First, add the constant intake to the patch, since it will influence the available amount for species.
            // TODO move to own effect
            AddConstantIntakeToPatch(patch);

            var compoundsProduced = new Dictionary<Compound, float>(gasCompounds.Count);

            // Here we consider species production only
            foreach (var species in patch.SpeciesInPatch.Keys)
            {
                // TODO: this is only adapted for microbe species, and should be expended when other species will arise.
                if (species is MicrobeSpecies microbeSpecies)
                {
                    var individualCompoundProduction = ProcessSystem.ComputeEnvironmentalBalance(
                        microbeSpecies.Organelles, patch.Biome);

                    foreach (var compound in gasCompounds)
                    {
                        if (individualCompoundProduction.TryGetValue(compound, out CompoundBalance compoundProduction))
                        {
                            if (!compoundsProduced.ContainsKey(compound))
                                compoundsProduced[compound] = 0;

                            compoundsProduced[compound] += compoundProduction.Balance * patch.SpeciesInPatch[species] *
                                Constants.DISSOLVED_PRODUCTION_FACTOR;
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("Only microbe species are supported!");
                }
            }

            compoundsProduced = ScaleByOverconsumption(compoundsProduced, patch);

            foreach (var intake in compoundsProduced)
            {
                AddIntakeToPatch(patch, intake);
            }
        }
    }

    private void AddIntakeToPatch(Patch patch, KeyValuePair<Compound, float> intake)
    {
        if (!patch.Biome.Compounds.ContainsKey(intake.Key))
        {
            patch.Biome.Compounds[intake.Key] = new EnvironmentalCompoundProperties
            {
                Amount = 0,
                Density = 0,
                Dissolved = 0,
            };
        }

        var compoundValue = patch.Biome.Compounds[intake.Key];

        // TODO: if capped here, use something to scale production
        compoundValue.Dissolved = Math.Max(
            compoundValue.Dissolved + intake.Value / patch.Volume, Constants.DISSOLVED_MIN);
        patch.Biome.Compounds[intake.Key] = compoundValue;
    }

    private void AddConstantIntakeToPatch(Patch patch)
    {
        var oxygen = SimulationParameters.Instance.GetCompound("oxygen");
        var carbonDioxide = SimulationParameters.Instance.GetCompound("carbondioxide");
        var nitrogen = SimulationParameters.Instance.GetCompound("nitrogen");
        var constantCompoundsDissolvedIntake = new Dictionary<Compound, float>
        {
            { oxygen, Constants.PATCH_CONSTANT_OXYGEN_INPUT },
            { carbonDioxide, Constants.PATCH_CONSTANT_CARBON_DIOXYDE_INPUT },
            { nitrogen, Constants.PATCH_CONSTANT_NITROGEN_INPUT },
        };

        foreach (var intake in constantCompoundsDissolvedIntake)
        {
            AddIntakeToPatch(patch, intake);
        }
    }

    /// <summary>
    ///   Computes the factor of over consumption, i.e. the factor by which the most limiting compound consumption
    ///   should be diminished not to consume more than there is (including compounds that were just produced).
    /// </summary>
    private float ComputeOverConsumptionFactor(Dictionary<Compound, float> compoundsProduced, Patch patch)
    {
        var overConsumptionScalingFactor = 1.0f;
        foreach (var production in compoundsProduced)
        {
            var concentrationInput = production.Value / patch.Volume;
            if (concentrationInput < 0)
            {
                if (patch.Biome.Compounds.TryGetValue(
                        production.Key, out EnvironmentalCompoundProperties compoundValue))
                {
                    if (compoundValue.Dissolved <= Constants.DISSOLVED_MIN)
                        return 0;

                    overConsumptionScalingFactor = Math.Min(
                        -(compoundValue.Dissolved - Constants.DISSOLVED_MIN) / concentrationInput,
                        overConsumptionScalingFactor);
                }
                else
                {
                    return 0;
                }
            }
        }

        return overConsumptionScalingFactor;
    }

    /// <summary>
    ///   Reduce ALL compound production by a scaling factor if one compounds happens to be overproduced.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is a heuristic to prevent production of compounds from non-existing ones.
    ///     A better yet more complicated approach would be to only limit the processes consuming the target compounds.
    ///   </para>
    /// </remarks>
    private Dictionary<Compound, float> ScaleByOverconsumption(Dictionary<Compound, float> compoundsProduced,
        Patch patch)
    {
        var overConsumptionScalingFactor = ComputeOverConsumptionFactor(compoundsProduced, patch);

        if (overConsumptionScalingFactor < 1)
        {
            foreach (var production in compoundsProduced.ToList())
            {
                compoundsProduced[production.Key] = production.Value * overConsumptionScalingFactor;
            }
        }

        return compoundsProduced;
    }
}
