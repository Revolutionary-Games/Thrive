using System;
using System.Collections;
using System.Collections.Generic;

public class ShuffleBag<T> : IEnumerable<T>
{
    private Random random;
    private List<T> initialContent;
    private List<T> currentContent;
    private int capacity;

    public ShuffleBag(Random random)
    {
        capacity = 0;
        initialContent = new List<T>();
        currentContent = new List<T>();
        this.random = random;
    }

    public void Clear()
    {
        initialContent.Clear();
        currentContent.Clear();
        capacity = 0;
    }

    public void Add(T element)
    {
        initialContent.Add(element);
        currentContent.Add(element);
        capacity += 1;
    }

    public bool Remove(T element)
    {
        currentContent.Remove(element);
        return initialContent.Remove(element);
    }

    public void RemoveAll(Predicate<T> f)
    {
        initialContent.RemoveAll(f);
    }

    public void FillAndShuffle()
    {
        currentContent.Clear();

        foreach (var element in initialContent)
            currentContent.Add(element);

        Shuffle();
    }

    public T PickAndDrop()
    {
        var drawnElement = Pick();
        Drop();

        return drawnElement;
    }

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

    private T Pick()
    {
        if (currentContent.Count == 0)
        {
            FillAndShuffle();
        }

        return currentContent[currentContent.Count - 1];
    }

    private void Drop()
    {
        if (currentContent.Count == 0)
            throw new NullReferenceException("Cannot drop from empty bag!");

        currentContent.RemoveAt(currentContent.Count - 1);
    }

    private class ShuffleBagEnumerator<T1> : IEnumerator<T1>
    {
        private ShuffleBag<T1> sourceBag;

        public ShuffleBagEnumerator(ShuffleBag<T1> sourceBag)
        {
            this.sourceBag = sourceBag;
        }

        public T1 Current => sourceBag.Pick();

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            sourceBag.Drop();
        }

        public bool MoveNext()
        {
            // WARNING : Given that the bag is refilled, it can infinitely loop.
            return sourceBag.capacity != 0;
        }

        public void Reset()
        {
            sourceBag.FillAndShuffle();
        }
    }
}
