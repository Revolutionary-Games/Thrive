using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Stores procedurally generated data to speed up things by not requiring it to be recomputed
/// </summary>
public partial class ProceduralDataCache : Node
{
    private static ProceduralDataCache? instance;

    private readonly Dictionary<long, CacheEntry<MembranePointData>> membraneCache = new();

    private readonly Dictionary<long, CacheEntry<CacheableShape>> loadedShapes = new();

    private readonly Dictionary<long, CacheEntry<MembraneCollisionShape>> membraneCollisions = new();

    private MainGameState previousState = MainGameState.Invalid;

    private float currentTime;
    private double timeSinceClean;

    private ProceduralDataCache()
    {
        instance = this;
    }

    public static ProceduralDataCache Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _ExitTree()
    {
        base._ExitTree();

        // Clear all caches on shutdown to get a cleaner game shutdown without a bunch of still alive resources

        lock (membraneCache)
        {
            ClearCacheData(membraneCache);
        }

        lock (loadedShapes)
        {
            ClearCacheData(loadedShapes);
        }

        lock (membraneCollisions)
        {
            ClearCacheData(membraneCollisions);
        }
    }

    public override void _Process(double delta)
    {
        timeSinceClean += delta;

        // This is just incidentally used inside lock blocks
        // ReSharper disable once InconsistentlySynchronizedField
        currentTime += (float)delta;

        if (!(timeSinceClean > Constants.PROCEDURAL_CACHE_CLEAN_INTERVAL))
            return;

        timeSinceClean = 0;

        lock (membraneCache)
        {
            CleanOldCacheEntriesIn(membraneCache, Constants.PROCEDURAL_CACHE_MEMBRANE_KEEP_TIME);
        }

        lock (loadedShapes)
        {
            CleanOldCacheEntriesIn(loadedShapes, Constants.PROCEDURAL_CACHE_LOADED_SHAPE_KEEP_TIME);
        }

        lock (membraneCollisions)
        {
            CleanOldCacheEntriesIn(membraneCollisions, Constants.PROCEDURAL_CACHE_MICROBE_SHAPE_TIME);
        }
    }

    /// <summary>
    ///   Notify about entering a game state. Used to clear unnecessary cached pointData
    /// </summary>
    /// <param name="gameState">The new game state the game is moving to</param>
    /// <remarks>
    ///   <para>
    ///     Note that we don't clear resources when exiting to the main menu as it's possible the player will then
    ///     immediately load a save of the same stage they were in previously, wasting time.
    ///   </para>
    /// </remarks>
    public void OnEnterState(MainGameState gameState)
    {
        // Editor(s) should keep the same pointData cache as the stage(s)
        if (gameState == MainGameState.MicrobeEditor)
            gameState = MainGameState.MicrobeStage;

        if (previousState == gameState)
            return;

        previousState = gameState;

        if (gameState != MainGameState.MicrobeStage)
        {
            lock (membraneCache)
            {
                ClearCacheData(membraneCache);
            }

            lock (membraneCollisions)
            {
                ClearCacheData(membraneCollisions);
            }
        }
    }

    public MembranePointData? ReadMembraneData(long hash)
    {
        lock (membraneCache)
        {
            if (!membraneCache.TryGetValue(hash, out var entry))
                return null;

            entry.LastUsed = currentTime;
            return entry.Value;
        }
    }

    /// <summary>
    ///   Writes calculated membrane data to the cache
    /// </summary>
    /// <param name="pointData">The data to write</param>
    /// <returns>The hash of the cache entry</returns>
    public long WriteMembraneData(MembranePointData pointData)
    {
        var hash = pointData.ComputeCacheHash();

        lock (membraneCache)
        {
            // Ensure old data is not lost without disposing
            if (membraneCache.TryGetValue(hash, out var existing))
            {
                // Skip adding same object to the cache multiple times
                if (ReferenceEquals(existing.Value, pointData))
                {
                    existing.LastUsed = currentTime;
                    return hash;
                }

                existing.Value.Dispose();
            }

            membraneCache[hash] = new CacheEntry<MembranePointData>(pointData, currentTime);
        }

        return hash;
    }

