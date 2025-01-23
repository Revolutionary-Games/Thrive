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
    where T : class, IPositionedOrganelle, ICloneable
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
            var organelleList = Organelles;
            var listLength = organelleList.Count;
            for (int i = 0; i < listLength; ++i)
            {
                // totalMass += organelle.Definition.Mass;
                ++count;
                weightedSum += Hex.AxialToCartesian(organelleList[i].Position) /* * organelle.Definition.Mass*/;
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
    ///   Checks if organelle can be placed at a location
    /// </summary>
    /// <returns>True if organelle can be placed at the position</returns>
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

        // Skip if the center of mass is already correct
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
    ///   Searches in a spiral pattern for a valid place to put the new organelle. The result is stored as the position
    ///   of the given new organelle object. Note that this is probably too slow for very huge cells.
    /// </summary>
    public void FindAndPlaceAtValidPosition(T newOrganelle, int startQ, int startR, List<Hex> workData1,
        List<Hex> workData2, HashSet<Hex> workData3)
    {
        // Compute hex cache just once to speed up this method a lot
        ComputeHexCache(workData3, workData1);

        var radiusOffset = Hex.HexNeighbourOffset[Hex.HexSide.BottomLeft];

        // Moves into the ring of radius "radius" around the given starting point center the old organelle
        for (int radius = 1; radius <= 1000; ++radius)
        {
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

                    if (TestOrganellePlacementPosition(newOrganelle, startQ, startR, workData3, workData1, workData2))
                    {
                        return;
                    }
                }
            }

            if (radius == 10)
                GD.Print("Organelle placement search is taking a really long time (radius > 9)");
        }

        GD.PrintErr("Abandoning an organelle as cannot find a position for it");
    }

    /// <summary>
    ///   Checks if the hexes an organelle would occupy are free (doesn't check for touching)
    /// </summary>
    /// <param name="organelleType">Type of organelle to place, used to know how many hexes it is big</param>
    /// <param name="q">Position to test</param>
    /// <param name="r">Position to test (r-coordinate)</param>
    /// <param name="rotation">Rotation to test</param>
    /// <param name="precalculatedHexCache">
    ///   Precalculated cache from <see cref="HexLayout{T}.ComputeHexCache(HashSet{Hex},List{Hex})"/>
    /// </param>
    /// <param name="primaryHexWasFree">
    ///   Returns true if the first position checked was free irrespective of if this returns true or not
    /// </param>
    /// <returns>True if all hex positions the organelle would occupy are free</returns>
    public bool CheckIsOrganellePlacementFree(OrganelleDefinition organelleType, int q, int r, int rotation,
        HashSet<Hex> precalculatedHexCache, out bool primaryHexWasFree)
    {
        var primaryPosition = new Hex(q, r);

        // Check for overlapping hexes with existing organelles
        var hexes = organelleType.GetRotatedHexes(rotation);
        int hexCount = hexes.Count;

        // Use an explicit loop to ensure no extra memory allocations as this method is called a ton
        for (int i = 0; i < hexCount; ++i)
        {
            if (precalculatedHexCache.Contains(hexes[i] + primaryPosition))
            {
                // We know the primary hex check succeeded if "i" is above zero
                primaryHexWasFree = i > 0;

                return false;
            }
        }

        primaryHexWasFree = true;

        // Basic placing doesn't have the restriction that the organelle needs to touch an existing one
        return true;
    }

    /// <summary>
    ///   Deep clones this organelle layout as a new layout in a more efficient way than copying organelles from here
    ///   to a new instance
    /// </summary>
    /// <returns>Cloned instance with deep copied organelle instances</returns>
    public OrganelleLayout<T> Clone()
    {
        var result = new OrganelleLayout<T>();

        foreach (var existingHex in existingHexes)
        {
            result.existingHexes.Add((T)existingHex.Clone());
        }

        return result;
    }

    protected override void GetHexComponentPositions(T hex, List<Hex> result)
    {
        result.Clear();

        var rotated = hex.Definition.GetRotatedHexes(hex.Orientation);
        var count = rotated.Count;

        for (int i = 0; i < count; ++i)
        {
            result.Add(rotated[i]);
        }
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

    private bool TestOrganellePlacementPosition(T newOrganelle, int q, int r, HashSet<Hex> precalculatedHexCache,
        List<Hex> workData1, List<Hex> workData2)
    {
        bool isSingleHex = newOrganelle.Definition.Hexes.Count < 2;

        // Checks every possible rotation value at the position
        for (int j = 0; j <= 5; ++j)
        {
            if (CheckIsOrganellePlacementFree(newOrganelle.Definition, q, r, j, precalculatedHexCache,
                    out var wasPrimaryFree))
            {
                // Found a valid position, can end the method here
                newOrganelle.Position = new Hex(q, r);
                newOrganelle.Orientation = j;
                AddFast(newOrganelle, workData1, workData2);
                return true;
            }

            if (j == 0)
            {
                if (!wasPrimaryFree)
                {
                    // When failing the initial position check, that means that no matter how this is rotated, the hole
                    // will always remain, so fail early here
                    break;
                }
            }

            // Single hex cannot be made to fit by rotating
            if (isSingleHex)
                break;
        }

        return false;
    }
}
