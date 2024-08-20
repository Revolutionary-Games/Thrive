using System;
using System.Collections.Generic;
using System.Linq;

public static class CommonMutationFunctions
{
    public static OrganelleDefinition Nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

    // These must be defined this way to avoid allocations
    private static readonly Hex.HexSide[] TraversalOrder1 = [Hex.HexSide.Top];
    private static readonly Hex.HexSide[] TraversalOrder2 = [Hex.HexSide.Bottom];

    private static readonly Hex.HexSide[] TraversalOrder3 =
    [
        Hex.HexSide.TopRight, Hex.HexSide.BottomRight,
        Hex.HexSide.Top, Hex.HexSide.Bottom, Hex.HexSide.BottomLeft, Hex.HexSide.TopLeft,
    ];

    private static readonly Hex.HexSide[] TraversalOrder4 =
    [
        Hex.HexSide.BottomRight, Hex.HexSide.TopRight,
        Hex.HexSide.Top, Hex.HexSide.BottomLeft, Hex.HexSide.Bottom, Hex.HexSide.TopLeft,
    ];

    private static readonly Hex.HexSide[] TraversalOrder5 =
    [
        Hex.HexSide.BottomLeft, Hex.HexSide.TopLeft,
        Hex.HexSide.Bottom, Hex.HexSide.Top, Hex.HexSide.TopRight, Hex.HexSide.BottomRight,
    ];

    private static readonly Hex.HexSide[] TraversalOrder6 =
    [
        Hex.HexSide.TopLeft, Hex.HexSide.BottomLeft,
        Hex.HexSide.Bottom, Hex.HexSide.Top, Hex.HexSide.BottomRight, Hex.HexSide.TopRight,
    ];

    private static readonly Hex.HexSide[] TraversalOrder7 =
    [
        Hex.HexSide.Top, Hex.HexSide.TopLeft, Hex.HexSide.TopRight,
        Hex.HexSide.BottomLeft, Hex.HexSide.BottomRight, Hex.HexSide.Bottom,
    ];

    private static readonly Hex.HexSide[] TraversalOrder8 =
    [
        Hex.HexSide.Top, Hex.HexSide.TopRight, Hex.HexSide.TopLeft,
        Hex.HexSide.BottomRight, Hex.HexSide.BottomLeft, Hex.HexSide.Bottom,
    ];

    public enum Direction
    {
        Front,
        Neutral,
        Rear,
    }

    public static void AddOrganelle(OrganelleDefinition organelle, Direction direction, MicrobeSpecies newSpecies,
        List<Hex> workMemory1, List<Hex> workMemory2, Random random)
    {
        var position = GetRealisticPosition(organelle, newSpecies.Organelles, direction, workMemory1, workMemory2,
            random);

        // We return early as not being able to add an organelle is not a critical failure
        if (position == null)
            return;

        newSpecies.Organelles.Add(position);

        // If the new species is a eukaryote, mark this as such.
        if (organelle == Nucleus)
        {
            newSpecies.IsBacteria = false;
        }
    }

