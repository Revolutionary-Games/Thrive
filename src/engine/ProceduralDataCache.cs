using System;
using System.Collections.Generic;
using System.Threading;
using Components;
using Godot;

/// <summary>
///   Stores procedurally generated data to speed up things by not requiring it to be recomputed
/// </summary>
[GodotAutoload]
public partial class ProceduralDataCache : Node
{
    private static ProceduralDataCache? instance;

    private readonly Dictionary<long, CacheEntry<MembranePointData>> membraneCache = new();

    private readonly ItemCache<LoadedShapeKey, PhysicsShape> loadedShapes = new();

    private readonly ItemCache<SimpleShapeKey, PhysicsShape> simpleShapes = new();

    private readonly Dictionary<long, CacheEntry<MembraneCollisionShape>> membraneCollisions = new();

    /// <summary>
    ///   When due to a hash collision a value needs to be written anyway on top of a cache entry (that may have been
    ///   returned already, see <see cref="onConflictPreferOlder"/> about how that can be handled safely) this is used
    ///   to perform the disposal of the object later to hopefully not dispose the object while the previous cache
    ///   writer is still using it.
    /// </summary>
    private readonly List<IDisposable> conflictedEntriesToDispose = [];

    private readonly List<KeyValuePair<long, CacheEntry<MembranePointData>>> temporaryList1 = [];
    private readonly List<KeyValuePair<long, CacheEntry<MembraneCollisionShape>>> temporaryList2 = [];

    /// <summary>
    ///   When enabled, prefers older entries in the cache to not mess with already returned data being randomly
    ///   Disposed.
    /// </summary>
    private readonly bool onConflictPreferOlder = true;

    private MainGameState previousState = MainGameState.Invalid;

    private float currentTime;
    private double timeSinceClean;

    /// <summary>
    ///   Counts how many cache-writes are abandoned due to another thread already creating a cache entry for that data
    /// </summary>
    private int wastedRecalculations;

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

        loadedShapes.Clear();
        simpleShapes.Clear();

        lock (membraneCollisions)
        {
            ClearCacheData(membraneCollisions);
        }

        lock (conflictedEntriesToDispose)
        {
            foreach (var disposable in conflictedEntriesToDispose)
            {
                disposable.Dispose();
            }

            conflictedEntriesToDispose.Clear();
        }

