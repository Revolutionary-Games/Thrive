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
            Dictionary<Compound, float> compoundsProduced = new Dictionary<Compound, float>(gasCompounds.Count);

            // Here we consider species production only
            foreach (var species in patch.SpeciesInPatch.Keys)
            {
                // TODO: only adapted for microbe species,
                if (species is MicrobeSpecies microbeSpecies)
                {
                    var individualCompoundProduction = ProcessSystem.ComputeEnvironmentalBalance(
                        microbeSpecies.Organelles, patch.Biome);

                    foreach (var compound in gasCompounds)
                    {
                        if (individualCompoundProduction.TryGetValue(compound, out CompoundBalance compoundProduction))
                        {
                            if (!compoundsProduced.ContainsKey(compound))
                            {
                                compoundsProduced[compound] = 0;
                            }

                            compoundsProduced[compound] += compoundProduction.Balance * patch.SpeciesInPatch[species] *
                                Constants.DISSOLVED_PRODUCTION_FACTOR;
                        }
                    }
                }
            }

            // TODO: Temporary patch-wide constant addition of gaseous compounds for balance.
            var constantCompoundsDissolvedIntake = new Dictionary<Compound, float>(gasCompounds.Count);
            var oxygen = SimulationParameters.Instance.GetCompound("oxygen");
            var carbonDioxide = SimulationParameters.Instance.GetCompound("carbondioxide");
            var nitrogen = SimulationParameters.Instance.GetCompound("nitrogen");

            constantCompoundsDissolvedIntake[oxygen] = Constants.PATCH_CONSTANT_OXYGEN_INPUT;
            constantCompoundsDissolvedIntake[carbonDioxide] = Constants.PATCH_CONSTANT_CARBON_DIOXYDE_INPUT;
            constantCompoundsDissolvedIntake[nitrogen] = Constants.PATCH_CONSTANT_NITROGEN_INPUT;

            foreach (var compound in gasCompounds)
            {
                if (!compoundsProduced.ContainsKey(compound))
                {
                    compoundsProduced[compound] = 0;
                }
            }

            // End of temporary block

            foreach (var compound in compoundsProduced.Keys)
            {
                // TODO: Temporary patch-wide constant addition of gaseous compounds for balance.
                var perPatchConstantIntake = constantCompoundsDissolvedIntake[compound];

                if (patch.Biome.Compounds.TryGetValue(compound, out EnvironmentalCompoundProperties compoundValue))
                {
                    compoundValue.Dissolved = Math.Max(
                        compoundValue.Dissolved + compoundsProduced[compound] / patch.Volume + perPatchConstantIntake,
                        Constants.DISSOLVED_MIN);
                    patch.Biome.Compounds[compound] = compoundValue;
                }
                else
                {
                    patch.Biome.Compounds[compound] = new EnvironmentalCompoundProperties
                    {
                        Amount = 0, Density = 0, Dissolved = compoundsProduced[compound] / patch.Volume,
                    };
                }
            }
        }
    }
}
