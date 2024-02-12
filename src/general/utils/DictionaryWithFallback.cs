using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Allows access to a dictionary with reads falling back to another backing dictionary when the primary dictionary
///   is missing a value
/// </summary>
/// <typeparam name="TKey">Dictionary key type</typeparam>
/// <typeparam name="TValue">Dictionary value type</typeparam>
/// <remarks>
///   <para>
///     The count operation and key counting operations (as well as value counting) are expensive and should be avoided
///   </para>
/// </remarks>
public class DictionaryWithFallback<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TValue : IEquatable<TValue>
{
    [JsonProperty]
    private readonly IDictionary<TKey, TValue> primary;

    [JsonProperty]
    private readonly IDictionary<TKey, TValue> fallback;

    [JsonConstructor]
    public DictionaryWithFallback(IDictionary<TKey, TValue> primary, IDictionary<TKey, TValue> fallback)
    {
        this.primary = primary;
        this.fallback = fallback;

        Keys = new MultiCollection<TKey>(primary.Keys, fallback.Keys);
        Values = new MultiCollection<TValue>(primary.Values, fallback.Values);
    }

    /// <summary>
    ///   Note that this is an expensive operation as this needs to only count the unique keys.
    ///   <see cref="RoughCount"/> is less accurate but much faster.
    /// </summary>
    [JsonIgnore]
    public int Count => Keys.Count;

    [JsonIgnore]
    public int RoughCount => primary.Count + fallback.Count;

    [JsonIgnore]
    public bool IsReadOnly => primary.IsReadOnly;

    [JsonIgnore]
    public ICollection<TKey> Keys { get; }

    [JsonIgnore]
    public ICollection<TValue> Values { get; }

    [JsonIgnore]
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Keys.Select(k => this[k]);

    [JsonIgnore]
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    [JsonIgnore]
    public TValue this[TKey key]
    {
        get
        {
            if (primary.TryGetValue(key, out var result))
            {
                return result;
            }

            return fallback[key];
        }
        set
        {
            primary[key] = value;
            ResetPrimaryIfMatchesFallback(key);
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var alreadySeen = new List<TKey>();

        foreach (var pair in primary)
        {
            yield return pair;

            alreadySeen.Add(pair.Key);
        }

        foreach (var pair in fallback)
        {
            if (alreadySeen.Contains(pair.Key))
                continue;

            yield return pair;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        primary.Add(item);
        ResetPrimaryIfMatchesFallback(item.Key);
    }

    public void Add(TKey key, TValue value)
    {
        primary.Add(key, value);
        ResetPrimaryIfMatchesFallback(key);
    }

    public void Clear()
    {
        primary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return primary.Contains(item) || fallback.Contains(item);
    }

    public bool ContainsKey(TKey key)
    {
        return primary.ContainsKey(key) || fallback.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        // Here we definitely don't want to remove things from the fallback due to the way this is used for compound
        // amounts
        return primary.Remove(item);
    }

    public bool Remove(TKey key)
    {
        // See the comment in the other remove overload
        return primary.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (primary.TryGetValue(key, out value))
            return true;

        return fallback.TryGetValue(key, out value);
    }

    /// <summary>
    ///   Removes a value from primary if it is the same value as the fallback
    /// </summary>
    /// <param name="key">The key to check</param>
    private void ResetPrimaryIfMatchesFallback(TKey key)
    {
        if (primary.TryGetValue(key, out var primaryValue) && fallback.TryGetValue(key, out var fallbackValue) &&
            primaryValue is not null)
        {
            if (primaryValue.Equals(fallbackValue))
            {
                primary.Remove(key);
            }
        }
    }
}
