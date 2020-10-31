using System;
using System.Collections;
using System.Collections.Generic;
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
        // Check for overlapping hexes with existing organelles
        foreach (var hex in organelle.Definition.GetRotatedHexes(organelle.Orientation))
        {
            var overlapping = GetOrganelleAt(hex + organelle.Position);
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
}