    public static void AttachIslandHexes(OrganelleLayout<OrganelleTemplate> organelles, MutationWorkMemory workMemory)
    {
        var workMemory1 = new HashSet<Hex>();
        var workMemory2 = new List<Hex>();
        var workMemory3 = new Queue<Hex>();

        var islandHexes = new List<Hex>();
        var mainHexes = new HashSet<Hex>();

        organelles.GetIslandHexes(islandHexes, workMemory1, workMemory2, workMemory3);

        // Attach islands
        while (islandHexes.Count > 0)
        {
            organelles.ComputeHexCache(mainHexes, workMemory2);

            // Compute shortest hex distance
            Hex minSubHex = default;
            int minDistance = int.MaxValue;
            foreach (var mainHex in mainHexes.Except(islandHexes))
            {
                foreach (var islandHex in islandHexes)
                {
                    var sub = islandHex - mainHex;
                    int distance = (Math.Abs(sub.Q) + Math.Abs(sub.Q + sub.R) + Math.Abs(sub.R)) / 2;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minSubHex = sub;

                        // early exit if minDistance == 2 (distance 1 == direct neighbour => not an island)
                        if (minDistance == 2)
                            break;
                    }
                }

                // early exit if minDistance == 2 (distance 1 == direct neighbour => not an island)
                if (minDistance == 2)
                    break;
            }

            // Calculate the path to move island organelles.
            // If statement is there because otherwise the path could be (0, 0).
            if (minSubHex.Q != minSubHex.R)
                minSubHex.Q = (int)(minSubHex.Q * (minDistance - 1.0) / minDistance);

            minSubHex.R = (int)(minSubHex.R * (minDistance - 1.0) / minDistance);

            // Move all island organelles by minSubHex
            foreach (var organelle in organelles)
            {
                foreach (var islandHex in islandHexes)
                {
                    if (organelle.Definition.GetRotatedHexes(organelle.Orientation)
                        .Contains(islandHex - organelle.Position))
                    {
                        organelle.Position -= minSubHex;
                        break;
                    }
                }
            }

            organelles.GetIslandHexes(islandHexes, workMemory1, workMemory2, workMemory3);
        }
    }

    private static OrganelleTemplate? GetRealisticPosition(OrganelleDefinition organelle,
        OrganelleLayout<OrganelleTemplate> existingOrganelles, Direction direction, List<Hex> workMemory1,
        List<Hex> workMemory2, Random random)
    {
        var result = new OrganelleTemplate(organelle, new Hex(0, 0), 0);

        // Loop through all the organelles and find an open spot to
        // place our new organelle attached to existing organelles
        // This almost always is over at the first iteration, so it's
        // not a huge performance hog
        foreach (var otherOrganelle in existingOrganelles.OrderBy(_ => random.Next()))
        {
            // The otherOrganelle is the organelle we wish to be next to
            // Loop its hexes and check positions next to them
            foreach (var hex in otherOrganelle.RotatedHexes)
            {
                // Offset by hexes in organelle we are looking at
                var pos = otherOrganelle.Position + hex;

                foreach (int side in SideTraversalOrder(hex, direction, random))
                {
                    // pick a hex direction, with a slight bias towards forwards
                    for (int radius = 1; radius <= 3; ++radius)
                    {
                        // Offset by hex offset multiplied by a factor to check for greater range
                        var hexOffset = Hex.HexNeighbourOffset[(Hex.HexSide)side];
                        hexOffset *= radius;
                        result.Position = pos + hexOffset;

                        if (organelle.HasMovementComponent)
                        {
                            // Face movement to move forward
                            result.Orientation = 3;

                            if (existingOrganelles
                                .CanPlace(result, workMemory1, workMemory2))
                            {
                                return result;
                            }
                        }

                        // Check every possible rotation value.
                        for (int rotation = 0; rotation <= 5; ++rotation)
                        {
                            result.Orientation = rotation;

                            if (existingOrganelles.CanPlace(result, workMemory1, workMemory2))
                            {
                                return result;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private static Hex.HexSide[] SideTraversalOrder(Hex hex, Direction direction, Random random)
    {
        if (hex.Q < 0)
        {
            if (direction == Direction.Front)
            {
                return TraversalOrder1;
            }

            if (direction == Direction.Rear)
            {
                return TraversalOrder2;
            }

            if (hex.R < 0)
            {
                return TraversalOrder3;
            }

            return TraversalOrder4;
        }

        if (hex.Q > 0)
        {
            if (direction == Direction.Front)
            {
                return TraversalOrder1;
            }

            if (direction == Direction.Rear)
            {
                return TraversalOrder2;
            }

            if (hex.R < 0)
            {
                return TraversalOrder5;
            }

            return TraversalOrder6;
        }

        if (random.Next(2) == 1)
        {
            return TraversalOrder7;
        }

        return TraversalOrder8;
    }
}