        if (instance == this)
            instance = null;
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
            CleanOldCacheEntriesIn(membraneCache, temporaryList1, Constants.PROCEDURAL_CACHE_MEMBRANE_KEEP_TIME);
        }

        loadedShapes.Clean(currentTime, Constants.PROCEDURAL_CACHE_LOADED_SHAPE_KEEP_TIME);
        simpleShapes.Clean(currentTime, Constants.PROCEDURAL_CACHE_SIMPLE_SHAPE_KEEP_TIME);

        lock (membraneCollisions)
        {
            CleanOldCacheEntriesIn(membraneCollisions, temporaryList2, Constants.PROCEDURAL_CACHE_MICROBE_SHAPE_TIME);
        }

        lock (conflictedEntriesToDispose)
        {
            foreach (var disposable in conflictedEntriesToDispose)
            {
                disposable.Dispose();
            }

            conflictedEntriesToDispose.Clear();
        }

        var totalWasted = Interlocked.Exchange(ref wastedRecalculations, 0)
            + loadedShapes.TakeWastedWrites() + simpleShapes.TakeWastedWrites();
        if (totalWasted > 10)
        {
            GD.Print("Cache data computations that were duplicate work: " + totalWasted);
        }

        // Would be better to make clearing this slightly rarer, but for now it is probably fine to leverage the
        // existing processing interval here
        CacheableDataExtensions.ClearCollisionWarnings();
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

    /// <summary>
    ///   Clears the cached membrane point data. Intended for developer/cheat use.
    /// </summary>
    public void ClearMembraneCache()
    {
        lock (membraneCache)
        {
            ClearCacheData(membraneCache);
        }
    }

    public MembranePointData? ReadMembraneData(long hash)
    {
        lock (membraneCache)
        {
            if (!membraneCache.TryGetValue(hash, out var entry))
                return null;

#if DEBUG
            if (entry.Value.Disposed)
                throw new InvalidOperationException("Value was not removed from cache before dispose");
#endif

            entry.LastUsed = currentTime;
            return entry.Value;
        }
    }

    /// <summary>
    ///   Writes calculated membrane data to the cache
    /// </summary>
    /// <param name="pointData">
    ///   The data to write. Taken as a ref parameter to replace the value if another thread just managed to write data
    ///   to the cache.
    /// </param>
    /// <returns>The hash of the cache entry</returns>
    public long WriteMembraneData(ref MembranePointData pointData)
    {
        var hash = pointData.ComputeCacheHash();

        lock (membraneCache)
        {
            // Ensure old data overwrite is done safely if required
            if (TryUseExistingValueBeforeWrite(membraneCache, ref pointData, hash))
                return hash;

            membraneCache[hash] = new CacheEntry<MembranePointData>(pointData, currentTime);
        }

        return hash;
    }

    public PhysicsShape? ReadLoadedShape(string filePath, float density)
    {
        return loadedShapes.Read(new LoadedShapeKey(filePath, density), currentTime);
    }

    public void WriteLoadedShape(string filePath, float density, PhysicsShape shape)
    {
        loadedShapes.Write(new LoadedShapeKey(filePath, density), shape, currentTime);
    }

    public PhysicsShape? ReadSimpleShape(SimpleShapeType type, float size, float density)
    {
        return simpleShapes.Read(new SimpleShapeKey(type, size, density), currentTime);
    }

    public void WriteSimpleShape(SimpleShapeType type, float size, float density, PhysicsShape shape)
    {
        simpleShapes.Write(new SimpleShapeKey(type, size, density), shape, currentTime);
    }

    public MembraneCollisionShape? ReadMembraneCollisionShape(long hash)
    {
        lock (membraneCollisions)
        {
            if (!membraneCollisions.TryGetValue(hash, out var entry))
                return null;

#if DEBUG
            if (entry.Value.Disposed)
                throw new InvalidOperationException("Value was not removed from cache before dispose");
#endif

            entry.LastUsed = currentTime;
            return entry.Value;
        }
    }

    public long WriteMembraneCollisionShape(ref MembraneCollisionShape shape)
    {
        var hash = shape.ComputeCacheHash();

        lock (membraneCollisions)
        {
            if (TryUseExistingValueBeforeWrite(membraneCollisions, ref shape, hash))
                return hash;

            membraneCollisions[hash] = new CacheEntry<MembraneCollisionShape>(shape, currentTime);
        }

        return hash;
    }

    /// <summary>
    ///   Checks existing entry in cache related to a new value. If someone populated the cache just now this replaces
    ///   the new value to be from the cache. Also handles hash collisions.
    /// </summary>
    /// <param name="cache">Cache to operate on</param>
    /// <param name="newValue">New value that wants to be written to the cache</param>
    /// <param name="hash">Hash of <see cref="newValue"/></param>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>
    ///   True if existing value was good and no write should be performed to the cache (the existing value will be
    ///   in <see cref="newValue"/>)
    /// </returns>
    private bool TryUseExistingValueBeforeWrite<T>(Dictionary<long, CacheEntry<T>> cache, ref T newValue, long hash)
        where T : IDisposable, ICacheableData
    {
        // Can skip doing any extra checks if there is nothing in the cache for the hash
        if (!cache.TryGetValue(hash, out var existing))
            return false;

        // Skip adding same object to the cache multiple times
        if (ReferenceEquals(existing.Value, newValue))
        {
            existing.LastUsed = currentTime;
            return true;
        }

        // Replace the given data with one from the cache and dispose the now useless data. But only if it truly
        // matches to work with hash collisions.
        if (existing.Value.MatchesCacheParameters(newValue))
        {
            Interlocked.Increment(ref wastedRecalculations);

            newValue.Dispose();
            newValue = existing.Value;

            existing.LastUsed = currentTime;
            return true;
        }

        // Data didn't match after all, there's a hash collision
        // Print debug info to help diagnose why different data produced the same hash
        try
        {
            if (typeof(T) == typeof(MembranePointData) && existing.Value is MembranePointData oldM &&
                newValue is MembranePointData newM)
            {
                GD.Print($"Hash collision detected for hash {hash}. existing.VertexCount={oldM.VertexCount}" +
                    $", new.VertexCount={newM.VertexCount}");
            }
            else
            {
                GD.Print($"Hash collision detected for hash {hash}. Type={typeof(T)}");
            }
        }
        catch (Exception)
        {
            // Ignore any debug printing errors
        }

        CacheableDataExtensions.OnCacheHashCollision<T>(hash);

        if (!onConflictPreferOlder)
        {
            // Dispose the old cache value that will be overwritten
            lock (conflictedEntriesToDispose)
            {
                conflictedEntriesToDispose.Add(existing.Value);
            }

            return false;
        }

        // Prefer to keep the old cache entry to not dispose already returned data. Tell the caller to assume we fixed
        // things even though the new value is not in the cache.
        return true;
    }

    private void CleanOldCacheEntriesIn<TKey, T>(Dictionary<TKey, CacheEntry<T>> entries,
        List<KeyValuePair<TKey, CacheEntry<T>>> temporaryList, float keepTime)
        where T : ICacheableData
        where TKey : notnull
    {
        if (entries.Count < 1)
            return;

        // This is just incidentally locked in one place
        // ReSharper disable once InconsistentlySynchronizedField
        var cutoff = currentTime - keepTime;

        foreach (var toRemove in entries)
        {
            if (toRemove.Value.LastUsed < cutoff)
            {
                temporaryList.Add(toRemove);
            }
        }

        foreach (var toRemove in temporaryList)
        {
            toRemove.Value.Value.Dispose();
            entries.Remove(toRemove.Key);
        }

        temporaryList.Clear();
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

    // We use these just as keys for equality comparison, so it looks like the values are not used, but they are used
    // in the dictionary implementations
    // ReSharper disable NotAccessedPositionalProperty.Local
    private readonly record struct LoadedShapeKey(string Path, float Density);

    private readonly record struct SimpleShapeKey(SimpleShapeType Type, float Size, float Density);

    // ReSharper restore NotAccessedPositionalProperty.Local

    private class CacheEntry<T>(T value, float currentTime)
    {
        public readonly T Value = value;

        public float LastUsed = currentTime;
    }
}
