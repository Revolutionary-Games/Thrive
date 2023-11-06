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
    public static void Merge<T>(this Dictionary<T, float> items,
        IReadOnlyDictionary<T, float> valuesToAdd)
    {
        foreach (var entry in valuesToAdd)
        {
            if (!items.TryGetValue(entry.Key, out float existing))
                existing = 0;

            items[entry.Key] = entry.Value + existing;
        }
    }

    /// <summary>
    ///   Creates a new merged dictionary with summed keys
    /// </summary>
    public static IReadOnlyDictionary<T, int> AsMerged<T>(this IReadOnlyDictionary<T, int> items1,
        IReadOnlyDictionary<T, int> items2)
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
    {
        // Looks like there isn't really a better way than having to make a copy of the keys
        foreach (var key in dictionary.Keys.ToList())
        {
            dictionary[key] /= divisor;
        }
    }

    public static Dictionary<TKey, TValue> CloneShallow<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary)
    {
        var result = new Dictionary<TKey, TValue>(dictionary.Count);

        foreach (var pair in dictionary)
        {
            result.Add(pair.Key, pair.Value);
        }

        return result;
    }
}
