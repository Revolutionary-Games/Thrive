using System;
using System.Collections.Generic;
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
                var compoundsAddedBySpecies = SpeciesEnvironmentEffect(patch);
                var defaultBiomeConditions = patch.BiomeTemplate.Conditions;

                long divider = 1000000000;

                foreach (var compound in compoundsAddedBySpecies)
                {
                    if (patch.Biome.ChangeableCompounds.ContainsKey(compound.Key))
                    {
                        // Uses default biome conditions to keep species / compounds balance
                        if (!defaultBiomeConditions.ChangeableCompounds.TryGetValue(compound.Key,
                                out BiomeCompoundProperties currentCompoundValue))
                            return;

                        if (!compound.Key.IsGas)
                        {
                            currentCompoundValue.Density += compound.Value / 100 / divider;
                        }
                        else
                        {
                            // TODO: gasseous compound support
                            // currentCompoundValue.Ambient += compound.Value / 100 / divider;
                        }

                        patch.Biome.ChangeableCompounds[compound.Key] = currentCompoundValue;
                    }
                }

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

        // Prevent a division by zero
        if (initialTotalGlucose == 0)
            return;

        var finalTotalGlucose = Math.Round(finalTotalDensity * totalAmount + totalChunkAmount, 3);
        var globalReduction = Math.Round((initialTotalGlucose - finalTotalGlucose) / initialTotalGlucose * 100, 1);

        if (globalReduction >= 50)
        {
            targetWorld.LogEvent(new LocalizedString("GLUCOSE_CONCENTRATIONS_DRASTICALLY_DROPPED"),
                false, "glucoseDown.png");
        }
        else if (globalReduction > 0)
        {
            targetWorld.LogEvent(new LocalizedString("COMPOUND_CONCENTRATIONS_DECREASED",
                    glucose.Name, new LocalizedString("PERCENTAGE_VALUE", globalReduction)), false,
                "glucoseDown.png");
        }
    }

    private Dictionary<Compound, float> SpeciesEnvironmentEffect(Patch patch)
    {
        // Values can be negative (compounds are reduced if so)
        Dictionary<Compound, float> totalCompoundsAdded = new Dictionary<Compound, float>();

        foreach (var speciesPair in patch.SpeciesInPatch)
        {
            var species = speciesPair.Key;
            var population = speciesPair.Value;

            // Microbe species
            if (species is MicrobeSpecies)
            {
                var microbeSpecies = (MicrobeSpecies)species;

                foreach (var organelle in microbeSpecies.Organelles)
                {
                    if (organelle.Definition.Processes == null)
                        continue;

                    foreach (var processPair in organelle.Definition.Processes)
                    {
                        var process = SimulationParameters.Instance.GetBioProcess(processPair.Key);

                        // Inputs
                        foreach (var processCompoundPair in process.Inputs)
                        {
                            var compound = processCompoundPair.Key;

                            var addedValue = processCompoundPair.Value * processPair.Value * population;

                            if (totalCompoundsAdded.ContainsKey(compound))
                                totalCompoundsAdded[compound] -= addedValue;
                            else
                                totalCompoundsAdded.Add(compound, addedValue);
                        }

                        // Outputs
                        foreach (var processCompoundPair in process.Outputs)
                        {
                            var compound = processCompoundPair.Key;

                            var addedValue = processCompoundPair.Value * processPair.Value * population;

                            if (totalCompoundsAdded.ContainsKey(compound))
                                totalCompoundsAdded[compound] += addedValue;
                            else
                                totalCompoundsAdded.Add(compound, addedValue);
                        }
                    }
                }
            }
        }

        return totalCompoundsAdded;
    }
}
