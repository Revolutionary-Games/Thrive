using System;
using Newtonsoft.Json;

/// <summary>
///   This class is part of OrganelleLayout. Basically just adding the
///   positioning info on top of OrganelleDefinition when the layout
///   is instantiated in a cell, PlacedOrganelle class is used.
/// </summary>
[JsonConverter(typeof(PlacedOrganelleConverter))]
public class OrganelleTemplate
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
}
