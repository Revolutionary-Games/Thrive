using System;
using System.Collections.Generic;
using System.Linq;

public static class ListUtils
{
    /// <summary>
    ///   Returns a random item from a list
    /// </summary>
    /// <returns>The random item.</returns>
    /// <param name="items">List to select from</param>
    /// <param name="random">Randomness source</param>
    /// <typeparam name="T">Type of list items.</typeparam>
    public static T Random<T>(this List<T> items, Random random)
    {
        if (items == null || items.Count < 1)
            throw new ArgumentException("Can't select a random item from an empty sequence");

        return items[random.Next(0, items.Count)];
    }

    public static T? RandomOrDefault<T>(this List<T>? items, Random random)
    {
        if (items == null || items.Count < 1)
            return default;

        return items[random.Next(0, items.Count)];
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items)
        where T : class
    {
        return items.Where(t => t != null)!;
    }
}
