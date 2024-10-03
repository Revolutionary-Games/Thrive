/// <summary>
///   Interface for various compute generated resources that are cached. For example images of species are cached.
/// </summary>
public interface IComputeCache<T>
    where T : ICacheItem
{
    public T? Get(ulong cacheKey);

    /// <summary>
    ///   Add a new item to this cache
    /// </summary>
    /// <param name="item">The item to add</param>
    public void Insert(T item);

    /// <summary>
    ///   Add a new item to this cache (with a known key to avoid re-computing it)
    /// </summary>
    /// <param name="cacheKey">The key of the item obtained from <see cref="ICacheItem.CalculateCacheHash"/></param>
    /// <param name="item">The item to add</param>
    public void Insert(ulong cacheKey, T item);

    /// <summary>
    ///   Should be called periodically by the owner of the cache to make sure old items are removed when they expire
    /// </summary>
    /// <param name="delta">Time since last call. This should be safe to call in _Process.</param>
    public void CleanCacheIfTime(double delta);

    /// <summary>
    ///   Clears the entire cache. Very dangerous if not used correctly! For on-disk caches this will delete a lot of
    ///   files so may take a while.
    /// </summary>
    public void Clear();
}
