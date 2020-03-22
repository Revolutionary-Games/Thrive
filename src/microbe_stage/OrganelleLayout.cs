using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   A list of positioned organelles. Verifies that they don't overlap
/// </summary>
public class OrganelleLayout<T>
    where T : IPositionedOrganelle
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
    public bool CanPlace(T organelle)
    {
        // TODO: implement by checking hexes not overlapping
        return true;
    }

    // TODO: implement a method to check if can place and is touching
    // an existing hex for use by the editor

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
        return false;
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
}
