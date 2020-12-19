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
    /// <param name="random">Randomnes source</param>
    /// <typeparam name="TKey">Type of dictionary keys.</typeparam>
    /// <typeparam name="T">Type of dictionary items.</typeparam>
    public static T Random<TKey, T>(this Dictionary<TKey, T> items, Random random)
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

        foreach (var entry in items)
        {
            sum += entry.Value;
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
        Dictionary<T, float> valuesToAdd)
    {
        foreach (var entry in valuesToAdd)
        {
            float existing = 0.0f;

            if (items.ContainsKey(entry.Key))
                existing = items[entry.Key];

            items[entry.Key] = entry.Value + existing;
        }
    }
}
