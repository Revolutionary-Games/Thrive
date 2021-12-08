﻿using System;
using AutoEvo;

public class MarineSnowFoodSource : FoodSource
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private readonly Patch patch;
    private readonly float totalEnergy;
    private readonly float chunkSize;

    public MarineSnowFoodSource(Patch patch)
    {
        this.patch = patch;

        if (patch.Biome.Chunks.TryGetValue("marineSnow", out ChunkConfiguration chunk))
        {
            chunkSize = chunk.Size;
            totalEnergy = chunk.Compounds[glucose].Amount * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
        }
    }

    public override float FitnessScore(Species species, SimulationCache simulationCache)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var energyBalance = simulationCache.GetEnergyBalanceForSpecies(microbeSpecies, patch);

        // Don't penalize species that can't move at full speed all the time as much here
        var chunkEaterSpeed = Math.Max(microbeSpecies.BaseSpeed + energyBalance.FinalBalance,
            microbeSpecies.BaseSpeed / 3);

        var score = chunkEaterSpeed * species.Behaviour.Activity;

        // If the species can't engulf, then they are dependent on only eating the runoff compounds
        if (microbeSpecies.MembraneType.CellWall ||
            microbeSpecies.BaseHexSize < chunkSize * Constants.ENGULF_SIZE_RATIO_REQ)
        {
            score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        // Marine snow food source penalizes big creatures that try to rely on it
        score /= energyBalance.TotalConsumptionStationary;

        return score;
    }

    public override IFormattable GetDescription()
    {
        return new LocalizedString("MARINE_SNOW_FOOD_SOURCE");
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnergy;
    }
}
