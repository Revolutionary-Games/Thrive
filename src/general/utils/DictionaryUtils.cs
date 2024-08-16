using System;
using System.Collections.Generic;
using System.Linq;

public static class DictionaryUtils
{
    /// <summary>
    ///   Returns a random value item from a dictionary
    /// </summary>
    /// <returns>The random item.</returns>
    /// <param name="items">Dictionary to select from</param>
    /// <param name="random">Randomness source</param>
    /// <typeparam name="TKey">Type of dictionary keys.</typeparam>
    /// <typeparam name="T">Type of dictionary items.</typeparam>
    public static T? Random<TKey, T>(this Dictionary<TKey, T>? items, Random random)
        where TKey : notnull
    {
        if (items == null || items.Count < 1)
            return default;

        // TODO: maybe there is a better way to do this
        return items[items.Keys.ToList()[random.Next(0, items.Keys.Count)]];
    }

    // Apparently C# doesn't support operator constraints on generics,
    // so this ends up being like this

    /// <summary>
    ///   Sums the values in a dictionary
    /// </summary>
    /// <returns>The sum.</returns>
    /// <param name="items">Dictionary to sum things in</param>
    public static float SumValues<T>(this Dictionary<T, float> items)
        where T : notnull
    {
        float sum = 0.0f;

        foreach (var entry in items.Values)
        {
            sum += entry;
        }

        return sum;
    }

    /// <summary>
    ///   Merge the values in items and valuesToAdd. When both dictionaries
    ///   contain the same key the result in items has the sum of the values
    ///   under the same key.
    /// </summary>
    /// <param name="items">Items to add things to. As well as the result</param>
    /// <param name="valuesToAdd">Values to add to items.</param>
    public static void Merge<T>(this Dictionary<T, float> items, IReadOnlyDictionary<T, float> valuesToAdd)
        where T : notnull
    {
        foreach (var entry in valuesToAdd)
        {
            items.TryGetValue(entry.Key, out var existing);
            items[entry.Key] = entry.Value + existing;
        }
    }

    public static void Merge<T>(this Dictionary<T, float> items, Dictionary<T, float> valuesToAdd)
        where T : notnull
    {
        foreach (var entry in valuesToAdd)
        {
            items.TryGetValue(entry.Key, out var existing);
            items[entry.Key] = entry.Value + existing;
        }
    }

    /// <summary>
    ///   Creates a new merged dictionary with summed keys
    /// </summary>
    public static IReadOnlyDictionary<T, int> AsMerged<T>(this IReadOnlyDictionary<T, int> items1,
        IReadOnlyDictionary<T, int> items2)
        where T : notnull
    {
        var result = items1.CloneShallow();

        foreach (var entry in items2)
        {
            result.TryGetValue(entry.Key, out var existing);

            result[entry.Key] = entry.Value + existing;
        }

        return result;
    }

    /// <summary>
    ///   Divide all values in a dictionary
    /// </summary>
    /// <param name="dictionary">Dictionary to divide the values in.</param>
    /// <param name="divisor">The divisor to use.</param>
    public static void DivideBy<T>(this Dictionary<T, float> dictionary, float divisor)
        where T : notnull
    {
        // Looks like there isn't really a better way than having to make a copy of the keys
        foreach (var key in dictionary.Keys.ToList())
        {
            dictionary[key] /= divisor;
        }
    }

    /// <summary>
    ///   An equals check that works to check if two dictionaries have the same items
    /// </summary>
    /// <returns>True if equal</returns>
    public static bool DictionaryEquals<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
        Dictionary<TKey, TValue> other)
        where TKey : notnull
        where TValue : IEquatable<TValue>
    {
        if (dictionary.Count != other.Count)
            return false;

        var keys1 = dictionary.Keys;

        foreach (var key in keys1)
        {
            var value1 = dictionary[key];

            if (!other.TryGetValue(key, out var value2))
                return false;

            if (!value1.Equals(value2))
                return false;
        }

        return true;
    }

    public static bool DictionaryEquals<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items1,
        IEnumerable<KeyValuePair<TKey, TValue>> items2)
        where TKey : IEquatable<TKey>
        where TValue : IEquatable<TValue>
    {
        // When working with enumerables it is not possible to avoid allocations in any case so this should be fine
        // to allocate enumerators like this
        using var enumerator1 = items1.GetEnumerator();
        using var enumerator2 = items2.GetEnumerator();

        while (enumerator1.MoveNext())
        {
            // Fail if different count
            if (!enumerator2.MoveNext())
                return false;

            var value1 = enumerator1.Current;
            var value2 = enumerator2.Current;

            if (!value1.Value.Equals(value2.Value))
                return false;

            if (!value1.Key.Equals(value2.Key))
                return false;
        }

        // Fail if different number of items
        if (enumerator2.MoveNext())
            return false;

        return true;
    }

    public static Dictionary<TKey, TValue> CloneShallow<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>(dictionary.Count);

        foreach (var pair in dictionary)
        {
            result.Add(pair.Key, pair.Value);
        }

        return result;
    }

    public static Dictionary<TKey, TValue> CloneShallow<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>(dictionary.Count);

        foreach (var pair in dictionary)
        {
            result.Add(pair.Key, pair.Value);
        }

        return result;
    }
}
