using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner
{
    private readonly PackedScene microbeScene;
    private readonly CompoundCloudSystem cloudSystem;
    private readonly Random random = new Random();

    public MicrobeSpawner(CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        this.cloudSystem = cloudSystem;
        microbeScene = LoadMicrobeScene();
        CurrentGame = currentGame;
    }

    public GameProperties CurrentGame { get; set; }

    public static Microbe Spawn(Species species, Vector3 location,
        Node worldRoot, PackedScene microbeScene, bool aiControlled,
        CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        var microbe = (Microbe)microbeScene.Instance();

        // The second parameter is (isPlayer), and we assume that if the
        // cell is not AI controlled it is the player's cell
        microbe.Init(cloudSystem, currentGame, !aiControlled);

        worldRoot.AddChild(microbe);
        microbe.Translation = location;

        microbe.AddToGroup(Constants.AI_TAG_MICROBE);
        microbe.AddToGroup(Constants.PROCESS_GROUP);

        if (aiControlled)
            microbe.AddToGroup(Constants.AI_GROUP);

        microbe.ApplySpecies(species);
        microbe.SetInitialCompounds();
        return microbe;
    }

    public static IEnumerable<Microbe> SpawnBacteriaColony(Species species, Vector3 location,
        Node worldRoot, PackedScene microbeScene, CompoundCloudSystem cloudSystem,
        GameProperties currentGame, Random random)
    {
        var curSpawn = new Vector3(random.Next(1, 8), 0, random.Next(1, 8));

        // Three kinds of colonies are supported, line colonies and clump colonies and Networks
        if (random.Next(0, 5) < 2)
        {
            // Clump
            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_COLONY_SIZE,
                Constants.MAX_BACTERIAL_COLONY_SIZE + 1); i++)
            {
                // Dont spawn them on top of each other because it
                // causes them to bounce around and lag
                yield return Spawn(species, location + curSpawn, worldRoot, microbeScene, true,
                    cloudSystem, currentGame);

                curSpawn = curSpawn + new Vector3(random.Next(-7, 8), 0, random.Next(-7, 8));
            }
        }
        else if (random.Next(0, 31) > 2)
        {
            // Line
            // Allow for many types of line
            // (I combined the lineX and lineZ here because they have the same values)
            var line = random.Next(-5, 6) + random.Next(-5, 6);

            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_LINE_SIZE,
                Constants.MAX_BACTERIAL_LINE_SIZE + 1); i++)
            {
                // Dont spawn them on top of each other because it
                // Causes them to bounce around and lag
                yield return Spawn(species, location + curSpawn, worldRoot, microbeScene, true,
                    cloudSystem, currentGame);

                curSpawn = curSpawn + new Vector3(line + random.Next(-2, 3), 0, line + random.Next(-2, 3));
            }
        }
        else
        {
            // Network
            // Allows for "jungles of cyanobacteria"
            // Network is extremely rare

            // To prevent bacteria being spawned on top of each other
            var vertical = false;

            var colony = new ColonySpawnInfo
            {
                Horizontal = false,
                Random = random,
                Species = species,
                CloudSystem = cloudSystem,
                CurrentGame = currentGame,
                CurSpawn = curSpawn,
                MicrobeScene = microbeScene,
                WorldRoot = worldRoot,
            };

            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_COLONY_SIZE,
                Constants.MAX_BACTERIAL_COLONY_SIZE + 1); i++)
            {
                if (random.Next(0, 5) < 2 && !colony.Horizontal)
                {
                    colony.Horizontal = true;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !vertical)
                {
                    colony.Horizontal = false;
                    vertical = true;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !colony.Horizontal)
                {
                    colony.Horizontal = true;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !vertical)
                {
                    colony.Horizontal = false;
                    vertical = true;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
                else
                {
                    // Diagonal
                    colony.Horizontal = false;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
            }
        }
    }

    public static PackedScene LoadMicrobeScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    public static AgentProjectile SpawnAgent(AgentProperties properties, float amount,
        float lifetime, Vector3 location, Vector3 direction,
        Node worldRoot, PackedScene agentScene, Node emitter)
    {
        var normalizedDirection = direction.Normalized();

        var agent = (AgentProjectile)agentScene.Instance();
        agent.Properties = properties;
        agent.Amount = amount;
        agent.TimeToLiveRemaining = lifetime;
        agent.Emitter = emitter;

        worldRoot.AddChild(agent);
        agent.Translation = location + (direction * 1.5f);

        agent.ApplyCentralImpulse(normalizedDirection *
            Constants.AGENT_EMISSION_IMPULSE_STRENGTH);

        agent.AddToGroup(Constants.TIMED_GROUP);
        return agent;
    }

    public IEnumerable<ISpawned> Spawn(Node worldNode, Vector3 location, MicrobeSpecies species, bool isWanderer)
    {
        // The true here is that this is AI controlled
        ISpawned first = Spawn(species, location, worldNode,
            microbeScene, true, cloudSystem, CurrentGame);
        yield return first;

        if (species.IsBacteria && !isWanderer)
        {
            foreach (Microbe microbe in SpawnBacteriaColony(species, location, worldNode, microbeScene,
                cloudSystem, CurrentGame, random))
            {
                yield return microbe;
            }
        }
    }

    private static IEnumerable<Microbe> MicrobeColonySpawnHelper(ColonySpawnInfo colony, Vector3 location)
    {
        for (int c = 0; c < colony.Random.Next(Constants.MIN_BACTERIAL_LINE_SIZE,
            Constants.MAX_BACTERIAL_LINE_SIZE + 1); c++)
        {
            // Dont spawn them on top of each other because
            // It causes them to bounce around and lag
            // And add a little organicness to the look

            if (colony.Horizontal)
            {
                colony.CurSpawn.x += colony.Random.Next(5, 8);
                colony.CurSpawn.z += colony.Random.Next(-2, 3);
            }
            else
            {
                colony.CurSpawn.z += colony.Random.Next(5, 8);
                colony.CurSpawn.x += colony.Random.Next(-2, 3);
            }

            yield return Spawn(colony.Species, location + colony.CurSpawn, colony.WorldRoot,
                colony.MicrobeScene, true, colony.CloudSystem, colony.CurrentGame);
        }
    }

    private class ColonySpawnInfo
    {
        public Species Species;
        public Node WorldRoot;
        public PackedScene MicrobeScene;
        public Vector3 CurSpawn;
        public bool Horizontal;
        public Random Random;
        public CompoundCloudSystem CloudSystem;
        public GameProperties CurrentGame;
    }
}
