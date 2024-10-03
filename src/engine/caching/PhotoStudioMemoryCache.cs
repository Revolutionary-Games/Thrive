using System.Collections.Generic;

/// <summary>
///   Handles caching <see cref="PhotoStudio"/> generated images in memory
/// </summary>
public class PhotoStudioMemoryCache : IComputeCache<ImageTask>
{
    private readonly Dictionary<ulong, CacheEntry> cache = new();
    private readonly List<ulong> keysToClear = new();

    private float time;
    private double timeSinceClean;

    public ImageTask? Get(ulong cacheKey)
    {
        if (cache.TryGetValue(cacheKey, out var cacheEntry))
        {
            cacheEntry.LastAccess = time;
            return cacheEntry.Item;
        }

        return null;
    }

    public void Insert(ImageTask item)
    {
        cache[item.CalculateCacheHash()] = new CacheEntry(item, time);
    }

    public void Insert(ulong cacheKey, ImageTask item)
    {
        cache[cacheKey] = new CacheEntry(item, time);
    }

    public void CleanCacheIfTime(double delta)
    {
        time += (float)delta;
        timeSinceClean += delta;

        if (timeSinceClean >= Constants.MEMORY_PHOTO_CACHE_CLEAN_INTERVAL)
        {
            timeSinceClean = 0;

            foreach (var entry in cache)
            {
                if (time - entry.Value.LastAccess > Constants.MEMORY_PHOTO_CACHE_TIME)
                {
                    keysToClear.Add(entry.Key);
                }
            }

            foreach (var key in keysToClear)
            {
                // TODO: could maybe reuse the cache entries if that improves GC pressure?
                cache.Remove(key);
            }

            keysToClear.Clear();
        }
    }

    public void Clear()
    {
        throw new System.NotImplementedException();
    }

    private class CacheEntry
    {
        public ImageTask Item;
        public float LastAccess;

        public CacheEntry(ImageTask item, float currentTime)
        {
            Item = item;
            LastAccess = currentTime;
        }
    }
}
