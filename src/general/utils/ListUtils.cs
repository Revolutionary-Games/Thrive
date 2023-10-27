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

    /// <summary>
    ///   Find the index of first matching element
    /// </summary>
    /// <param name="list">The list to search through</param>
    /// <param name="match">Predicate used to check each item</param>
    /// <typeparam name="T">Type of elements</typeparam>
    /// <returns>The matched index or -1 if not found</returns>
    public static int FindIndex<T>(this IReadOnlyList<T> list, Predicate<T> match)
    {
        int length = list.Count;

        for (int i = 0; i < length; ++i)
        {
            if (match.Invoke(list[i]))
                return i;
        }

        return -1;
    }

    /// <summary>
    ///   Removes an item from a list at index without preserving the list order, this should be faster than normal
    ///   list remove that preserves order
    /// </summary>
    /// <param name="list">The list to modify</param>
    /// <param name="indexToRemove">Index in the list to remove an item at</param>
    /// <typeparam name="T">Type of items in the list</typeparam>
    public static void RemoveWithoutPreservingOrder<T>(this IList<T> list, int indexToRemove)
    {
        var itemCount = list.Count;

        if (indexToRemove + 1 == itemCount)
        {
            // Already last
        }
        else
        {
            // Need to swap the last item to the indexToRemove to preserve it
            var temp = list[itemCount - 1];

            list[indexToRemove] = temp;
        }

        list.RemoveAt(itemCount - 1);
    }
}
