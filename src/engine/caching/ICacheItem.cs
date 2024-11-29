﻿/// <summary>
///   Item in a <see cref="IComputeCache{T}"/>
/// </summary>
public interface ICacheItem
{
    /// <summary>
    ///   False when this cache item has not been calculated yet and should not be placed in persistent caches.
    ///   Or alternative this data is only the metadata and needs to be loaded before being used.
    /// </summary>
    public bool Finished { get; }

    public ulong CalculateCacheHash();
}

public interface ILoadableCacheItem : ICacheItem
{
    /// <summary>
    ///   Loads this cache item from the cache. Should do nothing if already loaded.
    /// </summary>
    public void Load();

    /// <summary>
    ///   Unloads the item from memory, but leaves this current instance metadata present so that <see cref="Load"/>
    ///   can be called if the item is needed again. Should set <see cref="ICacheItem.Finished"/> to false.
    /// </summary>
    public void Unload();
}

public interface ISavableCacheItem : ICacheItem
{
    /// <summary>
    ///   Writes this item to disk, can only be called if <see cref="ICacheItem.Finished"/>
    /// </summary>
    public void Save();
}
