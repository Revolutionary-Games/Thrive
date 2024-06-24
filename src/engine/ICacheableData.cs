using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Interface for data that can be stored in <see cref="ProceduralDataCache"/>
/// </summary>
/// <remarks>
///   <para>
///     This is disposable to allow releasing extra resources that were allocated when removed from the cache.
///     Note that after dispose this cache data instance is not safe to use at all.
///   </para>
/// </remarks>
public interface ICacheableData : IDisposable
{
    /// <summary>
    ///   Used to check that data returned from cache didn't suffer a hash collision
    /// </summary>
    /// <param name="cacheData">The data read from the cache based on the hash of this object</param>
    /// <returns>True if all data matches, false if the data doesn't match us (indicates a hash conflict)</returns>
    public bool MatchesCacheParameters(ICacheableData cacheData);

    public long ComputeCacheHash();
}

public static class CacheableDataExtensions
{
    private static HashSet<long> reportedHashes = new();

    public static T? FetchDataFromCache<TSource, T>(this TSource currentParameters, Func<long, T?> dataFetch)
        where T : class, ICacheableData
        where TSource : ICacheableData
    {
        var currentHash = currentParameters.ComputeCacheHash();
        var fetchedFromCache = dataFetch(currentHash);

        if (fetchedFromCache == null)
            return null;

        if (!currentParameters.MatchesCacheParameters(fetchedFromCache))
        {
            OnCacheHashCollision<T>(currentHash);
            return null;
        }

        return fetchedFromCache;
    }

    public static T? FetchDataFromCache<T>(this T currentParameters, Func<long, T?> dataFetch)
        where T : class, ICacheableData
    {
        return FetchDataFromCache<T, T>(currentParameters, dataFetch);
    }

    public static void OnCacheHashCollision<T>(long hash)
    {
        // Only report each hash warning once (until clear)
        lock (reportedHashes)
        {
            // Using just the hash and not (hash, T) is probably good enough to report any problems that matter,
            // collisions should anyway be super rare so collisions with different types should be even rarer
            if (reportedHashes.Add(hash))
            {
                GD.PrintErr("Hash collision for procedural cache data. Losing performance due to " +
                    "recomputation! ", "Multiple ", typeof(T).Name, " have hash of ", hash);
            }
        }
    }

    public static void ClearCollisionWarnings()
    {
        lock (reportedHashes)
        {
            reportedHashes.Clear();
        }
    }
}
