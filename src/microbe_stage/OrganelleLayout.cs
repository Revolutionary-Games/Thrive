using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   A list of positioned organelles. Verifies that they don't overlap
/// </summary>
/// <remarks>
///   <para>
///   </para>
/// </remarks>
public class OrganelleLayout<T> : IEnumerable<T>
    where T : class, IPositionedOrganelle
{
    [JsonProperty]
    public readonly List<T> Organelles = new List<T>();

    private Action<T> onAdded;
    private Action<T> onRemoved;

    public OrganelleLayout(Action<T> onAdded = null, Action<T> onRemoved = null)
    {
        this.onAdded = onAdded;
        this.onRemoved = onRemoved;
    }

    /// <summary>
    ///   Number of contained organelles
    /// </summary>
    public int Count
    {
        get
        {
            return Organelles.Count;
        }
    }

    /// <summary>
    ///   Access organelle by index
    /// </summary>
    public T this[int index]
    {
        get
        {
            return Organelles[index];
        }
    }

    /// <summary>
    ///   Adds a new organelle to this layout. Throws if overlaps or can't be placed
    /// </summary>
    public void Add(T organelle)
    {
        if (!CanPlace(organelle))
            throw new ArgumentException("organelle can't be placed at this location");

        Organelles.Add(organelle);
        if (onAdded != null)
            onAdded(organelle);
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
    ///   hex touches one of the new hexes.
    /// </summary>
    public bool CanPlaceAndIsTouching(T organelle, bool allowCytoplasmOverlap = false)
    {
        if (!CanPlace(organelle, allowCytoplasmOverlap))
            return false;

        return IsTouchingExistingHex(organelle);
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

        return default(T);
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

    /// <summary>
    ///   Removes a previously placed organelle
    /// </summary>
    public bool Remove(T organelle)
    {
        if (!Organelles.Contains(organelle))
            return false;

        Organelles.Remove(organelle);
        if (onRemoved != null)
            onRemoved(organelle);
        return true;
    }

    /// <summary>
    ///   Removes all organelles in this layout one by one
    /// </summary>
    public void RemoveAll()
    {
        while (Organelles.Count > 0)
        {
            Remove(Organelles[Organelles.Count - 1]);
        }
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
