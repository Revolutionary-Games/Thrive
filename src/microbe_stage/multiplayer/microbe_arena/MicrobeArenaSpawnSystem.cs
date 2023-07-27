using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class MicrobeArenaSpawnSystem : SpawnSystem
{
    public const int MAX_SPAWN_SPOTS = 30;
    public const int MAX_SPAWNS_IN_ONE_SPAWN_SPOT = 20;
    public const float SPAWN_SPOT_MIN_LIFETIME = 15.0f;
    public const float SPAWN_SPOT_MAX_LIFETIME = 35.0f;
    public const float DEFAULT_SPAWN_SPOT_SIZE = 200.0f;
    public const float SPAWN_RADIUS_MARGIN_MULTIPLIER = 0.8f;

    private MultiplayerGameWorld gameWorld;
    private CompoundCloudSystem clouds;

    private List<SpawnSpot> spawnSpots = new();

    private float spawnAreaRadius;

    public MicrobeArenaSpawnSystem(Node root, MultiplayerGameWorld gameWorld, CompoundCloudSystem clouds,
        float radius) : base(root)
    {
        this.gameWorld = gameWorld;
        this.clouds = clouds;
        spawnAreaRadius = radius;
    }

    public Action<List<Vector2>>? OnSpawnCoordinatesChanged { get; set; }

    public override void Init()
    {
        base.Init();

        GenerateSpawnSpots();

        if (gameWorld.Map.CurrentPatch == null)
            throw new InvalidOperationException("Current patch not set");

        var biome = gameWorld.Map.CurrentPatch.Biome;

        // Register chunk spawners
        foreach (var entry in biome.Chunks)
        {
            // Don't spawn Easter eggs if the player has chosen not to
            if (entry.Value.EasterEgg && !gameWorld.WorldSettings.EasterEggs)
                continue;

            // Difficulty only scales the spawn rate for chunks containing compounds
            var density = entry.Value.Density * Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR;

            AddSpawnType(Spawners.MakeChunkSpawner(entry.Value), density, 0);
        }

        // Register cloud spawners
        foreach (var entry in biome.Compounds)
        {
            // Density value in difficulty settings scales overall compound amount quadratically
            var density = entry.Value.Density * Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR;
            var amount = entry.Value.Amount * Constants.MICROBE_ARENA_CLOUD_SPAWN_AMOUNT_SCALE_FACTOR;

            AddSpawnType(Spawners.MakeCompoundBlobSpawner(entry.Key, clouds, amount, 70), density, 0);
        }
    }

    public override void Process(float delta, Vector3 playerPosition)
    {
        elapsed += delta;

        // Remove the y-position from player position
        playerPosition.y = 0;

        float spawnsLeftThisFrame = Constants.MAX_SPAWNS_PER_FRAME;

        // If we have queued spawns to do spawn those
        HandleQueuedSpawns(ref spawnsLeftThisFrame, playerPosition);

        if (spawnsLeftThisFrame <= 0)
            return;

        // This is now an if to make sure that the spawn system is
        // only ran once per frame to avoid spawning a bunch of stuff
        // all at once after a lag spike
        // NOTE: that as QueueFree is used it's not safe to just switch this to a loop
        if (elapsed >= interval)
        {
            elapsed -= interval;

            GenerateSpawnSpots();
            UpdateSpawnSpots(delta, ref spawnsLeftThisFrame);
        }
    }

    private void UpdateSpawnSpots(float delta, ref float spawnsLeftThisFrame)
    {
        foreach (var spot in spawnSpots)
        {
            spot.TimeUntilRemoval -= delta;
            SpawnInSpot(spot, ref spawnsLeftThisFrame);
        }

        spawnSpots.RemoveAll(s => s.TimeUntilRemoval <= 0);
    }

    private void SpawnInSpot(SpawnSpot spot, ref float spawnsLeftThisFrame)
    {
        float spawns = 0.0f;

        foreach (var spawnType in spawnTypes)
        {
            if (SpawnsBlocked(spawnType, spot))
                continue;

            var center = new Vector3(spot.Coordinate.x, 0, spot.Coordinate.y);

            // Distance from the spawn point center.
            var displacement = new Vector3(
                random.NextFloat() * DEFAULT_SPAWN_SPOT_SIZE - (DEFAULT_SPAWN_SPOT_SIZE * 0.5f),
                0,
                random.NextFloat() * DEFAULT_SPAWN_SPOT_SIZE - (DEFAULT_SPAWN_SPOT_SIZE * 0.5f));

            spawns += SpawnWithSpawner(spawnType, center + displacement, ref spawnsLeftThisFrame);

            if (spawns <= 0)
                spawns = 1.0f;

            spot.Spawns += spawns;
        }

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
            debugOverlay.ReportSpawns(spawns);
    }

    private bool SpawnsBlocked(Spawner spawnType, SpawnSpot point)
    {
        return point.Spawns >= MAX_SPAWNS_IN_ONE_SPAWN_SPOT || SpawnsBlocked(spawnType);
    }

    private void GenerateSpawnSpots()
    {
        var generated = false;

        for (int i = 0; i < MAX_SPAWN_SPOTS; ++i)
        {
            if (spawnSpots.Count >= MAX_SPAWN_SPOTS)
                break;

            var r = spawnAreaRadius * SPAWN_RADIUS_MARGIN_MULTIPLIER * Mathf.Sqrt(random.NextFloat());
            var angle = random.NextFloat() * 2 * Mathf.Pi;

            var point = new SpawnSpot
            {
                Coordinate = new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle)),
                TimeUntilRemoval = random.Next(SPAWN_SPOT_MIN_LIFETIME, SPAWN_SPOT_MAX_LIFETIME),
            };

            spawnSpots.Add(point);

            generated = true;
        }

        if (generated)
        {
            OnSpawnCoordinatesChanged?.Invoke(spawnSpots.Select(s => s.Coordinate).ToList());
        }
    }

    private class SpawnSpot
    {
        public Vector2 Coordinate { get; set; }

        public float Spawns { get; set; }

        public float TimeUntilRemoval { get; set; }
    }
}
