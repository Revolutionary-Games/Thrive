using System;
using System.Collections;
using System.Collections.Generic;

// TODO Access
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

    public void Clear()
    {
        initialContent.Clear();
        currentContent.Clear();
    }

    public void Add(T element)
    {
        initialContent.Add(element);
        currentContent.Add(element);
    }

    public void FillAndShuffle()
    {
        currentContent.Clear();

        foreach (var element in initialContent)
            currentContent.Add(element);

        Shuffle();
    }

    public T Pick()
    {
        if (currentContent.Count == 0)
        {
            FillAndShuffle();
        }

        return currentContent[currentContent.Count - 1];
    }

    public void Drop()
    {
        if (currentContent.Count == 0)
            throw new NullReferenceException("Cannot drop from empty bag!");
        currentContent.RemoveAt(currentContent.Count - 1);
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

    private T Draw()
    {
        if (initialContent.Count == 0)
            throw new NullReferenceException("Can't draw from a bag who can't be filled!");

        // TODO Change implem: RemoveAt makes a copy under the hood
        var leftSize = currentContent.Count;

        if (leftSize == 0)
        {
            FillAndShuffle();
            leftSize = currentContent.Count;
        }

        var drawnElement = currentContent[leftSize - 1];
        currentContent.RemoveAt(leftSize - 1);

        return drawnElement;
    }

    private class ShuffleBagEnumerator<T> : IEnumerator<T>
    {
        private ShuffleBag<T> sourceBag;

        public ShuffleBagEnumerator(ShuffleBag<T> sourceBag)
        {
            this.sourceBag = sourceBag;
        }

        public T Current => sourceBag.Pick();

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            sourceBag.Drop();
        }

        public bool MoveNext()
        {
            // WARNING : Given that the bag is refilled, it can infinitely loop.
            return sourceBag.Draw() != null;
        }

        public void Reset()
        {
            sourceBag.FillAndShuffle();
        }
    }
}
