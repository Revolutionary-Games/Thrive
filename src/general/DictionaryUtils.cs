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

    public static T[] GetSortedKeyArray<T>(this Dictionary<T, long> dictionary)
    {
        var orderedArray = new T[dictionary.Count];
        dictionary.Keys.CopyTo(orderedArray, 0);

        // Sorting by insertion, asymptotically sub-optimal (quadratic) but usually efficient on small datasets.
        for (int i = 1; i < orderedArray.Length; i++)
        {
            var keyToSort = orderedArray[i];
            var valueToSort = dictionary[keyToSort];

            // Sort value at index i within the previous ones, already sorted from low to high
            for (int j = i; j >= 0; j--)
            {
                // No smaller value found, just place it at the bottom.
                if (j == 0)
                {
                    orderedArray[j] = keyToSort;
                    break;
                }

                var referenceObject = orderedArray[j - 1];

                // If we are above the before index, just plug it here and stop.
                if (valueToSort > dictionary[referenceObject])
                {
                    orderedArray[j] = keyToSort;
                    break;
                }

                // Else just shift the hole to place the key in and continue.
                orderedArray[j] = referenceObject;
            }
        }

        return orderedArray;
    }
}
