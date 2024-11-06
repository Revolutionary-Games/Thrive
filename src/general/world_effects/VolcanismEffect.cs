using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Adds CO2 (and other) volcanism caused compounds to patches (up to a limit)
/// </summary>
[JSONDynamicTypeAllowed]
public class VolcanismEffect : IWorldEffect
{
    private const float VentsCO2Strength = 0.15f;
    private const float VentsCO2Threshold = 0.3f;

    private const float SurfaceCO2Strength = 0.025f;
    private const float SurfaceCO2Threshold = 0.15f;

    private const float FloorCO2Strength = 0.005f;
    private const float FloorCO2Threshold = 0.1f;

    private readonly Dictionary<Compound, float> addedCo2 = new();
    private readonly Dictionary<Compound, float> cloudSizesDummy = new();

    [JsonProperty]
    private GameWorld targetWorld;

    public VolcanismEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        ApplyVolcanism();
    }

    private void ApplyVolcanism()
    {
        foreach (var patchKeyValue in targetWorld.Map.Patches)
        {
            // TODO: make these configured by the biomes.json file
            if (patchKeyValue.Value.BiomeType == BiomeType.Vents)
            {
                // Vents get a bunch of CO2 to then spread into the ocean
                ProduceCO2(patchKeyValue.Value, VentsCO2Strength, VentsCO2Threshold);
            }
            else if (patchKeyValue.Value.BiomeType is BiomeType.Epipelagic or BiomeType.Coastal or BiomeType.Estuary
                     or BiomeType.Tidepool or BiomeType.IceShelf)
            {
                // Ice shelf gets co2 here as it seems to be pretty often the driver for early oxygen in the world

                // Surface patches are given some CO2 from assumed volcanic activity on land
                ProduceCO2(patchKeyValue.Value, SurfaceCO2Strength, SurfaceCO2Threshold);
            }
            else if (patchKeyValue.Value.BiomeType is BiomeType.Seafloor)
            {
                // And to be fair lets give a bit of CO2 also to ocean floor from underwater volcanoes
                ProduceCO2(patchKeyValue.Value, FloorCO2Strength, FloorCO2Threshold);
            }
        }
    }

    private void ProduceCO2(Patch patch, float co2Strength, float threshold)
    {
        if (!patch.Biome.TryGetCompound(Compound.Carbondioxide, CompoundAmountType.Biome, out var amount))
        {
            addedCo2[Compound.Carbondioxide] = co2Strength;

            patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, addedCo2, cloudSizesDummy);
            return;
        }

        // Add to existing if threshold is low enough
        if (amount.Ambient >= threshold)
            return;

        // TODO: should this clamp or not?
        addedCo2[Compound.Carbondioxide] = Math.Clamp(amount.Ambient + co2Strength, 0, threshold);

        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, addedCo2, cloudSizesDummy);
    }
}
