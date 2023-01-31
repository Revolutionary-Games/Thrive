using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Stores procedurally generated data to speed up things by not requiring it to be recomputed
/// </summary>
public class ProceduralDataCache : Node
{
    private static ProceduralDataCache? instance;

    private readonly Dictionary<long, CacheEntry<ComputedMembraneData>> membraneCache = new();

    private MainGameState previousState = MainGameState.Invalid;

    private float currentTime;
    private float timeSinceClean;

    private ProceduralDataCache()
    {
        instance = this;
    }

    public static ProceduralDataCache Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Process(float delta)
    {
        currentTime += delta;
        timeSinceClean += delta;

        if (timeSinceClean > Constants.PROCEDURAL_CACHE_CLEAN_INTERVAL)
        {
            timeSinceClean = 0;

            CleanOldCacheEntriesIn(membraneCache, Constants.PROCEDURAL_CACHE_MEMBRANE_KEEP_TIME);
        }
    }

    /// <summary>
    ///   Notify about entering a game state. Used to clear unnecessary cached data
    /// </summary>
    /// <param name="gameState">The new game state the game is moving to</param>
    /// <remarks>
    ///   <para>
    ///     Note that we don't clear resources when exiting to the main menu as it's possible the player will then
    ///     immediately load a save of the same stage they were in previously, wasting time.
    ///   </para>
    /// </remarks>
    /// TODO: need to add calls to this
    public void OnEnterState(MainGameState gameState)
    {
        // Editor(s) should keep the same data cache as the stage(s)
        if (gameState == MainGameState.MicrobeEditor)
            gameState = MainGameState.MicrobeStage;

        if (previousState == gameState)
            return;

        previousState = gameState;

        if (gameState != MainGameState.MicrobeStage)
        {
            membraneCache.Clear();
        }
    }

    public ComputedMembraneData? ReadMembraneData(long hash)
    {
        if (!membraneCache.TryGetValue(hash, out var entry))
            return null;

        entry.LastUsed = currentTime;
        return entry.Value;
    }

    public void WriteMembraneData(ComputedMembraneData data)
    {
        membraneCache[data.ComputeCacheHash()] = new CacheEntry<ComputedMembraneData>(data, currentTime);
    }

    private void CleanOldCacheEntriesIn<TKey, T>(Dictionary<TKey, CacheEntry<T>> entries, float keepTime)
    {
        if (entries.Count < 1)
            return;

        var cutoff = currentTime - keepTime;

        foreach (var toRemove in entries.Where(e => e.Value.LastUsed < cutoff).ToList())
        {
            entries.Remove(toRemove.Key);
        }
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
}
