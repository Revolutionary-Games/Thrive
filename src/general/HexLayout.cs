using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using JetBrains.Annotations;

/// <summary>
///   Base class implementing the basic structure for holding layouts composed of hexes (for example, microbe's
///   organelles)
/// </summary>
/// <remarks>
///   <para>
///     Various methods here take in a "temporaryStorage" parameter. This is used for storing random stuff during the
///     method execution to avoid extra memory allocations. Some methods require multiple
///   </para>
/// </remarks>
/// <typeparam name="T">The concrete type of the hex to hold</typeparam>
public abstract class HexLayout<T> : ICollection<T>, IReadOnlyList<T>, IReadOnlyHexLayout<T>
    where T : class, IPositionedHex
{
    protected HexLayoutView existingHexes;

    // This and the next property are protected to make JSON work
    protected Action<T>? onAdded;

    protected Action<T>? onRemoved;

    public HexLayout(Action<T>? onAdded, Action<T>? onRemoved = null)
    {
        this.onAdded = onAdded;
        this.onRemoved = onRemoved;

        existingHexes = new HexLayoutView([], false);
    }

    public HexLayout()
    {
        existingHexes = new HexLayoutView([], false);
    }

    public HexLayout(List<T> hexes)
    {
        existingHexes = new HexLayoutView(hexes, true, true);
    }

    /// <summary>
    ///   Derived type JSON constructor (for types that need this to force proper deserialization)
    /// </summary>
    protected HexLayout(List<T> hexes, Action<T>? onAdded, Action<T>? onRemoved) : this(hexes)
    {
        this.onAdded = onAdded;
        this.onRemoved = onRemoved;
    }

    /// <summary>
    ///   Number of contained hex-based elements
    /// </summary>
    public int Count => existingHexes.Count;

    public bool IsReadOnly => false;

    /// <summary>
    ///   Access stored layout elements by index
    /// </summary>
    public T this[int index] => existingHexes[index];

    /// <summary>
    ///   Adds a new hex-based element to this layout. Throws if overlaps or can't be placed. This is the preferred
    ///   add method variant as this doesn't need to allocate extra memory.
    /// </summary>
    public void AddFast(T hex, List<Hex> temporaryStorage, List<Hex> temporaryStorage2)
    {
        if (!CanPlace(hex, temporaryStorage, temporaryStorage2))
        {
            throw new ArgumentException($"{typeof(T).Name} can't be placed at this location " +
                $"({hex} at {hex.Position})");
        }

        existingHexes.Add(hex);
        onAdded?.Invoke(hex);
    }

    /// <summary>
    ///   Adds a new hex-based element to this layout if possible.
    /// </summary>
    /// <returns>True if it could be placed</returns>
    public bool AddIfPossible(T hex, List<Hex> temporaryStorage, List<Hex> temporaryStorage2)
    {
        if (!CanPlace(hex, temporaryStorage, temporaryStorage2))
            return false;

        existingHexes.Add(hex);
        onAdded?.Invoke(hex);
        return true;
    }

    /// <summary>
    ///   Generic interface implementation of adding. Note that this allocates memory and should be avoided.
    /// </summary>
    public void Add(T hex)
    {
        if (!CanPlaceAllocating(hex))
        {
            throw new ArgumentException($"{typeof(T).Name} can't be placed at this location " +
                $"({hex} at {hex.Position})");
        }

        existingHexes.Add(hex);
        onAdded?.Invoke(hex);
    }

    /// <summary>
    ///   Explicit name for the slow variant
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSlow(T hex)
    {
        Add(hex);
    }

    /// <summary>
    ///   Returns true if hex can be placed at a location. Only checks that the location doesn't overlap with any
    ///   existing hexes
    /// </summary>
    public virtual bool CanPlace(T hex, List<Hex> temporaryStorage, List<Hex> temporaryStorage2)
    {
#if DEBUG
        if (ReferenceEquals(temporaryStorage, temporaryStorage2))
            throw new ArgumentException("Temporary storage may not point to the same object");
#endif

        var position = hex.Position;

        // Check for overlapping hexes with existing hexes
        GetHexComponentPositions(hex, temporaryStorage);

        int count = temporaryStorage.Count;

        for (int i = 0; i < count; ++i)
        {
            if (GetElementAt(temporaryStorage[i] + position, temporaryStorage2) != null)
                return false;
        }

        return true;
    }

    /// <summary>
    ///   Can-place variant that allocates temporary memory
    /// </summary>
    public virtual bool CanPlaceAllocating(T hex)
    {
        var position = hex.Position;

        var temporaryStorage = new List<Hex>();
        var temporaryStorage2 = new List<Hex>();

        // Check for overlapping hexes with existing hexes
        GetHexComponentPositions(hex, temporaryStorage);

        int count = temporaryStorage.Count;

        for (int i = 0; i < count; ++i)
        {
            if (GetElementAt(temporaryStorage[i] + position, temporaryStorage2) != null)
                return false;
        }

        return true;
    }

    /// <summary>
    ///   Returns true if CanPlace would return true and an existing
    ///   hex touches one of the new hexes, or is the last hex and can be replaced.
    /// </summary>
    public virtual bool CanPlaceAndIsTouching(T hex, List<Hex> temporaryStorage, List<Hex> temporaryStorage2)
    {
        if (!CanPlace(hex, temporaryStorage, temporaryStorage2))
            return false;

        return IsTouchingExistingHex(hex, temporaryStorage, temporaryStorage2);
    }

    /// <summary>
    ///   Returns true if the specified hex is touching an already added hex
    /// </summary>
    public bool IsTouchingExistingHex(T hex, List<Hex> temporaryStorage, List<Hex> temporaryStorage2)
    {
#if DEBUG
        if (ReferenceEquals(temporaryStorage, temporaryStorage2))
            throw new ArgumentException("Temporary storage may not point to the same object");
#endif

        GetHexComponentPositions(hex, temporaryStorage);

        int count = temporaryStorage.Count;

        for (int i = 0; i < count; ++i)
        {
            if (CheckIfAHexIsNextTo(temporaryStorage[i] + hex.Position, temporaryStorage2))
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Returns true if there is some placed hex that has a
    ///   hex next to the specified location.
    /// </summary>
    public bool CheckIfAHexIsNextTo(Hex location, List<Hex> temporaryStorage)
    {
        return
            GetElementAt(location + new Hex(0, -1), temporaryStorage) != null ||
            GetElementAt(location + new Hex(1, -1), temporaryStorage) != null ||
            GetElementAt(location + new Hex(1, 0), temporaryStorage) != null ||
            GetElementAt(location + new Hex(0, 1), temporaryStorage) != null ||
            GetElementAt(location + new Hex(-1, 1), temporaryStorage) != null ||
            GetElementAt(location + new Hex(-1, 0), temporaryStorage) != null;
    }

    /// <summary>
    ///   Searches the hex list for a hex at the specified hex
    /// </summary>
    public T? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        int count = existingHexes.Count;

        // This uses a manual loop as this method is called a lot, so this needs to ensure that this doesn't do any
        // unnecessary computations
        for (int i = 0; i < count; ++i)
        {
            var existingHex = existingHexes[i];

            temporaryHexesStorage.Clear();
            GetHexComponentPositions(existingHex, temporaryHexesStorage);

            int hexCount = temporaryHexesStorage.Count;

            for (int j = 0; j < hexCount; ++j)
            {
                if (temporaryHexesStorage[j] + existingHex.Position == location)
                {
                    return existingHex;
                }
            }
        }

        return null;
    }

    /// <summary>
    ///   Search variant that searches for just root positions, not general overlap. Don't use this variant unless you
    ///   know exactly what that means and why this might miss something that overlaps the new position.
    /// </summary>
    /// <param name="location">Where to check for root items exactly</param>
    /// <returns>Hex at exact position and not just overlapping the location</returns>
    public T? GetByExactElementRootPosition(Hex location)
    {
        int count = existingHexes.Count;

        // This uses a manual loop as this method is called a lot, so this needs to ensure that this doesn't do any
        // unnecessary computations
        for (int i = 0; i < count; ++i)
        {
            var existingHex = existingHexes[i];

            if (existingHex.Position == location)
                return existingHex;
        }

        return null;
    }

    /// <summary>
    ///   Removes hex that contains a hex position
    /// </summary>
    /// <returns>True when removed, false if there was nothing at this position</returns>
    public bool RemoveHexAt(Hex location, List<Hex> temporaryStorage)
    {
        var hex = GetElementAt(location, temporaryStorage);

        if (hex == null)
            return false;

        return Remove(hex);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var hex in existingHexes)
        {
            array[arrayIndex++] = hex;
        }
    }

    /// <summary>
    ///   Removes a previously placed hex
    /// </summary>
    public bool Remove(T hex)
    {
        if (!existingHexes.Contains(hex))
            return false;

        existingHexes.Remove(hex);
        onRemoved?.Invoke(hex);
        return true;
    }

    /// <summary>
    ///   Removes all existingHexes in this layout one by one
    /// </summary>
    public void Clear()
    {
        while (existingHexes.Count > 0)
        {
            Remove(existingHexes[existingHexes.Count - 1]);
        }
    }

    public bool Contains(T item)
    {
        return existingHexes.Contains(item);
    }

    public void Approve()
    {
        existingHexes.Commit(false);
    }

    public void Reject()
    {
        // This species will probably be discarded by auto-evo in this situation, so this returns the allocated
        // memory to the pool.
        existingHexes.Drop();
    }

    public HexLayoutView.Enumerator GetEnumerator()
    {
        return existingHexes.GetEnumerator();
    }

    // TODO: remove this bit of boxing here. https://nede.dev/blog/preventing-unnecessary-allocation-in-net-collections
    // Need to switch this from ICollection to just IEnumerable<T> (which hopefully doesn't break saving or can be
    // worked around with a custom converter) and directly return a list typed enumerator.
    [MustDisposeResource]
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return existingHexes.FallbackEnumerator();
    }

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return existingHexes.FallbackEnumerator();
    }

    /// <summary>
    ///   Loops though all hexes and checks if there are any without connection to the rest.
    /// </summary>
    /// <returns>Returns the number of island hexes</returns>
    public int GetIslandHexes(List<Hex> resultIslands, HashSet<Hex> workMemory1, List<Hex> workMemory2,
        Queue<Hex> workMemory3)
    {
#if DEBUG
        if (ReferenceEquals(resultIslands, workMemory2))
            throw new ArgumentException("Work memory is same as islands memory");
#endif

        resultIslands.Clear();

        if (Count < 1)
            return 0;

        // These are all the existing hexes, that if there are no islands will all be visited
        // This is calculated first to free up workMemory2 up for re-use
        ComputeHexCache(workMemory1, workMemory2);
        var shouldBeVisited = workMemory1;

        // The hex to start with
        var initHex = existingHexes[0].Position;

        // These are the hexes have neighbours and aren't islands
        var hexesWithNeighbours = workMemory2;
        hexesWithNeighbours.Clear();
        hexesWithNeighbours.Add(initHex);

        CheckmarkNeighbors(hexesWithNeighbours, workMemory1, workMemory3);

        // Calculate the difference of the lists (hexes that were not visited)
        foreach (var hex in shouldBeVisited)
        {
            if (!hexesWithNeighbours.Contains(hex))
                resultIslands.Add(hex);
        }

        return resultIslands.Count;
    }

    /// <summary>
    ///   Computes all the hex positions
    /// </summary>
    /// <returns>The set of hex positions</returns>
    public HashSet<Hex> ComputeHexCache()
    {
        var set = new HashSet<Hex>();
        var temporaryMemory = new List<Hex>();

        ComputeHexCache(set, temporaryMemory);

        return set;
    }

    /// <summary>
    ///   Computes all the hex positions (with existing memory allocations)
    /// </summary>
    /// <param name="result">Results are placed here, will be cleared before use</param>
    /// <param name="workMemory">Used as scratch memory, will be cleared before use</param>
    public void ComputeHexCache(HashSet<Hex> result, List<Hex> workMemory)
    {
        result.Clear();

        foreach (var hex in existingHexes)
        {
            GetHexComponentPositions(hex, workMemory);
            int count = workMemory.Count;

            for (int i = 0; i < count; ++i)
            {
                result.Add(hex.Position + workMemory[i]);
            }
        }
    }

    protected abstract void GetHexComponentPositions(T hex, List<Hex> result);

    /// <summary>
    ///   Adds the neighbors of the element in checked to checked, as well as their neighbors, and so on
    /// </summary>
    /// <param name="checked">The list of already visited hexes. Will be filled up with found hexes.</param>
    /// <param name="hexCache">All computed hex positions, generated by <see cref="ComputeHexCache()"/></param>
    /// <param name="workMemory">Temporary work memory needed by this method, will be cleared</param>
    private void CheckmarkNeighbors(List<Hex> @checked, HashSet<Hex> hexCache, Queue<Hex> workMemory)
    {
        workMemory.Clear();

        foreach (var hex in @checked)
        {
            workMemory.Enqueue(hex);
        }

        while (workMemory.Count > 0)
        {
            GetNeighborHexes(workMemory.Dequeue(), hexCache, @checked, workMemory);
        }
    }

    /// <summary>
    ///   Gets all neighboring hexes where there is a hex
    /// </summary>
    /// <param name="hex">The hex to get the neighbours for</param>
    /// <param name="hexCache">The cache of all existing hex locations for fast lookup</param>
    /// <param name="result">
    ///   Result is placed in here as a list of neighbors that are part of a hex, not cleared before writing to it
    /// </param>
    /// <param name="newlyFound">Returns newly added items to <see cref="result"/></param>
    private void GetNeighborHexes(Hex hex, HashSet<Hex> hexCache, List<Hex> result, Queue<Hex> newlyFound)
    {
        foreach (var pair in Hex.HexNeighbourOffset)
        {
            var hexToCheck = hex + pair.Value;

            if (hexCache.Contains(hexToCheck))
            {
                if (!result.Contains(hexToCheck))
                {
                    result.Add(hexToCheck);
                    newlyFound.Enqueue(hexToCheck);
                }
            }
        }
    }

    public struct HexLayoutView : IReadOnlyList<T>
    {
        internal List<T> MainHexes;
        internal bool Shared;

        private const int MEMORY_ALLOC_SIZE = 16;

        private T?[]? diffHexes;
        private int diffIndex = 0;

        public HexLayoutView(List<T> parent, bool shared, bool initDiffHexes = false)
        {
            MainHexes = parent;
            Shared = shared;

            if (initDiffHexes)
                diffHexes = ArrayPool<T?>.Shared.Rent(MEMORY_ALLOC_SIZE);
        }

        public int Count => MainHexes.Count + diffIndex;

        public T this[int index]
        {
            get
            {
                if (index < MainHexes.Count)
                    return MainHexes[index];

                if (diffHexes != null)
                {
                    int pendingIndex = index - MainHexes.Count;
                    if (pendingIndex < diffIndex)
                    {
                        var item = diffHexes[pendingIndex];

                        return item ?? throw new ArgumentOutOfRangeException(nameof(index), "Out of range.");
                    }
                }

                throw new ArgumentOutOfRangeException(nameof(index), "Out of range.");
            }
            set
            {
                if (index < MainHexes.Count)
                {
                    MainHexes[index] = value;
                    return;
                }

                if (diffHexes != null)
                {
                    int pendingIndex = index - MainHexes.Count;
                    if (pendingIndex < diffIndex)
                    {
                        diffHexes[pendingIndex] = value;
                        return;
                    }
                }

                throw new ArgumentOutOfRangeException(nameof(index), "Out of range.");
            }
        }

        public void Add(T item)
        {
            if (diffHexes == null)
            {
                // No diff hexes, so we add directly to the layout.

                // This is a shared hex layout. Since we're diverging from it, we need to allocate a brand-new list.
                IsolateIfShared();

                MainHexes.Add(item);
                return;
            }

            if (diffIndex == diffHexes.Length)
            {
                ArrayPool<T?>.Shared.Resize(ref diffHexes, diffHexes.Length + MEMORY_ALLOC_SIZE);
            }

            diffHexes[diffIndex++] = item;
        }

        public void Remove(T item)
        {
            if (diffHexes != null)
            {
                for (int i = 0; i < diffIndex; ++i)
                {
                    if (!ReferenceEquals(item, diffHexes[i]))
                        continue;

                    diffHexes[i] = diffHexes[--diffIndex];
                    diffHexes[diffIndex] = null;
                    return;
                }
            }

            IsolateIfShared();

            MainHexes.Remove(item);
        }

        /// <summary>
        ///   This method clears the diff layout and loads the modifications into the permanent layout.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is the method that allocates memory. Only when this is called and the diff is not empty the
        ///     organelles get deep cloned.
        ///   </para>
        /// </remarks>
        /// <returns>true if this layout was in diff mode.</returns>
        public bool Commit(bool reallocate)
        {
            if (diffHexes is null || diffIndex == 0)
                return false;

            IsolateIfShared();

            for (int i = 0; i < diffIndex; ++i)
            {
                var diff = diffHexes[i];

                if (diff is null)
                    break;

                MainHexes.Add(diff);
            }

            if (reallocate)
            {
                Array.Clear(diffHexes, 0, diffIndex);
                diffIndex = 0;
            }
            else
            {
                Drop();
            }

            return true;
        }

        public void IsolateIfShared()
        {
            if (!Shared)
                return;

            var count = MainHexes.Count;
            var newList = new List<T>(count);

            for (int i = 0; i < count; ++i)
            {
                newList.Add((T)((ICloneable)MainHexes[i]).Clone());
            }

            MainHexes = newList;
            Shared = false;
        }

        public void Drop()
        {
            if (diffHexes is null)
                return;

            Array.Clear(diffHexes, 0, diffIndex);
            ArrayPool<T?>.Shared.Return(diffHexes, false);

            diffHexes = null;
            diffIndex = 0;
        }

        public ReadOnlyEnumerator GetReadOnlyEnumerator()
        {
            return new ReadOnlyEnumerator(GetEnumerator());
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(MainHexes, diffHexes, diffIndex);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return FallbackEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return FallbackEnumerator();
        }

        internal IEnumerator<T> FallbackEnumerator()
        {
            int count = Count;
            for (int i = 0; i < count; ++i)
            {
                yield return this[i];
            }
        }

        public ref struct Enumerator(List<T> mainHexes, T?[]? diffHexes, int diffIndex)
        {
            private readonly ReadOnlySpan<T> mainSpan = CollectionsMarshal.AsSpan(mainHexes);
            private readonly ReadOnlySpan<T?> diffSpan = diffHexes != null ?
                new ReadOnlySpan<T?>(diffHexes, 0, diffIndex) : default;
            private int index = -1;

            public T Current
            {
                get
                {
                    if (index < mainSpan.Length)
                        return mainSpan[index];

                    return diffSpan[index - mainSpan.Length]!;
                }
            }

            public bool MoveNext()
            {
                index++;
                return index < mainSpan.Length + diffSpan.Length;
            }
        }

        public ref struct ReadOnlyEnumerator(Enumerator baseEnumerator)
        {
            private Enumerator baseEnumerator = baseEnumerator;

            public IReadOnlyPositionedHex Current => baseEnumerator.Current;

            public bool MoveNext()
            {
                return baseEnumerator.MoveNext();
            }
        }
    }
}
