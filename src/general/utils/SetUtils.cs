using System;
using System.Collections.Generic;
using Godot;

public static class SetUtils
{
    /// <summary>
    ///   Returns a random item from a set
    /// </summary>
    /// <returns>The random item.</returns>
    /// <param name="items">Items to pick from</param>
    /// <param name="random">Randomness source</param>
    /// <typeparam name="T">Type of item</typeparam>
    public static T Random<T>(this ISet<T> items, Random random)
    {
        if (items == null || items.Count < 1)
            throw new ArgumentException("There must be at least one item", nameof(items));

        int selectedIndex = random.Next(0, items.Count);
        int index = 0;

        // This allocates an enumerator but that's probably the best way
        using var enumerator = items.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            // This shouldn't happen as we checked there's always at least one item
            throw new ArgumentException("Unexpected end of enumerator");
        }

        while (true)
        {
            if (selectedIndex == index)
            {
                // Hit the index we want
                return enumerator.Current;
            }

            ++index;

            // Still searching for the randomly picked index we want
            if (!enumerator.MoveNext())
            {
                // Reached the end of the enumerator, which shouldn't happen as we checked the item count above
                GD.PrintErr("Unexpected end of enumerator when picking a random item");

                // Instead return the first item as a safety fallback
                enumerator.Reset();
                enumerator.MoveNext();
                return enumerator.Current;
            }
        }
    }
}
