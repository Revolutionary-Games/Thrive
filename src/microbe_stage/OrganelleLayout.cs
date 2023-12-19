using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A list of positioned organelles. Verifies that they don't overlap
/// </summary>
/// <typeparam name="T">The type of organelle contained in this layout</typeparam>
[UseThriveSerializer]
public class OrganelleLayout<T> : HexLayout<T>
    where T : class, IPositionedOrganelle
{
    public OrganelleLayout(Action<T> onAdded, Action<T>? onRemoved = null) : base(onAdded, onRemoved)
    {
    }

    public OrganelleLayout()
    {
    }

    [JsonIgnore]
    public IReadOnlyList<T> Organelles => existingHexes;

    [JsonIgnore]
    public int HexCount => existingHexes.Sum(h => h.Definition.HexCount);

    /// <summary>
    ///   The center of mass of the contained organelles.
    /// </summary>
    [JsonIgnore]
    public Hex CenterOfMass
    {
        get
        {
            // TODO: this used to weigh the center position based on the organelle masses, this is no longer possible
            // to do as simply
            // float totalMass = 0;
            int count = 0;
            Vector3 weightedSum = Vector3.Zero;

            // TODO: shouldn't this take multihex organelles into account?
            foreach (var organelle in Organelles)
            {
                // totalMass += organelle.Definition.Mass;
                ++count;
                weightedSum += Hex.AxialToCartesian(organelle.Position) /* * organelle.Definition.Mass*/;
            }

            if (count == 0)
                return new Hex(0, 0);

            return Hex.CartesianToAxial(weightedSum / count);
        }
    }

    public override bool CanPlace(T hex)
    {
        return CanPlace(hex, false);
    }

    public bool CanPlace(T organelle, bool allowCytoplasmOverlap)
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
            var overlapping = GetElementAt(hex + position);
            if (overlapping != null && (allowCytoplasmOverlap == false ||
                    overlapping.Definition.InternalName != "cytoplasm"))
                return false;
        }

        // Basic placing doesn't have the restriction that the
        // organelle needs to touch an existing one
        return true;
    }

    public override bool CanPlaceAndIsTouching(T hex)
    {
        return CanPlaceAndIsTouching(hex, false, false);
    }

    public bool CanPlaceAndIsTouching(T organelle,
        bool allowCytoplasmOverlap,
        bool allowReplacingLastCytoplasm = false)
    {
        if (!CanPlace(organelle, allowCytoplasmOverlap))
            return false;

        return IsTouchingExistingHex(organelle) || (allowReplacingLastCytoplasm && IsReplacingLast(organelle));
    }

    public bool RepositionToOrigin()
    {
        var centerOfMass = CenterOfMass;

        // Skip if center of mass is already correct
        if (centerOfMass.Q == 0 && centerOfMass.R == 0)
            return false;

        foreach (var organelle in Organelles)
        {
            // This calculation aligns the center of mass with the origin by moving every organelle of the microbe.
            organelle.Position -= centerOfMass;
        }

        return true;
    }

    protected override IEnumerable<Hex> GetHexComponentPositions(T hex)
    {
        return hex.Definition.GetRotatedHexes(hex.Orientation);
    }

    /// <summary>
    ///   Returns true if the specified organelle is replacing the last hex of cytoplasm.
    /// </summary>
    private bool IsReplacingLast(T organelle)
    {
        if (Count != 1)
            return false;

        var replacedOrganelle = GetElementAt(organelle.Position);

        if ((replacedOrganelle != null) && (replacedOrganelle.Definition.InternalName == "cytoplasm"))
            return true;

        return false;
    }
}
