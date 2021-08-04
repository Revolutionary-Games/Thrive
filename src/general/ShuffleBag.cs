using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
///     This class implements a shuffle bag, that is, a structure to randomly pick elements from a list,
///     with the specification of not picking an element twice before having picked every element once.
///     It works as a real bag you draw content from, before filling it again when empty.
/// </summary>
/// <typeparam name="T"> Type of the elements in the bag. </typeparam>
public class ShuffleBag<T> : IEnumerable<T>
{
    private Random random;
    private List<T> initialContent;
    private List<T> currentContent;

    public ShuffleBag(Random random)
    {
        initialContent = new List<T>();
        currentContent = new List<T>();
        this.random = random;
    }

    /// <summary>
    ///   Clears the bag, including its content on refill.
    /// </summary>
    public void Clear()
    {
        initialContent.Clear();
        currentContent.Clear();
    }

    /// <summary>
    ///   Adds an element to the bag. This element will be included everytime the bag is refilled.
    /// </summary>
    public void Add(T element)
    {
        initialContent.Add(element);
        currentContent.Add(element);
    }

    /// <summary>
    ///   Removes an element from the bag. This element will no longer be put back when the bag is refilled.
    /// </summary>
    /// <returns> Returns whether the element was indeed in the bag refill content.</returns>
    public bool Remove(T element)
    {
        currentContent.Remove(element);
        return initialContent.Remove(element);
    }

    /// <summary>
    ///   Removes all elements matching a predicate from the bag.
    ///   These elements will no longer be put back when the bag is refilled.
    /// </summary>
    public void RemoveAll(Predicate<T> f)
    {
        currentContent.RemoveAll(f);
        initialContent.RemoveAll(f);
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
    ///   Picks an element from the bag and return it.
    ///   The element is dropped from the bag but not removed: it will be put back in it at next refill.
    /// </summary>
    public T PickAndDrop()
    {
        var drawnElement = Pick();
        Drop();

        return drawnElement;
    }

    /// <summary>
    /// Returns the enumerator for this ShuffleBag.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This enumerator will loop through the current content of the bag (unless shortcut),
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

            T swapedElement = currentContent[i];
            currentContent[i] = currentContent[j];
            currentContent[j] = swapedElement;
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
    ///   <para>
    ///     Pick and drop are kept separate for the enumerator.
    ///   </para>
    /// </remarks>
    /// <returns> Returns wether the bag was left empty after drop. </returns>
    private bool Drop()
    {
        if (currentContent.Count == 0)
            throw new InvalidOperationException("Cannot drop from an empty bag!");

        currentContent.RemoveAt(currentContent.Count - 1);
        return currentContent.Count == 0;
    }

    /// <summary>
    ///   Enumerator class for the shuffle bag.
    ///   This enumerator will loop through the current content of the bag (unless shortcut),
    ///   and will fill it again upon reaching the end of it. It will not loop again.
    /// </summary>
    private class ShuffleBagEnumerator<T1> : IEnumerator<T1>
    {
        private ShuffleBag<T1> sourceBag;

        /// <summary>
        ///   Instantiate the enumerator to loop through what is left in the bag.
        ///   If the bag was emptied, fills it again.
        /// </summary>
        public ShuffleBagEnumerator(ShuffleBag<T1> sourceBag)
        {
            this.sourceBag = sourceBag;

            // If the shuffle bag is empty, just fill and shuffle it, else just use whatever is left in it.
            if (sourceBag.currentContent.Count == 0)
                sourceBag.FillAndShuffle();
        }

        /// <summary>
        ///   Returns the current element for the enumerator,
        ///   effectively picking the front element of the shuffle bag without dropping it.
        /// </summary>
        public T1 Current => sourceBag.Pick();

        object IEnumerator.Current => Current;

        /// <summary>
        ///   Handles the disposal of the enumerator, i.e. when closing the foreach loop that was using it.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        ///   Moves to the next element in the bag.
        /// </summary>
        /// <returns> Returns wether the bag still holds items afterwards. </returns>
        public bool MoveNext()
        {
            var leftItemsInBag = !sourceBag.Drop();
            return leftItemsInBag;
        }

        /// <summary>
        ///   Resets the bag, effectively making it a full and shuffled bag again.
        /// </summary>
        public void Reset()
        {
            sourceBag.FillAndShuffle();
        }
    }
}
