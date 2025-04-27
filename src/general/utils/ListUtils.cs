using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

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

    public static T Random<[MustBeVariant] T>(this Array<T> items, Random random)
    {
        if (items == null || items.Count < 1)
            throw new ArgumentException("Can't select a random item from an empty sequence");

        return items[random.Next(0, items.Count)];
    }

    public static Variant Random(this Array items, Random random)
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

    public static int RandomElementIndexByProbability(this IReadOnlyList<double> chances, double probability)
    {
        double cumulative = 0.0;
        var count = chances.Count;
        for (var i = 0; i < count; ++i)
        {
            cumulative += chances[i];
            if (probability <= cumulative)
                return i;
        }

        throw new ArgumentException("Chances list is empty");
    }

    /// <summary>
    ///   <inheritdoc cref="ShuffleBag{T}.Shuffle"/>
    /// </summary>
    public static void Shuffle<T>(this IList<T> list, Random random)
    {
        int length = list.Count;
        for (int i = 0; i < length - 1; ++i)
        {
            int j = random.Next(i, length);

            (list[i], list[j]) = (list[j], list[i]);
        }
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
    ///   Finds the index of the first pair, the first element of which is equl to <paramref name="key"/>
    /// </summary>
    /// <typeparam name="TKey">Type of the pairs' first element</typeparam>
    /// <typeparam name="TValue">Type of the pairs' second element</typeparam>
    /// <param name="list">The list to seartch through</param>
    /// <param name="key">The first element of the pair that needs to be found</param>
    /// <returns>The index of the found element or -1 if not found</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindIndexByKey<TKey, TValue>(this IReadOnlyList<(TKey Key, TValue Value)> list, TKey key)
    {
        var comparer = EqualityComparer<TKey>.Default;

        for (int i = 0; i < list.Count; ++i)
        {
            if (comparer.Equals(key, list[i].Key))
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

    /// <summary>
    ///   Finds the closest value in the list of values and returns that
    /// </summary>
    /// <param name="valueToSearchFor">The value to look for</param>
    /// <param name="values">Where to search for the value</param>
    /// <returns>The closest element in <see cref="values"/> when compared to <see cref="valueToSearchFor"/></returns>
    public static int FindClosestValue(int valueToSearchFor, params int[] values)
    {
        if (values.Length < 1)
            throw new ArgumentException("Must be given at least one value to match against");

        var closest = values[0];
        var closestDistance = Math.Abs(valueToSearchFor - closest);

        for (int i = 1; i < values.Length; ++i)
        {
            var distance = Math.Abs(valueToSearchFor - values[i]);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = values[i];
            }
        }

        return closest;
    }
}
