﻿// This file contains all the different microbe stage spawner types
// just so that they are in one place.

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Helpers for making different types of spawners
/// </summary>
public static class Spawners
{
    public static MicrobeSpawner MakeMicrobeSpawner(Species species,
        CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        return new MicrobeSpawner(species, cloudSystem, currentGame);
    }

    public static ChunkSpawner MakeChunkSpawner(ChunkConfiguration chunkType)
    {
        foreach (var mesh in chunkType.Meshes)
        {
            if (mesh.LoadedScene == null)
                throw new ArgumentException("configured chunk spawner has a mesh that has no scene loaded");
        }

        return new ChunkSpawner(chunkType);
    }

    public static CompoundCloudSpawner MakeCompoundSpawner(Compound compound,
        CompoundCloudSystem clouds, float amount)
    {
        return new CompoundCloudSpawner(compound, clouds, amount);
    }
}

/// <summary>
///   Helper functions for spawning various things
/// </summary>
public static class SpawnHelpers
{
    public static Microbe SpawnMicrobe(Species species, Vector3 location,
        Node worldRoot, PackedScene microbeScene, bool aiControlled,
        CompoundCloudSystem cloudSystem, ISpawnSystem spawnSystem, GameProperties currentGame,
        CellType? multicellularCellType = null)
    {
        var microbe = (Microbe)microbeScene.Instance();

        // The second parameter is (isPlayer), and we assume that if the
        // cell is not AI controlled it is the player's cell
        microbe.Init(cloudSystem, spawnSystem, currentGame, !aiControlled);

        worldRoot.AddChild(microbe);
        microbe.Translation = location;

        microbe.AddToGroup(Constants.AI_TAG_MICROBE);
        microbe.AddToGroup(Constants.PROCESS_GROUP);
        microbe.AddToGroup(Constants.RUNNABLE_MICROBE_GROUP);

        if (aiControlled)
            microbe.AddToGroup(Constants.AI_GROUP);

        if (multicellularCellType != null)
        {
            microbe.ApplyMulticellularNonFirstCellSpecies((EarlyMulticellularSpecies)species, multicellularCellType);
        }
        else
        {
            microbe.ApplySpecies(species);
        }

        microbe.SetInitialCompounds();
        return microbe;
    }

    /// <summary>
    ///   Gives a random chance for a multicellular cell colony to spawn partially or fully grown
    /// </summary>
    /// <param name="microbe">The multicellular microbe</param>
    /// <param name="random">Random to use for the randomness</param>
    /// <exception cref="ArgumentException">If the microbe is not multicellular</exception>
    public static void GiveFullyGrownChanceForMulticellular(Microbe microbe, Random random)
    {
        if (!microbe.IsMulticellular)
            throw new ArgumentException("must be multicellular");

        // Chance to spawn fully grown or partially grown
        if (random.NextDouble() < Constants.CHANCE_MULTICELLULAR_SPAWNS_GROWN)
        {
            microbe.BecomeFullyGrownMulticellularColony();
        }
        else if (random.NextDouble() < Constants.CHANCE_MULTICELLULAR_SPAWNS_PARTLY_GROWN)
        {
            while (!microbe.IsFullyGrownMulticellular)
            {
                microbe.AddMulticellularGrowthCell();

                if (random.NextDouble() > Constants.CHANCE_MULTICELLULAR_PARTLY_GROWN_CELL_CHANCE)
                    break;
            }
        }
    }

    // TODO: this is likely a huge cause of lag. Would be nice to be able
    // to spawn these so that only one per tick is spawned.
    public static IEnumerable<Microbe> SpawnBacteriaColony(Species species, Vector3 location,
        Node worldRoot, PackedScene microbeScene, CompoundCloudSystem cloudSystem, ISpawnSystem spawnSystem,
        GameProperties currentGame, Random random)
    {
        var curSpawn = new Vector3(random.Next(1, 8), 0, random.Next(1, 8));

        var clumpSize = random.Next(Constants.MIN_BACTERIAL_COLONY_SIZE,
            Constants.MAX_BACTERIAL_COLONY_SIZE + 1);
        for (int i = 0; i < clumpSize; i++)
        {
            // Dont spawn them on top of each other because it
            // causes them to bounce around and lag
            yield return SpawnMicrobe(species, location + curSpawn, worldRoot, microbeScene, true,
                cloudSystem, spawnSystem, currentGame);

            curSpawn += new Vector3(random.Next(-7, 8), 0, random.Next(-7, 8));
        }
    }

