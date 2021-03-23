using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Spawns compound clouds of a certain type
/// </summary>
public class CloudSpawner : Spawner
{
    private readonly CompoundCloudSystem clouds;

    public CloudSpawner(CompoundCloudSystem clouds)
    {
        this.clouds = clouds ?? throw new ArgumentException("clouds is null");
    }

    public CompoundCloudSystem GetCloudSystem()
    {
        return clouds;
    }

    public void SpawnCloud(Vector3 location, Compound compound, float amount)
    {
         int resolution = Settings.Instance.CloudResolution;

        GD.Print("Spawning a Cloud");

        // This spreads out the cloud spawn a bit
        clouds.AddCloud(compound, amount, location + new Vector3(0 + resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0 - resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 + resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 - resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0));
    }

    public override IEnumerable<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        return null;
    }
}
