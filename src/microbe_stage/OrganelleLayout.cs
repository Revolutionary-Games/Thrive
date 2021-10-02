using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A list of positioned organelles. Verifies that they don't overlap
/// </summary>
[UseThriveSerializer]
#pragma warning disable CA1710 // intentional naming
public class OrganelleLayout<T> : ICollection<T>
    where T : class, IPositionedOrganelle
#pragma warning restore CA1710
{
    [JsonProperty]
    public readonly List<T> Organelles = new List<T>();

    [JsonProperty]
    private Action<T> onAdded;

    [JsonProperty]
    private Action<T> onRemoved;

    public OrganelleLayout(Action<T> onAdded, Action<T> onRemoved = null)
    {
        this.onAdded = onAdded;
        this.onRemoved = onRemoved;
    }

    public OrganelleLayout()
    {
    }

    /// <summary>
    ///   Number of contained organelles
    /// </summary>
    public int Count => Organelles.Count;

    public bool IsReadOnly => false;

    /// <summary>
    ///   The center of mass of the contained organelles.
    /// </summary>
    public Hex CenterOfMass
    {
        get
        {
            float totalMass = 0;
            Vector3 weightedSum = Vector3.Zero;
            foreach (var organelle in Organelles)
            {
                totalMass += organelle.Definition.Mass;
                weightedSum += Hex.AxialToCartesian(organelle.Position) * organelle.Definition.Mass;
            }

            return Hex.CartesianToAxial(weightedSum / totalMass);
        }
    }

    /// <summary>
    ///   Access organelle by index
    /// </summary>
    public T this[int index] => Organelles[index];

    /// <summary>
    ///   Access organelle by hex
    /// </summary>
    public T this[Hex hex] => Organelles.FirstOrDefault(p => p.Position == hex);

    /// <summary>
    ///   Adds a new organelle to this layout. Throws if overlaps or can't be placed
    /// </summary>
    public void Add(T organelle)
    {
        if (!CanPlace(organelle))
            throw new ArgumentException("organelle can't be placed at this location");

        Organelles.Add(organelle);
        onAdded?.Invoke(organelle);
    }

    /// <summary>
    ///   Returns true if organelle can be placed at location
    /// </summary>
    public bool CanPlace(T organelle, bool allowCytoplasmOverlap = false)
    {
        return CanPlace(organelle.Definition, organelle.Position, organelle.Orientation, allowCytoplasmOverlap);
    }

    /// <summary>
    ///   Returns true if organelle can be placed at location
    /// </summary>
    public bool CanPlace(OrganelleDefinition organelleType, Hex position, int orientation,
        bool allowCytoplasmOverlap = false)
    {
        // Check for overlapping hexes with existing organelles
        foreach (var hex in organelleType.GetRotatedHexes(orientation))
        {
            var overlapping = GetOrganelleAt(hex + position);
            if (overlapping != null && (allowCytoplasmOverlap == false ||
                overlapping.Definition.InternalName != "cytoplasm"))
                return false;
        }

        // Basic placing doesn't have the restriction that the
        // organelle needs to touch an existing one
        return true;
    }

    /// <summary>
    ///   Returns true if CanPlace would return true and an existing
    ///   hex touches one of the new hexes, or is the last hex and can be replaced.
    /// </summary>
    public bool CanPlaceAndIsTouching(T organelle,
        bool allowCytoplasmOverlap = false,
        bool allowReplacingLastCytoplasm = false)
    {
        if (!CanPlace(organelle, allowCytoplasmOverlap))
            return false;

        return IsTouchingExistingHex(organelle) || (allowReplacingLastCytoplasm && IsReplacingLast(organelle));
    }

    /// <summary>
    ///   Returns true if the specified organelle is touching an already added hex
    /// </summary>
    public bool IsTouchingExistingHex(T organelle)
    {
        foreach (var hex in organelle.Definition.GetRotatedHexes(organelle.Orientation))
        {
            if (CheckIfAHexIsNextTo(hex + organelle.Position))
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Returns true if the specified organelle is replacing the last hex of cytoplasm.
    /// </summary>
    public bool IsReplacingLast(T organelle)
    {
        if (Count != 1)
            return false;

        var replacedOrganelle = GetOrganelleAt(organelle.Position);

        if ((replacedOrganelle != null) && (replacedOrganelle.Definition.InternalName == "cytoplasm"))
            return true;

        return false;
    }

    /// <summary>
    ///   Returns true if there is some placed organelle that has a
    ///   hex next to the specified location.
    /// </summary>
    public bool CheckIfAHexIsNextTo(Hex location)
    {
        return
            GetOrganelleAt(location + new Hex(0, -1)) != null ||
            GetOrganelleAt(location + new Hex(1, -1)) != null ||
            GetOrganelleAt(location + new Hex(1, 0)) != null ||
            GetOrganelleAt(location + new Hex(0, 1)) != null ||
            GetOrganelleAt(location + new Hex(-1, 1)) != null ||
            GetOrganelleAt(location + new Hex(-1, 0)) != null;
    }

    /// <summary>
    ///   Searches organelle list for an organelle at the specified hex
    /// </summary>
    public T GetOrganelleAt(Hex location)
    {
        foreach (var organelle in Organelles)
        {
            var relative = location - organelle.Position;
            foreach (var hex in organelle.Definition.GetRotatedHexes(organelle.Orientation))
            {
                if (hex.Equals(relative))
                {
                    return organelle;
                }
            }
        }

        return default;
    }

    /// <summary>
    ///   Removes organelle that contains hex
    /// </summary>
    /// <returns>True when removed, false if there was nothing at this position</returns>
    public bool RemoveOrganelleAt(Hex location)
    {
        var organelle = GetOrganelleAt(location);

        if (organelle == null)
            return false;

        return Remove(organelle);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var organelle in this)
        {
            array[arrayIndex++] = organelle;
        }
    }

    /// <summary>
    ///   Removes a previously placed organelle
    /// </summary>
    public bool Remove(T organelle)
    {
        if (!Organelles.Contains(organelle))
            return false;

        Organelles.Remove(organelle);
        onRemoved?.Invoke(organelle);
        return true;
    }

    /// <summary>
    ///   Removes all organelles in this layout one by one
    /// </summary>
    public void Clear()
    {
        while (Organelles.Count > 0)
        {
            Remove(Organelles[Organelles.Count - 1]);
        }
    }

    public bool Contains(T item)
    {
        return Organelles.Contains(item);
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Organelles.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Organelles.GetEnumerator();
    }

    /// <summary>
    ///   Loops though all hexes and checks if there any without connection to the rest.
    /// </summary>
    /// <returns>
    ///   Returns a list of hexes that are not connected to the rest
    /// </returns>
    internal List<Hex> GetIslandHexes()
    {
        if (Organelles.Count < 1)
            return new List<Hex>();

        // The hex to start with
        var initHex = Organelles[0].Position;

        // These are the hexes have neighbours and aren't islands
        var hexesWithNeighbours = new List<Hex> { initHex };

        // These are all of the existing hexes, that if there are no islands will all be visited
        var shouldBeVisited = Organelles.Select(p => p.Position).ToList();

        CheckmarkNeighbors(hexesWithNeighbours);

        // Return the difference of the lists (hexes that were not visited)
        return shouldBeVisited.Except(hexesWithNeighbours).ToList();
    }

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
    ///   Computes all the hex positions
    /// </summary>
    /// <returns>The set of hex positions</returns>
    private HashSet<Hex> ComputeHexCache()
    {
        var set = new HashSet<Hex>();

        foreach (var hex in Organelles.SelectMany(o =>
            o.Definition.GetRotatedHexes(o.Orientation).Select(h => h + o.Position)))
        {
            set.Add(hex);
        }

        return set;
    }

    /// <summary>
    ///   Gets all neighboring hexes where there is an organelle
    /// </summary>
    /// <param name="hex">The hex to get the neighbours for</param>
    /// <param name="hexCache">The cache of all existing hex locations for fast lookup</param>
    /// <returns>A list of neighbors that are part of an organelle</returns>
    private IEnumerable<Hex> GetNeighborHexes(Hex hex, HashSet<Hex> hexCache)
    {
        return Hex.HexNeighbourOffset
            .Select(p => hex + p.Value)
            .Where(hexCache.Contains);
    }

    /// <summary>
    ///   Gets all neighboring hexes where there is an organelle. This doesn't use a cache so for each potential
    ///   hex this recomputes the positions of all organelles because this uses GetOrganelleAt
    /// </summary>
    /// <param name="hex">The hex to get the neighbours for</param>
    /// <returns>A list of neighbors that are part of an organelle</returns>
    private IEnumerable<Hex> GetNeighborHexesSlow(Hex hex)
    {
        return Hex.HexNeighbourOffset
            .Select(p => hex + p.Value)
            .Where(p => GetOrganelleAt(p) != null);
    }
}
