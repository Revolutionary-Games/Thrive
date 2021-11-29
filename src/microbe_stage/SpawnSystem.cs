using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Godot;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
{
    /// <summary>
    ///   Root node to parent all spawned things to
    /// </summary>
    private readonly Node worldRoot;

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
    private QueuedSpawn queuedSpawns;

    private Dictionary<Sector, float> sectorDensities = new Dictionary<Sector, float>();

    public SpawnSystem(Node root)
    {
        worldRoot = root;

        noise = new FastNoiseLite(25565);
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

    public void OnPlayerSectorChanged(Sector? oldSector, Sector newSector)
    {
        // TODO: Load and unload sectors
        _ = oldSector;
        _ = newSector;
    }

    public float GetSectorDensity(Sector sector)
    {
        if (sectorDensities.ContainsKey(sector))
            return sectorDensities[sector];

        var density = noise.GetNoise(sector.X, sector.Y);
        density = (density + 1f) / 2f;
        sectorDensities.Add(sector, density);
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
        queuedSpawns = null;
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);

        foreach (Node entity in spawnedEntities)
        {
            if (!entity.IsQueuedForDeletion())
            {
                var spawned = entity as SpawnedRigidBody;

                if (spawned == null)
                {
                    GD.PrintErr("A node has been put in the spawned group but it isn't derived from SpawnedRigidBody");
                    continue;
                }

                spawned.DestroyDetachAndQueueFree();
            }
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

    private void HandleQueuedSpawns(int spawnsLeftThisFrame)
    {
        // Spawn from the queue
        while (spawnsLeftThisFrame > 0)
        {
            if (!queuedSpawns.Spawns.MoveNext())
            {
                // Ended
                queuedSpawns.Spawns.Dispose();
                queuedSpawns = null;
                break;
            }

            --spawnsLeftThisFrame;
        }
    }

    private class QueuedSpawn
    {
        public IEnumerator<SpawnedRigidBody> Spawns;

        public QueuedSpawn(IEnumerator<SpawnedRigidBody> spawner)
        {
            Spawns = spawner;
        }
    }
}
