using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
///   Generic in-memory cache keyed by a value-type identity. Lookups go through the dictionary's own equality.
///   Subclass and override <see cref="OnEvict"/> to react to evictions
///   (see <see cref="DisposableItemCache{TKey,TValue}"/>).
/// </summary>
/// <typeparam name="TKey">Identity used to look up cached values. Equality is delegated to the dictionary.</typeparam>
/// <typeparam name="TValue">
///   Reference type stored in the cache. The cache does not own its lifetime by default.
/// </typeparam>
public class ItemCache<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    private readonly Dictionary<TKey, CacheEntry> entries = new();
    private readonly Lock gate = new();

    private int wastedWrites;

    /// <summary>
    ///   Returns the cached value for <paramref name="key"/> if present, otherwise null. Touches the entry's
    ///   last-used time with <paramref name="currentTime"/> so it survives the next <see cref="Clean"/>.
    /// </summary>
    /// <returns>The cached value, or null if no entry exists for <paramref name="key"/>.</returns>
    public TValue? Read(TKey key, float currentTime)
    {
        lock (gate)
        {
            if (!entries.TryGetValue(key, out var entry))
                return null;

            entry.LastUsed = currentTime;
            return entry.Value;
        }
    }

    /// <summary>
    ///   Stores <paramref name="value"/> under <paramref name="key"/>. If the key is already present with a
    ///   different instance, the previous value is evicted via <see cref="OnEvict"/> and a wasted-write counter
    ///   is incremented (see <see cref="TakeWastedWrites"/>).
    /// </summary>
    public void Write(TKey key, TValue value, float currentTime)
    {
        lock (gate)
        {
            if (entries.TryGetValue(key, out var existing))
            {
                if (ReferenceEquals(existing.Value, value))
                {
                    existing.LastUsed = currentTime;
                    return;
                }

                Interlocked.Increment(ref wastedWrites);
                OnEvict(existing.Value);
            }

            entries[key] = new CacheEntry(value, currentTime);
        }
    }

    /// <summary>
    ///   Returns the count of writes that produced a redundant new instance for an existing key, and resets the
    ///   counter. Used by owners that want to surface cache-thrash telemetry.
    /// </summary>
    /// <returns>The number of wasted writes accumulated since the previous call (or since construction).</returns>
    public int TakeWastedWrites()
    {
        return Interlocked.Exchange(ref wastedWrites, 0);
    }

    /// <summary>
    ///   Evicts entries whose last-used time is older than
    ///   <paramref name="currentTime"/> - <paramref name="keepTime"/>. Each evicted value passes through
    ///   <see cref="OnEvict"/>.
    /// </summary>
    public void Clean(float currentTime, float keepTime)
    {
        lock (gate)
        {
            if (entries.Count < 1)
                return;

            var cutoff = currentTime - keepTime;

            // TODO: avoid this temporary list allocation here
            foreach (var toRemove in entries.Where(e => e.Value.LastUsed < cutoff).ToList())
            {
                OnEvict(toRemove.Value.Value);
                entries.Remove(toRemove.Key);
            }
        }
    }

    /// <summary>
    ///   Evicts every entry, passing each value through <see cref="OnEvict"/>.
    /// </summary>
    public void Clear()
    {
        lock (gate)
        {
            foreach (var entry in entries.Values)
            {
                OnEvict(entry.Value);
            }

            entries.Clear();
        }
    }

    /// <summary>
    ///   Called for each value removed from the cache. Default does nothing. Subclasses can use this to release
    ///   resources owned by the cached items (see <see cref="DisposableItemCache{TKey,TValue}"/>). Called inside the
    ///   cache's internal lock, so overrides must not block on slow work.
    /// </summary>
    protected virtual void OnEvict(TValue value)
    {
    }

    private sealed class CacheEntry(TValue value, float currentTime)
    {
        public readonly TValue Value = value;
        public float LastUsed = currentTime;
    }
}
