namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

public static class CommonMutationFunctions
{
    public static OrganelleDefinition Nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

    public enum Direction
    {
        Front,
        Neutral,
        Rear,
    }

    public static void AddOrganelle(OrganelleDefinition organelle, Direction direction, MicrobeSpecies newSpecies,
        Random random)
    {
        OrganelleTemplate position;

        if (direction == Direction.Neutral)
        {
            position = GetRealisticPosition(organelle, newSpecies.Organelles, new Random());
        }
        else
        {
            var x = (int)(random.NextSingle() * 7 - 3);

            position = new OrganelleTemplate(organelle,
                direction == Direction.Front ? new Hex(x, -100) : new Hex(x, 100),
                direction == Direction.Front ? 0 : 3);
        }

        newSpecies.Organelles.Add(position);
        AttachIslandHexes(newSpecies.Organelles);

        // If the new species is a eukaryote, mark this as such.
        if (organelle == Nucleus)
        {
            newSpecies.IsBacteria = false;
        }
    }

    public static OrganelleTemplate GetRealisticPosition(OrganelleDefinition organelle,
        OrganelleLayout<OrganelleTemplate> existingOrganelles, Random random)
    {
        var result = new OrganelleTemplate(organelle, new Hex(0, 0), 0);

        // Loop through all the organelles and find an open spot to
        // place our new organelle attached to existing organelles
        // This almost always is over at the first iteration, so its
        // not a huge performance hog
        foreach (var otherOrganelle in existingOrganelles.OrderBy(_ => random.Next()))
        {
            // The otherOrganelle is the organelle we wish to be next to
            // Loop its hexes and check positions next to them
            foreach (var hex in otherOrganelle.RotatedHexes)
            {
                // Offset by hexes in organelle we are looking at
                var pos = otherOrganelle.Position + hex;

                foreach (int side in SideTraveralOrder(hex, random))
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

                            if (existingOrganelles.CanPlace(result, new List<Hex>(), new List<Hex>()))
                            {
                                return result;
                            }
                        }

                        // Check every possible rotation value.
                        for (int rotation = 0; rotation <= 5; ++rotation)
                        {
                            result.Orientation = rotation;

                            if (existingOrganelles.CanPlace(result, new List<Hex>(), new List<Hex>()))
                            {
                                return result;
                            }
                        }
                    }
                }
            }
        }

        // We didn't find an open spot, this doesn't make much sense
        throw new Exception("Mutation code could not find a good position " +
            "for a new organelle");
    }

    private static int[] SideTraveralOrder(Hex hex, Random random)
    {
        if (hex.Q < 0)
        {
            if (hex.R < 0)
            {
                return [2, 3, 1, 4, 5, 6];
            }

            return [3, 2, 1, 5, 4, 6];
        }

        if (hex.Q > 0)
        {
            if (hex.R < 0)
            {
                return [5, 6, 4, 1, 2, 3];
            }

            return [6, 5, 4, 1, 3, 2];
        }

        if (random.Next(2) == 1)
        {
            return [1, 6, 2, 5, 3, 4];
        }

        return [1, 2, 6, 3, 5, 4];
    }

    public static void AttachIslandHexes(OrganelleLayout<OrganelleTemplate> organelles)
    {
        var islandHexes = organelles.GetIslandHexes();

        // Attach islands
        while (islandHexes.Count > 0)
        {
            var mainHexes = organelles.ComputeHexCache().Except(islandHexes);

            // Compute shortest hex distance
            Hex minSubHex = default;
            int minDistance = int.MaxValue;
            foreach (var mainHex in mainHexes)
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
            foreach (var organelle in organelles.Where(o => islandHexes.Any(h =>
                         o.Definition.GetRotatedHexes(o.Orientation).Contains(h - o.Position))))
            {
                organelle.Position -= minSubHex;
            }

            islandHexes = organelles.GetIslandHexes();
        }
    }
}
