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

    [JsonConstructor]
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

    public override bool CanPlace(T hex, List<Hex> temporaryStorage, List<Hex> temporaryStorage2)
    {
        return CanPlace(hex, false, temporaryStorage);
    }

    public bool CanPlace(T organelle, bool allowCytoplasmOverlap, List<Hex> temporaryStorage)
    {
        return CanPlace(organelle.Definition, organelle.Position, organelle.Orientation, temporaryStorage,
            allowCytoplasmOverlap);
    }

    /// <summary>
    ///   Returns true if organelle can be placed at location
    /// </summary>
    public bool CanPlace(OrganelleDefinition organelleType, Hex position, int orientation,
        List<Hex> temporaryStorage, bool allowCytoplasmOverlap = false)
    {
        // Check for overlapping hexes with existing organelles
        var hexes = organelleType.GetRotatedHexes(orientation);
        int hexCount = hexes.Count;

        // Use an explicit loop to ensure no extra memory allocations as this method is called a ton
        for (int i = 0; i < hexCount; ++i)
        {
            var overlapping = GetElementAt(hexes[i] + position, temporaryStorage);
            if (overlapping != null && (allowCytoplasmOverlap == false ||
                    overlapping.Definition.InternalName != "cytoplasm"))
            {
                return false;
            }
        }

        // Basic placing doesn't have the restriction that the
        // organelle needs to touch an existing one
        return true;
    }

    public override bool CanPlaceAndIsTouching(T hex, List<Hex> temporaryStorage, List<Hex> temporaryStorage2)
    {
        return CanPlaceAndIsTouching(hex, false, temporaryStorage, temporaryStorage2, false);
    }

    public bool CanPlaceAndIsTouching(T organelle, bool allowCytoplasmOverlap, List<Hex> temporaryStorage,
        List<Hex> temporaryStorage2, bool allowReplacingLastCytoplasm = false)
    {
        if (!CanPlace(organelle, allowCytoplasmOverlap, temporaryStorage))
            return false;

        return IsTouchingExistingHex(organelle, temporaryStorage, temporaryStorage2) ||
            (allowReplacingLastCytoplasm && IsReplacingLast(organelle, temporaryStorage));
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

    /// <summary>
    ///   Searches in a spiral pattern for a valid place to put the new organelle. Result is stored as the position
    ///   of the given new organelle object. Note that this is probably too slow for very huge cells.
    /// </summary>
    public void FindValidPositionForNewOrganelle(T newOrganelle, int startQ, int startR, List<Hex> workData1,
        List<Hex> workData2)
    {
        int radius = 1;

        while (true)
        {
            // Moves into the ring of radius "radius" around the given starting point center the old organelle
            var radiusOffset = Hex.HexNeighbourOffset[Hex.HexSide.BottomLeft];
            startQ += radiusOffset.Q;
            startR += radiusOffset.R;

            // Iterates in the ring
            for (int side = 1; side <= 6; ++side)
            {
                var offset = Hex.HexNeighbourOffset[(Hex.HexSide)side];

                // Moves "radius" times into each direction
                for (int i = 1; i <= radius; ++i)
                {
                    startQ += offset.Q;
                    startR += offset.R;

                    // Checks every possible rotation value.
                    for (int j = 0; j <= 5; ++j)
                    {
                        newOrganelle.Position = new Hex(startQ, startR);

                        newOrganelle.Orientation = j;
                        if (CanPlace(newOrganelle, workData1, workData2))
                        {
                            AddFast(newOrganelle, workData1, workData2);
                            return;
                        }
                    }
                }
            }

            ++radius;
        }
    }

    protected override void GetHexComponentPositions(T hex, List<Hex> result)
    {
        result.Clear();

        result.AddRange(hex.Definition.GetRotatedHexes(hex.Orientation));
    }

    /// <summary>
    ///   Returns true if the specified organelle is replacing the last hex of cytoplasm.
    /// </summary>
    private bool IsReplacingLast(T organelle, List<Hex> temporaryStorage)
    {
        if (Count != 1)
            return false;

        var replacedOrganelle = GetElementAt(organelle.Position, temporaryStorage);

        if (replacedOrganelle != null && replacedOrganelle.Definition.InternalName == "cytoplasm")
            return true;

        return false;
    }
}
