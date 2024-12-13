﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AutoEvo;
using Godot;

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

    /// <summary>
    ///   Direction bias for <see cref="OrganelleAddStrategy.Realistic"/>
    /// </summary>
    public enum Direction
    {
        Front,
        Neutral,
        Rear,
    }

    /// <summary>
    ///   Controls the overall used strategy to place an organelle
    /// </summary>
    public enum OrganelleAddStrategy
    {
        Realistic = 0,
        Spiral,
        Front,
        Back,
    }

    public static MicrobeSpecies GenerateRandomSpecies(MicrobeSpecies mutated, MutationWorkMemory workMemory,
        Random random, float mp = 300)
    {
        var mutationStrategy = new AddOrganelleAnywhere(_ => true);

        GameWorld.SetInitialSpeciesProperties(mutated, workMemory.WorkingMemory1, workMemory.WorkingMemory2);

        while (mp > 0)
        {
            var mutation = mutationStrategy.MutationsOf(mutated, mp, true, random)?.OrderBy(_ => random.Next())
                .FirstOrDefault();

            if (mutation == null)
                break;

            mutated = mutation.Item1;
            mp -= mutation.Item2;

            var oldColour = mutated.Colour;

            var redShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;
            var greenShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;
            var blueShift = (random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;

            mutated.Colour = new Color(Math.Clamp((float)(oldColour.R + redShift), 0, 1),
                Math.Clamp((float)(oldColour.G + greenShift), 0, 1),
                Math.Clamp((float)(oldColour.B + blueShift), 0, 1));
        }

        // Override the default species starting name to have more variability in the names
        var nameGenerator = SimulationParameters.Instance.NameGenerator;
        mutated.Epithet = nameGenerator.GenerateNameSection(random, true);
        mutated.Genus = nameGenerator.GenerateNameSection(random);

        mutated.OnEdited();

        return mutated;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AddOrganelle(OrganelleDefinition organelle, Direction direction, MicrobeSpecies newSpecies,
        List<Hex> workMemory1, List<Hex> workMemory2, Random random)
    {
        return AddOrganelleWithStrategy(OrganelleAddStrategy.Realistic, organelle, direction, newSpecies, workMemory1,
            workMemory2, random);
    }

    public static bool AddOrganelleWithStrategy(OrganelleAddStrategy strategy, OrganelleDefinition organelle,
        Direction direction, MicrobeSpecies newSpecies, List<Hex> workMemory1, List<Hex> workMemory2, Random random)
    {
        OrganelleTemplate? position;

        switch (strategy)
        {
            case OrganelleAddStrategy.Realistic:
                position = GetRealisticPosition(organelle, newSpecies.Organelles, direction, workMemory1, workMemory2,
                    random);
                break;
            case OrganelleAddStrategy.Spiral:
                position = GetSpiralPosition(organelle, newSpecies.Organelles, workMemory1, workMemory2);
                break;
            case OrganelleAddStrategy.Front:
                position = GetFrontPosition(organelle, newSpecies.Organelles, workMemory1, workMemory2);
                break;
            case OrganelleAddStrategy.Back:
                position = GetBackPosition(organelle, newSpecies.Organelles, workMemory1, workMemory2);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }

        // We return early as not being able to add an organelle is not a critical failure
        if (position == null)
            return false;

        newSpecies.Organelles.AddFast(position, workMemory1, workMemory2);

        // If the new species is a eukaryote, mark this as such.
        if (organelle == Nucleus)
        {
            newSpecies.IsBacteria = false;
        }

        return true;
    }

    public static void AttachIslandHexes(OrganelleLayout<OrganelleTemplate> organelles, MutationWorkMemory workMemory)
    {
        HashSet<Hex>? mainHexes = null;

        // Use as much work memory as we can get from what we are given
        // TODO: this would need one more hashset to not allocate memory
        var islandHexes = workMemory.WorkingMemory1;

        organelles.GetIslandHexes(islandHexes, workMemory.WorkingMemory3, workMemory.WorkingMemory2,
            workMemory.WorkingMemory4);

        // Attach islands
        while (islandHexes.Count > 0)
        {
            // Unfortunately it seems that just barely the cache is not enough for this to not allocate memory
            mainHexes ??= new HashSet<Hex>();
            organelles.ComputeHexCache(mainHexes, workMemory.WorkingMemory2);

            // Compute the shortest hex distance
            Hex minSubHex = default;
            int minDistance = int.MaxValue;
            foreach (var mainHex in mainHexes)
            {
                if (islandHexes.Contains(mainHex))
                    continue;

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
            var organelleCount = organelles.Count;
            for (int i = 0; i < organelleCount; ++i)
            {
                var organelle = organelles[i];

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

            organelles.GetIslandHexes(islandHexes, workMemory.WorkingMemory3, workMemory.WorkingMemory2,
                workMemory.WorkingMemory4);
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
        // TODO: try to avoid the memory allocation here
        foreach (var otherOrganelle in existingOrganelles.OrderBy(_ => random.Next()))
        {
            // The otherOrganelle is the organelle we wish to be next to
            // Loop its hexes and check positions next to them
            var rotated = otherOrganelle.RotatedHexes;
            var rotatedCount = rotated.Count;
            for (int i = 0; i < rotatedCount; ++i)
            {
                var hex = rotated[i];

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

    private static OrganelleTemplate? GetSpiralPosition(OrganelleDefinition organelle,
        OrganelleLayout<OrganelleTemplate> existingOrganelles, List<Hex> workMemory1,
        List<Hex> workMemory2)
    {
        var result = new OrganelleTemplate(organelle, new Hex(0, 0), 0);

        // Assume can't be placed at 0,0 so start at distance 1 (which is 2 as we divide by two in the real search
        // coordinates)
        for (int q = 2; q <= Constants.DIRECTION_ORGANELLE_CHECK_MAX_DISTANCE * 2; ++q)
        {
            int realQ = q / 2;
            int checkQ;

            if (q % 2 == 1)
            {
                // Alternative checking the negative distance
                checkQ = -realQ;
            }
            else
            {
                checkQ = realQ;
            }

            for (int r = -realQ; r <= checkQ; ++r)
            {
                result.Position = new Hex(checkQ, r);

                if (existingOrganelles.CanPlace(result, workMemory1, workMemory2))
                {
                    return result;
                }
            }
        }

        return null;
    }

    private static OrganelleTemplate? GetFrontPosition(OrganelleDefinition organelle,
        OrganelleLayout<OrganelleTemplate> existingOrganelles, List<Hex> workMemory1,
        List<Hex> workMemory2)
    {
        var result = new OrganelleTemplate(organelle, new Hex(0, 0), 0);

        // Assume can't be placed at 0,0 so start at -1
        for (int r = -1; r > -Constants.DIRECTION_ORGANELLE_CHECK_MAX_DISTANCE; --r)
        {
            result.Position = new Hex(0, r);

            if (existingOrganelles.CanPlace(result, workMemory1, workMemory2))
            {
                return result;
            }
        }

        return null;
    }

    private static OrganelleTemplate? GetBackPosition(OrganelleDefinition organelle,
        OrganelleLayout<OrganelleTemplate> existingOrganelles, List<Hex> workMemory1, List<Hex> workMemory2)
    {
        var result = new OrganelleTemplate(organelle, new Hex(0, 0), 0);

        // Assume can't be placed at 0,0 so start at 1
        for (int r = 1; r < Constants.DIRECTION_ORGANELLE_CHECK_MAX_DISTANCE; ++r)
        {
            result.Position = new Hex(0, r);

            if (existingOrganelles.CanPlace(result, workMemory1, workMemory2))
            {
                return result;
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
