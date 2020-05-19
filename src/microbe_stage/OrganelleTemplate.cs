using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Basically just adding the
///   positioning info on top of OrganelleDefinition when the layout
///   is instantiated in a cell, PlacedOrganelle class is used.
/// </summary>
[UseThriveSerializer]
public class OrganelleTemplate : IPositionedOrganelle, ICloneable
{
    [JsonProperty]
    public readonly OrganelleDefinition Definition;

    public OrganelleTemplate(OrganelleDefinition definition, Hex location, int rotation)
    {
        Definition = definition;
        Position = location;
        Orientation = rotation;
    }

    public Hex Position { get; set; }

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    public int Orientation { get; set; } = 0;

#pragma warning disable CA1033
    OrganelleDefinition IPositionedOrganelle.Definition
    {
        get
        {
            return Definition;
        }
    }
#pragma warning restore CA1033

    [JsonIgnore]
    public IEnumerable<Hex> RotatedHexes
    {
        get
        {
            return Definition.GetRotatedHexes(Orientation);
        }
    }

    public object Clone()
    {
        return new OrganelleTemplate(Definition, Position, Orientation);
    }
}
