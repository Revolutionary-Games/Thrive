using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   A list of items that can be marked (but don't implement the relevant interface).
///   Similar to <see cref="ChildObjectCache{TKey,TNode}"/> but for non-Godot types and no keys.
/// </summary>
public class MarkedItemList<T>
    where T : IEquatable<T>
{
    [JsonProperty]
    private readonly List<MarkableWrapper<T>> internalList;

    private readonly List<MarkableWrapper<T>> toDeleteItems = new();

    private readonly Stack<MarkableWrapper<T>> unusedWrappers = new();

    public MarkedItemList()
    {
        internalList = new List<MarkableWrapper<T>>();
    }

    /// <summary>
    ///   Construct with existing list. Note that this takes ownership of the list and will modify it.
    /// </summary>
    /// <param name="existingData">Data to start off with, must be modifiable</param>
    [JsonConstructor]
    public MarkedItemList(List<MarkableWrapper<T>> existingData)
    {
        internalList = existingData;
    }

    public int Count => internalList.Count;
    public bool IsReadOnly => false;

    public bool Add(T item)
    {
        if (Contains(item))
            return false;

        internalList.Add(GetWrapper(item));
        return true;
    }

    /// <summary>
    ///   Adds an item or marks the item as used if already exists
    /// </summary>
    /// <param name="item">Item to check for</param>
    /// <returns>True if the item is new, false if already was in this list</returns>
    public bool AddOrMark(T item)
    {
        if (Contains(item, true))
            return false;

        internalList.Add(GetWrapper(item));
        return true;
    }

    public void UnMarkAll()
    {
        toDeleteItems.Clear();

        foreach (var wrapper in internalList)
        {
            wrapper.Marked = false;
        }
    }

    public IReadOnlyList<MarkableWrapper<T>> GetUnMarkedItems()
    {
        // Internal details are relied on by RemoveUnmarked
        toDeleteItems.Clear();

        foreach (var wrapper in internalList)
        {
            if (!wrapper.Marked)
                toDeleteItems.Add(wrapper);
        }

        return toDeleteItems;
    }

    public int RemoveUnmarked()
    {
        // This relies on this method internally populating toDeleteItems, so be very careful if changing this
        GetUnMarkedItems();

        foreach (var toDelete in toDeleteItems)
        {
            if (!internalList.Remove(toDelete))
            {
                throw new InvalidOperationException("Internal error when trying to delete unmarked items");
            }

            unusedWrappers.Push(toDelete);
        }

        return toDeleteItems.Count;
    }

    public void Clear()
    {
        foreach (var wrapper in internalList)
        {
            // TODO: should wrappers be cleared of object references when returned?
            unusedWrappers.Push(wrapper);

            // Limit unused wrapper count
            if (unusedWrappers.Count > internalList.Count)
                break;
        }

        internalList.Clear();
    }

    public bool Contains(T item, bool markUsed = false)
    {
        foreach (var wrapper in internalList)
        {
            if (item.Equals(wrapper.Item))
            {
                if (markUsed)
                    wrapper.Marked = true;

                return true;
            }
        }

        return false;
    }

    public bool Remove(T item)
    {
        foreach (var wrapper in internalList)
        {
            if (item.Equals(wrapper.Item))
            {
                if (internalList.Remove(wrapper))
                {
                    unusedWrappers.Push(wrapper);
                    return true;
                }

                throw new InvalidOperationException("Failed to delete found item");
            }
        }

        return false;
    }

    private MarkableWrapper<T> GetWrapper(T item)
    {
        if (unusedWrappers.TryPop(out var wrapper))
        {
            wrapper.Item = item;
            wrapper.Marked = true;
            return wrapper;
        }

        wrapper = new MarkableWrapper<T>(item)
        {
            Marked = true,
        };

        return wrapper;
    }
}
