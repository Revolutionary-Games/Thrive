// This file contains all the different microbe stage spawner types
// just so that they are in one place.

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Helpers for making different types of spawners
/// </summary>
public static class Spawners
{
    public static MicrobeSpawner MakeSpeciesSpawner()
    {
        return new MicrobeSpawner();
    }

    public static CompoundCloudSpawner MakeCompoundSpawner(Compound compound,
        CompoundCloudSystem clouds, float amount)
    {
        return new CompoundCloudSpawner(compound, clouds, amount);
    }
}

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner : ISpawner
{
    private readonly PackedScene microbeScene;

    public MicrobeSpawner()
    {
        // if(species == null)
        //     throw new ArgumentException("species is null");

        microbeScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    public override List<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        var entities = new List<ISpawned>();

        var microbe = (Microbe)microbeScene.Instance();

        worldNode.AddChild(microbe);
        microbe.Translation = location;
        entities.Add(microbe);

        return entities;
    }
}

/// <summary>
///   Spawns compound clouds of a certain type
/// </summary>
public class CompoundCloudSpawner : ISpawner
{
    private readonly Compound compound;
    private readonly CompoundCloudSystem clouds;
    private readonly float amount;

    public CompoundCloudSpawner(Compound compound, CompoundCloudSystem clouds, float amount)
    {
        this.compound = compound ?? throw new ArgumentException("compound is null");
        this.clouds = clouds ?? throw new ArgumentException("clouds is null");
        this.amount = amount;
    }

    public override List<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        clouds.AddCloud(compound, amount, location);

        // We don't spawn entities
        return null;
    }
}
