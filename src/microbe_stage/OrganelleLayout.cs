using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   A list of positioned organelles. Verifies that they don't overlap
/// </summary>
public class OrganelleLayout
{
    [JsonProperty]
    public readonly List<OrganelleTemplate> Organelles = new List<OrganelleTemplate>();

    public OrganelleLayout()
    {
    }

    /// <summary>
    ///   Adds a new organelle to this layout. Throws if overlaps or can't be placed
    /// </summary>
    public void Add(Hex location, int rotation, OrganelleDefinition organelle)
    {
        if (!CanPlace(location, rotation, organelle))
            throw new ArgumentException("organelle can't be placed at this location");

        var placed = new OrganelleTemplate(organelle, location, rotation);

        Organelles.Add(placed);
    }

    /// <summary>
    ///   Returns true if organelle can be placed at location
    /// </summary>
    public bool CanPlace(Hex location, int rotation, OrganelleDefinition organelle)
    {
        // TODO: implement
        return true;
    }

    /// <summary>
    ///   Removes a previously placed organelle
    /// </summary>
    public bool Remove(OrganelleTemplate organelle)
    {
        if (!Organelles.Contains(organelle))
            return false;

        Organelles.Remove(organelle);
        return false;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
