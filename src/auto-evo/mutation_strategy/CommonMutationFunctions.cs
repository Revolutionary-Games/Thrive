namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class CommonMutationFunctions
    {
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

                    for (int sideRoll = 1; sideRoll <= 6; ++sideRoll)
                    {
                        // pick a hex direction, with a slight bias towards forwards
                        var side = Math.Max(1, random.Next(7));
                        for (int radius = 1; radius <= 3; ++radius)
                        {
                            // Offset by hex offset multiplied by a factor to check for greater range
                            var hexOffset = Hex.HexNeighbourOffset[(Hex.HexSide)side];
                            hexOffset *= radius;
                            result.Position = pos + hexOffset;

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

            // We didnt find an open spot, this doesn't make much sense
            throw new Exception("Mutation code could not find a good position " +
                "for a new organelle");
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
                foreach (var organelle in organelles.Where(
                            o => islandHexes.Any(h =>
                                o.Definition.GetRotatedHexes(o.Orientation).Contains(h - o.Position))))
                {
                    organelle.Position -= minSubHex;
                }

                islandHexes = organelles.GetIslandHexes();
            }
        }
    }
}
