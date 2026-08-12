using System;

/// <summary>
///   <see cref="ItemCache{TKey,TValue}"/> variant that disposes values when they are evicted from the cache.
///   Use this when the cache owns the lifetime of the cached items.
/// </summary>
/// <typeparam name="TKey">Identity used to look up cached values.</typeparam>
/// <typeparam name="TValue">Reference type owned by the cache; disposed on eviction.</typeparam>
public sealed class DisposableItemCache<TKey, TValue> : ItemCache<TKey, TValue>
    where TKey : notnull
    where TValue : class, IDisposable
{
    protected override void OnEvict(TValue value)
    {
        value.Dispose();
    }
}
