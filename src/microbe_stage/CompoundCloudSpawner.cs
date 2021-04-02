using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Spawns compound clouds of a certain type
/// </summary>
public class CompoundCloudSpawner : Spawner
{
    private readonly CompoundCloudSystem clouds;

    // Number of CloudItems for each compound to put in spawn bag
    private Dictionary<Compound, int> compoundCloudCounts = new Dictionary<Compound, int>();

    // Amount of each compound
    private Dictionary<Compound, float> compoundAmounts = new Dictionary<Compound, float>();

    public CompoundCloudSpawner(CompoundCloudSystem clouds, int spawnRadius)
    {
        this.clouds = clouds ?? throw new ArgumentException("clouds is null");
        SetSpawnRadius(spawnRadius);
    }

    public CompoundCloudSystem GetCloudSystem()
    {
        return clouds;
    }

    public void AddBiomeCompound(Compound compound, int numOfItems, float amount)
    {
        compoundCloudCounts.Add(compound, numOfItems);
        compoundAmounts.Add(compound, amount);
    }

    public void ClearBiomeCompounds()
    {
        compoundCloudCounts.Clear();
        compoundAmounts.Clear();
    }

    // I guess I can't just return the Keys without doing this.
    // This won't be slow, as there are very few keys.
    public Compound[] GetCompounds()
    {
        Compound[] compounds = new Compound[compoundCloudCounts.Keys.Count];
        compoundCloudCounts.Keys.CopyTo(compounds, 0);
        return compounds;
    }

    public int GetCloudItemCount(Compound compound)
    {
        return compoundCloudCounts[compound];
    }

    public float GetCloudAmount(Compound compound)
    {
        return compoundAmounts[compound];
    }

    public void SpawnCloud(Vector3 location, Compound compound, float amount)
    {
        int resolution = Settings.Instance.CloudResolution;

        // This spreads out the cloud spawn a bit
        clouds.AddCloud(compound, amount, location + new Vector3(0 + resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0 - resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 + resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 - resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0));
    }
}