    public PhysicsShape? ReadLoadedShape(string filePath, float density)
    {
        var hash = CacheableShape.CalculateHash(filePath, density);

        lock (loadedShapes)
        {
            if (!loadedShapes.TryGetValue(hash, out var entry))
                return null;

            entry.LastUsed = currentTime;
            return entry.Value.Shape;
        }
    }

    public long WriteLoadedShape(string filePath, float density, PhysicsShape shape)
    {
        var hash = CacheableShape.CalculateHash(filePath, density);

        lock (loadedShapes)
        {
            if (loadedShapes.TryGetValue(hash, out var existing))
            {
                // Skip adding same object to the cache multiple times
                if (ReferenceEquals(existing.Value.Shape, shape))
                {
                    existing.LastUsed = currentTime;
                    return hash;
                }

                existing.Value.Dispose();
            }

            loadedShapes[hash] =
                new CacheEntry<CacheableShape>(new CacheableShape(shape, filePath, density), currentTime);
        }

        return hash;
    }

    public MembraneCollisionShape? ReadMembraneCollisionShape(long hash)
    {
        lock (membraneCollisions)
        {
            if (!membraneCollisions.TryGetValue(hash, out var entry))
                return null;

            entry.LastUsed = currentTime;
            return entry.Value;
        }
    }

    public long WriteMembraneCollisionShape(MembraneCollisionShape shape)
    {
        var hash = shape.ComputeCacheHash();

        lock (membraneCollisions)
        {
            if (membraneCollisions.TryGetValue(hash, out var existing))
            {
                // Skip adding same object to the cache multiple times
                if (ReferenceEquals(existing.Value, shape))
                {
                    existing.LastUsed = currentTime;
                    return hash;
                }

                existing.Value.Dispose();
            }

            membraneCollisions[hash] = new CacheEntry<MembraneCollisionShape>(shape, currentTime);
        }

        return hash;
    }

    private void CleanOldCacheEntriesIn<TKey, T>(Dictionary<TKey, CacheEntry<T>> entries, float keepTime)
        where T : ICacheableData
        where TKey : notnull
    {
        if (entries.Count < 1)
            return;

        // This is just incidentally locked in one place
        // ReSharper disable once InconsistentlySynchronizedField
        var cutoff = currentTime - keepTime;

        // TODO: avoid this temporary list allocation here
        foreach (var toRemove in entries.Where(e => e.Value.LastUsed < cutoff).ToList())
        {
            toRemove.Value.Value.Dispose();
            entries.Remove(toRemove.Key);
        }
    }

    private void ClearCacheData<TKey, T>(Dictionary<TKey, CacheEntry<T>> entries)
        where T : ICacheableData
        where TKey : notnull
    {
        foreach (var entry in entries)
        {
            entry.Value.Value.Dispose();
        }

        entries.Clear();
    }

    private class CacheEntry<T>
    {
        /// <summary>
        ///   The value stored in this entry
        /// </summary>
        public readonly T Value;

        public float LastUsed;

        public CacheEntry(T value, float currentTime)
        {
            Value = value;
            LastUsed = currentTime;
        }
    }

    private class CacheableShape : ICacheableData
    {
        private readonly string path;
        private readonly float density;

        public CacheableShape(PhysicsShape shape, string path, float density)
        {
            this.path = path;
            this.density = density;
            Shape = shape;
        }

        public PhysicsShape Shape { get; }

        public static long CalculateHash(string path, float density)
        {
            return path.GetHashCode() + ((long)density.GetHashCode() << 32);
        }

        public bool MatchesCacheParameters(ICacheableData cacheData)
        {
            if (cacheData is CacheableShape otherShape)
                return path == otherShape.path && density == otherShape.density;

            return false;
        }

        public long ComputeCacheHash()
        {
            return CalculateHash(path, density);
        }

        public void Dispose()
        {
            // Don't dispose point as something else might still be referring to it
            // Shape.Dispose();
        }
    }
}
