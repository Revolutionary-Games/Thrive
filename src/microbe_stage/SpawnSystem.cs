using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
{
    /// <summary>
    ///   Root node to parent all spawned things to
    /// </summary>
    private readonly Node worldRoot;

    private readonly List<Spawner> spawnTypes = new();

    [JsonProperty]
    private readonly Random random = new();

    private readonly FastNoiseLite noise;

    /// <summary>
    ///   This is used to spawn only a few entities per frame with minimal changes needed to code that wants to
    ///   spawn a bunch of stuff at once
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This isn't saved but the likelihood that losing out on spawning some things is not super critical.
    ///     Also it is probably the case that this isn't even used on most frames so it is perhaps uncommon
    ///     that there are queued things when saving.
    ///   </para>
    ///   <para>
    ///     TODO: it might be nice to use a struct instead and a field indicating if this is valid to not recreate
    ///     this object so much
    ///   </para>
    /// </remarks>
    private Queue<QueuedSpawn> queuedSpawns = new();

    private Dictionary<Sector, float> sectorDensities = new();
    private List<Sector> loadedSectors = new();

    public SpawnSystem(Node root)
    {
        worldRoot = root;

        noise = new FastNoiseLite(12346234);
        noise.SetFrequency(5f);
        noise.SetDomainWarpType(FastNoiseLite.DomainWarpType.BasicGrid);
    }

    /// <summary>
    ///   Adds an externally spawned entity to be despawned
    /// </summary>
    public static void AddEntityToTrack(SpawnedRigidBody entity)
    {
        entity.EntityNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    public void OnPlayerSectorChanged(Sector newSector)
    {
        // List of sectors that are in the load radius
        var newLoadedSectors = GetSectorsInRadius(newSector.Pos, Constants.SECTOR_LOAD_RADIUS);
        var sectorsToLoad = newLoadedSectors.Except(loadedSectors).ToList();

        // List of sectors that are in the non-unload radius
        var sectorsToKeep = GetSectorsInRadius(newSector.Pos, Constants.SECTOR_UNLOAD_RADIUS);
        var sectorsToUnload = loadedSectors.Except(sectorsToKeep).ToList();

        LoadSectors(sectorsToLoad);
        UnloadSectors(sectorsToUnload);

        loadedSectors = loadedSectors.Concat(sectorsToLoad).Except(sectorsToUnload).ToList();
    }

    public void AddSpawnType(Spawner spawner)
    {
        spawnTypes.Add(spawner);
    }

    public void RemoveSpawnType(Spawner spawner)
    {
        spawnTypes.Remove(spawner);
    }

    public float GetSectorDensity(Sector sector)
    {
        if (sectorDensities.ContainsKey(sector))
            return sectorDensities[sector];

        var density = noise.GetNoise(sector.X, sector.Y);
        density = (density + 1f) / 2f;
        sectorDensities[sector] = density;
        return density;
    }

    /// <summary>
    ///   Saves a image to the disk containing the density values of nearby chunks. Darker spots are richer.
    ///   The player is in the middle of the picture.
    /// </summary>
    /// <param name="size">The size in pixels</param>
    public void GenerateNoiseImage(int size = 31)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format24bppRgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, bitmap.PixelFormat);

        var sizeHalf = size / 2;

        unsafe
        {
            var ptr = (byte*)data.Scan0;
            if (ptr == null)
                return;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var density = GetSectorDensity(new Sector(x - sizeHalf, y - sizeHalf));
                    var grayscaleValue = (byte)((1 - density) * byte.MaxValue);
                    ptr[x * 3 + y * data.Stride] = grayscaleValue;
                    ptr[x * 3 + y * data.Stride + 1] = grayscaleValue;
                    ptr[x * 3 + y * data.Stride + 2] = grayscaleValue;
                }
            }
        }

        bitmap.UnlockBits(data);
        bitmap.Save(ProjectSettings.GlobalizePath(Constants.GENERATE_SPAWN_SYSTEM_NOISE_IMAGE_PATH), ImageFormat.Bmp);
    }

    /// <summary>
    ///   Despawns all spawned entities
    /// </summary>
    public void DespawnAll()
    {
        queuedSpawns = new Queue<QueuedSpawn>();
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);

        foreach (Node entity in spawnedEntities)
        {
            DespawnEntity(entity);
        }
    }

    /// <summary>
    ///   Processes spawning and despawning things
    /// </summary>
    public void Process(float delta)
    {
        _ = delta;

        // If we have queued spawns to do spawn those
        if (queuedSpawns == null)
            return;

        HandleQueuedSpawns(Constants.MAX_SPAWNS_PER_FRAME);
    }

    private IEnumerable<Sector> GetSectorsInRadius(Int2 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                yield return new Sector(center.x + x, center.y + y);
            }
        }
    }

    /// <summary>
    ///   Determines what stuff to spawn in the new sectors and queues this stuff
    /// </summary>
    private void LoadSectors(List<Sector> sectors)
    {
        foreach (var spawner in spawnTypes)
        {
            foreach (var sector in sectors)
            {
                var density = GetSectorDensity(sector);

                var spawnPoints = spawner.GetSpawnPoints(density, random)
                    .Select(p => p + new Vector2(sector.X, sector.Y) * Constants.SECTOR_SIZE);

                foreach (var spawnPoint in spawnPoints)
                {
                    queuedSpawns.Enqueue(new QueuedSpawn(spawner, spawnPoint));
                }
            }
        }
    }

    /// <summary>
    ///   Despawns the stuff in these sectors
    /// </summary>
    private void UnloadSectors(List<Sector> sectors)
    {
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);
        foreach (SpawnedRigidBody entity in spawnedEntities)
        {
            if (sectors.Contains(entity.CurrentSector))
            {
                DespawnEntity(entity);
            }
        }
    }

    private void DespawnEntity(Node entity)
    {
        if (!entity.IsQueuedForDeletion())
        {
            var spawned = entity as SpawnedRigidBody;

            if (spawned == null)
            {
                GD.PrintErr("A node has been put in the spawned group but it isn't derived from SpawnedRigidBody");
            }

            spawned.DestroyDetachAndQueueFree();
        }
    }

    private void HandleQueuedSpawns(int spawnsLeftThisFrame)
    {
        // Spawn from the queue
        while (spawnsLeftThisFrame > 0 && queuedSpawns.Count > 0)
        {
            var current = queuedSpawns.Dequeue();
            var instances = current.Spawner.Instantiate(current.Position);
            if (instances == null)
            {
                --spawnsLeftThisFrame;
                continue;
            }

            foreach (var instance in instances)
            {
                instance!.AddToGroup(Constants.SPAWNED_GROUP);
                worldRoot.AddChild(instance);
                --spawnsLeftThisFrame;
            }
        }
    }

    private record QueuedSpawn(Spawner Spawner, Vector2 Position);
}
