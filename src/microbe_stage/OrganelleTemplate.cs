using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Basically just adding the
///   positioning info on top of OrganelleDefinition when the layout
///   is instantiated in a cell, PlacedOrganelle class is used.
/// </summary>
[JsonObject(IsReference = true)]
public class OrganelleTemplate : IPositionedOrganelle, ICloneable
{
    [JsonProperty]
    public readonly OrganelleDefinition Definition;

    public OrganelleTemplate(OrganelleDefinition definition, Hex location, int rotation, int numberOfTimesMoved = 0)
    {
        Definition = definition;
        Position = location;
        Orientation = rotation;
        NumberOfTimesMoved = numberOfTimesMoved;
    }

    /// <summary>
    /// Used to flag whether this Organelle was placed during the current editor session.
    /// </summary>
    public bool PlacedThisSession { get; set; }

    public int NumberOfTimesMoved { get; set; }

    /// <summary>
    ///   Used to flag whether this Organelle was moved during the current editor session.
    /// </summary>
    public bool MovedThisSession => NumberOfTimesMoved > 0;

    public Hex Position { get; set; }

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    ///   The upgrades this organelle will have when instantiated in a microbe
    /// </summary>
    public OrganelleUpgrades? Upgrades { get; set; }

#pragma warning disable CA1033
    OrganelleDefinition IPositionedOrganelle.Definition => Definition;
#pragma warning restore CA1033

    [JsonIgnore]
    public IEnumerable<Hex> RotatedHexes => Definition.GetRotatedHexes(Orientation);

    public void SetCustomUpgradeObject(IComponentSpecificUpgrades upgrades)
    {
        Upgrades ??= new OrganelleUpgrades();
        Upgrades.CustomUpgradeData = upgrades;
    }

    public object Clone()
    {
        return new OrganelleTemplate(Definition, Position, Orientation, NumberOfTimesMoved)
        {
            Upgrades = (OrganelleUpgrades?)Upgrades?.Clone(),
        };
    }

    public override int GetHashCode()
    {
        return (Position.GetHashCode() * 131) ^ (Orientation * 2909) ^ (Definition.GetHashCode() * 947) ^
            ((Upgrades != null ? Upgrades.GetHashCode() : 1) * 1063);
    }
}
