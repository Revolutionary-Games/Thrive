using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Base class implementing the basic structure for holding layouts composed of hexes (for example microbe's
///   organelles)
/// </summary>
/// <typeparam name="T">The concrete type of the hex to hold</typeparam>
[UseThriveSerializer]
public abstract class HexLayout<T> : ICollection<T>
    where T : class, IPositionedHex
{
    [JsonProperty]
    protected readonly List<T> existingHexes = new();

    // This and the next property are protected to make JSON work
    [JsonProperty]
    protected Action<T>? onAdded;

    [JsonProperty]
    protected Action<T>? onRemoved;

    public HexLayout(Action<T>? onAdded, Action<T>? onRemoved = null)
    {
        this.onAdded = onAdded;
        this.onRemoved = onRemoved;
    }

    public HexLayout()
    {
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
    ///   Adds a new hex-based element to this layout. Throws if overlaps or can't be placed
    /// </summary>
    public void Add(T hex)
    {
        if (!CanPlace(hex))
            throw new ArgumentException($"{typeof(T).Name} can't be placed at this location");

        existingHexes.Add(hex);
        onAdded?.Invoke(hex);
    }

    /// <summary>
    ///   Returns true if hex can be placed at location. Only checks that the location doesn't overlap with any
    ///   existing hexes
    /// </summary>
    public virtual bool CanPlace(T hex)
    {
        var position = hex.Position;

        // Check for overlapping hexes with existing hexes
        foreach (var newHex in GetHexComponentPositions(hex))
        {
            if (GetElementAt(newHex + position) != null)
                return false;
        }

        return true;
    }

    /// <summary>
    ///   Returns true if CanPlace would return true and an existing
    ///   hex touches one of the new hexes, or is the last hex and can be replaced.
    /// </summary>
    public virtual bool CanPlaceAndIsTouching(T hex)
    {
        if (!CanPlace(hex))
            return false;

        return IsTouchingExistingHex(hex);
    }

    /// <summary>
    ///   Returns true if the specified hex is touching an already added hex
    /// </summary>
    public bool IsTouchingExistingHex(T hex)
    {
        foreach (var hexComponentPart in GetHexComponentPositions(hex))
        {
            if (CheckIfAHexIsNextTo(hexComponentPart + hex.Position))
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Returns true if there is some placed hex that has a
    ///   hex next to the specified location.
    /// </summary>
    public bool CheckIfAHexIsNextTo(Hex location)
    {
        return
            GetElementAt(location + new Hex(0, -1)) != null ||
            GetElementAt(location + new Hex(1, -1)) != null ||
            GetElementAt(location + new Hex(1, 0)) != null ||
            GetElementAt(location + new Hex(0, 1)) != null ||
            GetElementAt(location + new Hex(-1, 1)) != null ||
            GetElementAt(location + new Hex(-1, 0)) != null;
    }

    /// <summary>
    ///   Searches hex list for an hex at the specified hex
    /// </summary>
    public T? GetElementAt(Hex location)
    {
        foreach (var existingHex in existingHexes)
        {
            var relative = location - existingHex.Position;
            foreach (var hex in GetHexComponentPositions(existingHex))
            {
                if (hex.Equals(relative))
                {
                    return existingHex;
                }
            }
        }

        return default;
    }

    /// <summary>
    ///   Removes hex that contains hex
    /// </summary>
    /// <returns>True when removed, false if there was nothing at this position</returns>
    public bool RemoveHexAt(Hex location)
    {
        var hex = GetElementAt(location);

        if (hex == null)
            return false;

        return Remove(hex);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var hex in this)
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

    public IEnumerator<T> GetEnumerator()
    {
        return existingHexes.GetEnumerator();
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return existingHexes.GetEnumerator();
    }

    /// <summary>
    ///   Loops though all hexes and checks if there any without connection to the rest.
    /// </summary>
    /// <returns>
    ///   Returns a list of hexes that are not connected to the rest
    /// </returns>
    public List<Hex> GetIslandHexes()
    {
        if (Count < 1)
            return new List<Hex>();

        // The hex to start with
        var initHex = existingHexes[0].Position;

        // These are the hexes have neighbours and aren't islands
        var hexesWithNeighbours = new List<Hex> { initHex };

        // These are all of the existing hexes, that if there are no islands will all be visited
        var shouldBeVisited = ComputeHexCache();

        CheckmarkNeighbors(hexesWithNeighbours);

        // Return the difference of the lists (hexes that were not visited)
        return shouldBeVisited.Except(hexesWithNeighbours).ToList();
    }

    /// <summary>
    ///   Computes all the hex positions
    /// </summary>
    /// <returns>The set of hex positions</returns>
    public HashSet<Hex> ComputeHexCache()
    {
        var set = new HashSet<Hex>();

        foreach (var hex in existingHexes.SelectMany(o => GetHexComponentPositions(o).Select(h => h + o.Position)))
        {
            set.Add(hex);
        }

        return set;
    }

    protected abstract IEnumerable<Hex> GetHexComponentPositions(T hex);

    /// <summary>
    ///   Adds the neighbors of the element in checked to checked, as well as their neighbors, and so on
    /// </summary>
    /// <param name="checked">The list of already visited hexes. Will be filled up with found hexes.</param>
    private void CheckmarkNeighbors(List<Hex> @checked)
    {
        var hexCache = ComputeHexCache();

        var queue = new Queue<Hex>(@checked);

        while (queue.Count > 0)
        {
            var neighbors = GetNeighborHexes(queue.Dequeue(), hexCache).Where(p => !@checked.Contains(p));

            foreach (var neighbor in neighbors)
            {
                queue.Enqueue(neighbor);
                @checked.Add(neighbor);
            }
        }
    }

    /// <summary>
    ///   Gets all neighboring hexes where there is an hex
    /// </summary>
    /// <param name="hex">The hex to get the neighbours for</param>
    /// <param name="hexCache">The cache of all existing hex locations for fast lookup</param>
    /// <returns>A list of neighbors that are part of an hex</returns>
    private IEnumerable<Hex> GetNeighborHexes(Hex hex, HashSet<Hex> hexCache)
    {
        return Hex.HexNeighbourOffset
            .Select(p => hex + p.Value)
            .Where(hexCache.Contains);
    }

    /// <summary>
    ///   Gets all neighboring hexes where there is an hex. This doesn't use a cache so for each potential
    ///   hex this recomputes the positions of all existingHexes because this uses GetElementAt
    /// </summary>
    /// <param name="hex">The hex to get the neighbours for</param>
    /// <returns>A list of neighbors that are part of an hex</returns>
    private IEnumerable<Hex> GetNeighborHexesSlow(Hex hex)
    {
        return Hex.HexNeighbourOffset
            .Select(p => hex + p.Value)
            .Where(p => GetElementAt(p) != null);
    }
}
