using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
///   This class implements a shuffle bag, that is, a structure to randomly pick elements from a list,
///   with the specification of not picking an element twice before having picked every element once.
///   It works as a real bag you draw content from, before filling it again when empty.
/// </summary>
/// <typeparam name="T"> Type of the elements in the bag.</typeparam>
public class ShuffleBag<T> : IEnumerable<T?>
{
    private readonly Random random;

    // We use Lists here because the shuffle algorithm rely on access by index, which does not fit LinkedLists.
    private readonly List<T> initialContent = new();
    private readonly List<T> currentContent = new();

    /// <summary>
    ///   This variable encodes if the bag should be automatically refilled by enumerators upon emptying the bag.
    ///   Manual refilling is always possible.
    /// </summary>
    private bool automaticRefill;

    public ShuffleBag(Random random, bool automaticRefill = true)
    {
        this.random = random;

        this.automaticRefill = automaticRefill;
    }

    /// <summary>
    ///   Checks if the bag is currently empty.
    /// </summary>
    public bool IsEmpty => currentContent.Count == 0;

    /// <summary>
    ///   Clears the bag, including its content on refill (so even refilling doesn't add any items back).
    /// </summary>
    public void Clear()
    {
        initialContent.Clear();
        currentContent.Clear();
    }

    /// <summary>
    ///   Adds an element to the bag. This element will be included everytime the bag is refilled.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Adding an element this way is deterministic: this element will always be the one picked first
    ///     if no other element is added.
    ///   </para>
    /// </remarks>
    public void Add(T element)
    {
        initialContent.Add(element);
        currentContent.Add(element);
    }

    /// <summary>
    ///   Removes an element from the bag. This element will no longer be put back when the bag is refilled.
    /// </summary>
    /// <returns>Returns whether the element was indeed in the bag refill content.</returns>
    public bool Remove(T element)
    {
        currentContent.Remove(element);
        return initialContent.Remove(element);
    }

    /// <summary>
    ///   Removes all elements matching a predicate from the bag.
    ///   These elements will no longer be put back when the bag is refilled.
    /// </summary>
    public void RemoveAll(Predicate<T> predicate)
    {
        currentContent.RemoveAll(predicate);
        initialContent.RemoveAll(predicate);
    }

    /// <summary>
    ///   Fills a bag with its refill content, and shuffles everything.
    /// </summary>
    public void FillAndShuffle()
    {
        currentContent.Clear();

        foreach (var element in initialContent)
            currentContent.Add(element);

        Shuffle();
    }

    /// <summary>
    ///   Picks an element from the bag and returns it.
    ///   The element is dropped from the bag but not removed: it will be put back in it at next refill.
    /// </summary>
    public T PickAndDrop()
    {
        var drawnElement = Pick();
        Drop();

        return drawnElement;
    }

    /// <summary>
    ///   Returns an enumerator for this ShuffleBag.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This enumerator will loop through the current content of the bag (unless cut short),
    ///     and will fill it again upon reaching the end of it. It will not loop again.
    ///   </para>
    /// </remarks>
    public IEnumerator<T> GetEnumerator()
    {
        return new ShuffleBagEnumerator<T>(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///   Fisher–Yates Shuffle Alg. (https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle)
    ///   Provides a uniform random shuffle in O(n)
    /// </summary>
    private void Shuffle()
    {
        for (int i = 0; i < currentContent.Count - 2; i++)
        {
            int j = random.Next(i, currentContent.Count);

            (currentContent[i], currentContent[j]) = (currentContent[j], currentContent[i]);
        }
    }

    /// <summary>
    ///   Picks the front element from the shuffled bag.
    ///   If the bag is empty, it refills it beforehand.
    /// </summary>
    private T Pick()
    {
        if (currentContent.Count == 0)
        {
            FillAndShuffle();
        }

        // Removing the last element is due to performance considerations on lists.
        return currentContent[currentContent.Count - 1];
    }

    /// <summary>
    ///   Drops the front element from the shuffled bag.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Although the bag being shuffled ensure its randomness,
    ///     the dropped element is the same as the one that would be picked instead of dropping it.
    ///     This makes it possible to pick an element and drop it altogether.
    ///   </para>
    /// </remarks>
    private void Drop()
    {
        if (currentContent.Count == 0)
            throw new InvalidOperationException("Cannot drop from an empty bag!");

        currentContent.RemoveAt(currentContent.Count - 1);
    }

    /// <summary>
    ///   Enumerator class for the shuffle bag.
    ///   This enumerator will loop through the current content of the bag (unless shortcut),
    ///   and will fill it again upon reaching the end of it. It will not loop again.
    /// </summary>
    private class ShuffleBagEnumerator<T2> : IEnumerator<T2>
    {
        private readonly ShuffleBag<T2> sourceBag;
        private T2? current;

        /// <summary>
        ///   Instantiate the enumerator to loop through what is left in the bag.
        ///   If the bag was emptied, fills it again.
        /// </summary>
        public ShuffleBagEnumerator(ShuffleBag<T2> sourceBag)
        {
            this.sourceBag = sourceBag;

            // If the shuffle bag is empty, just fill and shuffle it, else just use whatever is left in it.
            if (sourceBag.currentContent.Count == 0 && sourceBag.automaticRefill)
                sourceBag.FillAndShuffle();
        }

        /// <summary>
        ///   Returns the current element for the enumerator,
        ///   effectively picking the front element of the shuffle bag without dropping it.
        /// </summary>
        public T2 Current
        {
            get => current ?? throw new InvalidOperationException("This enumerator is not at a valid item");
            private set => current = value;
        }

        object? IEnumerator.Current => Current;

        /// <summary>
        ///   Moves to the next element in the bag.
        /// </summary>
        /// <returns>Returns whether the bag still holds items afterwards.</returns>
        public bool MoveNext()
        {
            if (!sourceBag.IsEmpty)
            {
                Current = sourceBag.PickAndDrop();
                return true;
            }

            current = default;
            return false;
        }

        /// <summary>
        ///   Resets the bag, effectively making it a full and shuffled bag again.
        /// </summary>
        public void Reset()
        {
            sourceBag.FillAndShuffle();
        }

        /// <summary>
        ///   Handles the disposal of the enumerator, i.e. when closing the foreach loop that was using it.
        /// </summary>
        public void Dispose() { }
    }
}
