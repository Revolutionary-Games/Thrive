﻿namespace AutoEvo;

using System;
using Godot;

public class ChunkCompoundPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("CHUNK_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

    private readonly float totalEnergy;
    private readonly ChunkConfiguration chunk;
    private readonly Patch patch;

    private readonly Compound compound;

    public ChunkCompoundPressure(Patch patch, float weight, string chunkType, Compound compound) : base(weight, [])
    {
        if (!patch.Biome.Chunks.TryGetValue(chunkType, out var chunkData))
            throw new ArgumentException("Chunk does not exist in patch");

        chunk = chunkData;
        this.compound = compound;
        this.patch = patch;

        if (chunk.Compounds?.TryGetValue(compound, out var compoundAmount) != true)
            throw new ArgumentException("Chunk does not exist in patch");

        // This computation nerfs big chunks with a large amount,
        // by adding an "accessibility" component to total energy.
        // Since most cells will rely on bigger chunks by exploiting the venting,
        // this technically makes it a less efficient food source than small chunks, despite a larger amount.
        // We thus account for venting also in the total energy from the source,
        // by adding a volume-to-surface radius exponent ratio (e.g. 2/3 for a sphere).
        // This logic doesn't match with the rest of auto-evo (which doesn't account for accessibility).
        // TODO: extend this approach or find another nerf.
        var ventedEnergy = Mathf.Pow(compoundAmount.Amount, Constants.AUTO_EVO_CHUNK_AMOUNT_NERF);
        totalEnergy = ventedEnergy * chunk.Density * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        var score = 1.0f;

        // Speed is not too important to chunk microbes
        // But all else being the same faster is better than slower
        score += cache.GetBaseSpeedForSpecies(species) * 0.1f;

        // Diminishing returns on storage
        score += (Mathf.Pow(species.StorageCapacities.Nominal + 1, 0.8f) - 1) / 0.8f;

        // If the species can't engulf, then they are dependent on only eating the runoff compounds
        if (!species.CanEngulf ||
            cache.GetBaseHexSizeForSpecies(species) < chunk.Size * Constants.ENGULF_SIZE_RATIO_REQ)
        {
            score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        var atpCreated = 0.0f;

        foreach (var organelle in species.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.ContainsKey(compound))
                {
                    if (process.Process.Outputs.TryGetValue(ATP, out var outputAmount))
                    {
                        var processEfficiency = cache.GetProcessMaximumSpeed(process, patch.Biome).Efficiency;

                        atpCreated += outputAmount * processEfficiency;
                    }
                }
            }
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(species, patch.Biome);

        score *= Mathf.Min(atpCreated / energyBalance.TotalConsumption, 1);

        return score;
    }

    public override IFormattable GetDescription()
    {
        return new LocalizedString("CHUNK_FOOD_SOURCE",
            string.IsNullOrEmpty(chunk.Name) ?
                new LocalizedString("NOT_FOUND_CHUNK") :
                new LocalizedString(chunk.Name));
    }

    public override float GetEnergy()
    {
        return totalEnergy;
    }

    public override string ToString()
    {
        var chunkName = string.IsNullOrEmpty(chunk.Name) ?
            new LocalizedString("NOT_FOUND_CHUNK") :
            new LocalizedString(chunk.Name);

        return $"{Name} ({chunkName})";
    }
}