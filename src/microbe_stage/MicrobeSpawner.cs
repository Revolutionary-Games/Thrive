using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner : Spawner
{
    private readonly PackedScene microbeScene;
    private readonly CompoundCloudSystem cloudSystem;
    private readonly Random random;
    private GameProperties currentGame;

    private Dictionary<Species, int> speciesCounts = new Dictionary<Species, int>();

    public MicrobeSpawner(CompoundCloudSystem cloudSystem, int spawnRadius)
    {
        microbeScene = LoadMicrobeScene();
        this.cloudSystem = cloudSystem;
        SetSpawnRadius(spawnRadius);

        random = new Random();
    }

    public static Microbe SpawnMicrobe(Species species, Vector3 location,
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
                yield return SpawnMicrobe(species, location + curSpawn, worldRoot, microbeScene, true,
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
                yield return SpawnMicrobe(species, location + curSpawn, worldRoot, microbeScene, true,
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

    public void SetCurrentGame(GameProperties currentGame)
    {
        this.currentGame = currentGame;
    }

    public void AddSpecies(Species species, int numOfItems)
    {
        speciesCounts.Add(species, numOfItems);
    }

    public void ClearSpecies()
    {
        speciesCounts.Clear();
    }

    public Species[] GetSpecies()
    {
        Species[] species = new Species[speciesCounts.Keys.Count];
        speciesCounts.Keys.CopyTo(species, 0);
        return species;
    }

    public int GetSpeciesCount(Species species)
    {
        return speciesCounts[species];
    }

    public List<ISpawned> Spawn(Node worldNode, Vector3 location, MicrobeSpecies species)
    {
        List<ISpawned> spawnedMicrobes = new List<ISpawned>();

        // The true here is that this is AI controlled
        spawnedMicrobes.Add(SpawnMicrobe(species, location, worldNode,
            microbeScene, true, cloudSystem, currentGame));

        if (species.IsBacteria)
        {
            foreach (Microbe microbe in SpawnBacteriaColony(species, location, worldNode, microbeScene,
                cloudSystem, currentGame, random))
            {
                spawnedMicrobes.Add(microbe);
            }
        }

        return spawnedMicrobes;
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

            yield return SpawnMicrobe(colony.Species, location + colony.CurSpawn, colony.WorldRoot,
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
