using System;
using Godot;

/// <summary>
///   Interface for data that can be stored in <see cref="ProceduralDataCache"/>
/// </summary>
public interface ICacheableData
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
            GD.PrintErr("Hash collision for procedural cache data. Losing performance due to recomputation! ",
                "Multiple ", typeof(T).Name, " have hash of ", currentHash);
            return null;
        }

        return fetchedFromCache;
    }

    public static T? FetchDataFromCache<T>(this T currentParameters, Func<long, T?> dataFetch)
        where T : class, ICacheableData
    {
        return FetchDataFromCache<T, T>(currentParameters, dataFetch);
    }
}