    public static PackedScene LoadMicrobeScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    public static FloatingChunk SpawnChunk(ChunkConfiguration chunkType,
        Vector3 location, Node worldNode, PackedScene chunkScene, Random random)
    {
        var chunk = (FloatingChunk)chunkScene.Instance();

        // Settings need to be applied before adding it to the scene
        var selectedMesh = chunkType.Meshes.Random(random);
        chunk.GraphicsScene = selectedMesh.LoadedScene ??
            throw new Exception("Chunk scene has not been loaded even though it should be loaded here");
        chunk.ConvexPhysicsMesh = selectedMesh.LoadedConvexShape;

        if (chunk.GraphicsScene == null)
            throw new ArgumentException("couldn't find a graphics scene for a chunk");

        // Pass on the chunk data
        chunk.Init(chunkType, selectedMesh.SceneModelPath, selectedMesh.SceneAnimationPath);
        chunk.UsesDespawnTimer = !chunkType.Dissolves;

        worldNode.AddChild(chunk);

        // Chunk is spawned with random rotation (in the 2D plane if it's an Easter egg)
        var rotationAxis = chunk.EasterEgg ? new Vector3(0, 1, 0) : new Vector3(0, 1, 1);
        chunk.Transform = new Transform(new Quat(
            rotationAxis.Normalized(), 2 * Mathf.Pi * (float)random.NextDouble()), location);

        chunk.GetNode<Spatial>("NodeToScale").Scale = new Vector3(chunkType.ChunkScale, chunkType.ChunkScale,
            chunkType.ChunkScale);

        chunk.AddToGroup(Constants.FLUID_EFFECT_GROUP);
        chunk.AddToGroup(Constants.AI_TAG_CHUNK);
        return chunk;
    }

