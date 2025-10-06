using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Basically just adding the positioning info on top of OrganelleDefinition.
///   When the layout is instantiated in a cell, the PlacedOrganelle class is used.
/// </summary>
[JsonObject(IsReference = true)]
[JSONDynamicTypeAllowed]
public class OrganelleTemplate : IPositionedOrganelle, ICloneable, IActionHex, IPlayerReadableName
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

    [JsonIgnore]
    public Vector3 OrganelleModelPosition => Hex.AxialToCartesian(Position) + Definition.ModelOffset;

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    ///   The upgrades this organelle will have when instantiated in a microbe
    /// </summary>
    public OrganelleUpgrades? Upgrades { get; set; }

    [JsonIgnore]
    public string ReadableName => Definition.Name;

    [JsonIgnore]
    public string ReadableExactIdentifier => Localization.Translate("ITEM_AT_2D_COORDINATES")
        .FormatSafe(Definition.Name, Position.Q, Position.R);

    [JsonIgnore]
    public IReadOnlyList<Hex> RotatedHexes => Definition.GetRotatedHexes(Orientation);

#pragma warning disable CA1033
    OrganelleDefinition IPositionedOrganelle.Definition => Definition;
#pragma warning restore CA1033

    public bool MatchesDefinition(IActionHex other)
    {
        return Definition == ((OrganelleTemplate)other).Definition;
    }

    /// <summary>
    ///   Calculates the actual active enzymes for this organelle.
    /// </summary>
    /// <param name="result">Puts the results here. Note that this doesn't clear any existing data.</param>
    /// <returns>True if organelle has enzymes (and thus the result was modified)</returns>
    public bool GetActiveEnzymes(Dictionary<Enzyme, int> result)
    {
        if (Definition.HasLysosomeComponent)
        {
            LysosomeComponent.CalculateLysosomeActiveEnzymes(Upgrades?.CustomUpgradeData as LysosomeUpgrades, result);
            return true;
        }

        // No other organelles are known to set up their active enzymes
        return false;
    }

    public object Clone()
    {
        return new OrganelleTemplate(Definition, Position, Orientation)
        {
            Upgrades = (OrganelleUpgrades?)Upgrades?.Clone(),
        };
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode() * 131 ^ Orientation * 2909 ^ Definition.GetHashCode() * 947 ^
            (Upgrades != null ? Upgrades.GetHashCode() : 1) * 1063;
    }

    public ulong GetVisualHashCode()
    {
        return (ulong)Position.GetHashCode() * 131 ^ (ulong)Orientation * 2909 ^ Definition.GetVisualHashCode() * 947 ^
            (Upgrades != null ? Upgrades.GetVisualHashCode() : 1) * 1063;
    }
}
