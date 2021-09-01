using System.Collections.Generic;
using System.Linq;
using Godot;

public class FloatingChunkFoodSource : FoodSource
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private Patch patch;
    private float totalEnergy;
    private float chunkSize;

    public FloatingChunkFoodSource(Patch patch)
    {
        this.patch = patch;

        if (patch.Biome.Chunks.ContainsKey("marineSnow"))
        {
            ChunkConfiguration chunk = patch.Biome.Chunks["marineSnow"];
            chunkSize = chunk.Size;
            totalEnergy = chunk.Compounds[glucose].Amount * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
        }
    }

    public override float FitnessScore(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        var predatorSize = microbeSpecies.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);
        var predatorSpeed = microbeSpecies.BaseSpeed;
        predatorSpeed += ProcessSystem
            .ComputeEnergyBalance(microbeSpecies.Organelles.Organelles, patch.Biome,
                microbeSpecies.MembraneType).FinalBalance;

        var score = predatorSpeed * species.Activity;

        if (predatorSize < chunkSize * Constants.ENGULF_SIZE_RATIO_REQ)
        {
            score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        return score;
    }

    public override float TotalEnergyAvailable()
    {
        return totalEnergy;
    }
}