    public static PackedScene LoadChunkScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/FloatingChunk.tscn");
    }

    public static void SpawnCloud(CompoundCloudSystem clouds, Vector3 location,
        Compound compound, float amount, Random random)
    {
        int resolution = Settings.Instance.CloudResolution;

        // Randomise amount of compound in the cloud a bit
        amount *= random.Next(0.5f, 1);

        // This spreads out the cloud spawn a bit
        clouds.AddCloud(compound, amount, location + new Vector3(0 + resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0 - resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 + resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 - resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0));
    }

    /// <summary>
    ///   Spawns an agent projectile
    /// </summary>
    public static AgentProjectile SpawnAgent(AgentProperties properties, float amount,
        float lifetime, Vector3 location, Vector3 direction,
        Node worldRoot, PackedScene agentScene, IEntity emitter)
    {
        var normalizedDirection = direction.Normalized();

        var agent = (AgentProjectile)agentScene.Instance();
        agent.Properties = properties;
        agent.Amount = amount;
        agent.TimeToLiveRemaining = lifetime;
        agent.Emitter = new EntityReference<IEntity>(emitter);

        worldRoot.AddChild(agent);
        agent.Translation = location + (direction * 1.5f);
        var scaleValue = amount / Constants.MAXIMUM_AGENT_EMISSION_AMOUNT;
        agent.Scale = new Vector3(scaleValue, scaleValue, scaleValue);

        agent.ApplyCentralImpulse(normalizedDirection *
            Constants.AGENT_EMISSION_IMPULSE_STRENGTH);

        agent.AddToGroup(Constants.TIMED_GROUP);
        return agent;
    }

    public static PackedScene LoadAgentScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/AgentProjectile.tscn");
    }

    public static MulticellularCreature SpawnCreature(Species species, Vector3 location,
        Node worldRoot, PackedScene multicellularScene, bool aiControlled, ISpawnSystem spawnSystem,
        GameProperties currentGame)
    {
        var creature = (MulticellularCreature)multicellularScene.Instance();

        // The second parameter is (isPlayer), and we assume that if the
        // cell is not AI controlled it is the player's cell
        creature.Init(spawnSystem, currentGame, !aiControlled);

        worldRoot.AddChild(creature);
        creature.Translation = location;

        creature.AddToGroup(Constants.ENTITY_TAG_CREATURE);
        creature.AddToGroup(Constants.PROCESS_GROUP);

        if (aiControlled)
            creature.AddToGroup(Constants.AI_GROUP);

        creature.ApplySpecies(species);
        creature.ApplyMovementModeFromSpecies();

        creature.SetInitialCompounds();
        return creature;
    }

    public static PackedScene LoadMulticellularScene()
    {
        return GD.Load<PackedScene>("res://src/late_multicellular_stage/MulticellularCreature.tscn");
    }
}

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner : Spawner
{
    private readonly PackedScene microbeScene;
    private readonly CompoundCloudSystem cloudSystem;
    private readonly GameProperties currentGame;
    private readonly Random random = new();

    public MicrobeSpawner(Species species, CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        Species = species ?? throw new ArgumentException("species is null");

        microbeScene = SpawnHelpers.LoadMicrobeScene();
        this.cloudSystem = cloudSystem;
        this.currentGame = currentGame;
    }

    public override bool SpawnsEntities => true;

    public Species Species { get; }

    public override IEnumerable<ISpawned>? Spawn(Node worldNode, Vector3 location, ISpawnSystem spawnSystem)
    {
        // This should no longer happen, but let's keep this print here to keep track of the situation
        if (Species.Obsolete)
            GD.PrintErr("Obsolete species microbe has spawned");

        // The true here is that this is AI controlled
        var first = SpawnHelpers.SpawnMicrobe(Species, location, worldNode, microbeScene, true, cloudSystem,
            spawnSystem, currentGame);

        if (first.IsMulticellular)
        {
            SpawnHelpers.GiveFullyGrownChanceForMulticellular(first, random);
        }

        yield return first;

        ModLoader.ModInterface.TriggerOnMicrobeSpawned(first);

        // Just in case the is bacteria flag is not correct in a multicellular cell type, here's an extra safety check
        if (first.CellTypeProperties.IsBacteria && !first.IsMulticellular)
        {
            foreach (var colonyMember in SpawnHelpers.SpawnBacteriaColony(Species, location, worldNode,
                         microbeScene, cloudSystem, spawnSystem, currentGame, random))
            {
                yield return colonyMember;

                ModLoader.ModInterface.TriggerOnMicrobeSpawned(colonyMember);
            }
        }
    }

    public override string ToString()
    {
        return $"MicrobeSpawner for {Species}";
    }
}

/// <summary>
///   Spawns compound clouds of a certain type
/// </summary>
public class CompoundCloudSpawner : Spawner
{
    private readonly Compound compound;
    private readonly CompoundCloudSystem clouds;
    private readonly float amount;
    private readonly Random random = new();

    public CompoundCloudSpawner(Compound compound, CompoundCloudSystem clouds, float amount)
    {
        this.compound = compound ?? throw new ArgumentException("compound is null");
        this.clouds = clouds ?? throw new ArgumentException("clouds is null");
        this.amount = amount;
    }

    public override bool SpawnsEntities => false;

    public override IEnumerable<ISpawned>? Spawn(Node worldNode, Vector3 location, ISpawnSystem spawnSystem)
    {
        SpawnHelpers.SpawnCloud(clouds, location, compound, amount, random);

        // We don't spawn entities
        return null;
    }

    public override string ToString()
    {
        return $"CloudSpawner for {compound}";
    }
}

/// <summary>
///   Spawns chunks of a specific type
/// </summary>
public class ChunkSpawner : Spawner
{
    private readonly PackedScene chunkScene;
    private readonly ChunkConfiguration chunkType;
    private readonly Random random = new();

    public ChunkSpawner(ChunkConfiguration chunkType)
    {
        this.chunkType = chunkType;
        chunkScene = SpawnHelpers.LoadChunkScene();
    }

    public override bool SpawnsEntities => true;

    public override IEnumerable<ISpawned>? Spawn(Node worldNode, Vector3 location, ISpawnSystem spawnSystem)
    {
        var chunk = SpawnHelpers.SpawnChunk(chunkType, location, worldNode, chunkScene,
            random);

        yield return chunk;

        ModLoader.ModInterface.TriggerOnChunkSpawned(chunk, true);
    }

    public override string ToString()
    {
        return $"ChunkSpawner for {chunkType.Name}";
    }
}
