using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Conversion helpers between the full (gameplay) and editor layouts of a multicellular species
/// </summary>
public static class MulticellularLayoutHelpers
{
    /// <summary>
    ///   Converts the layout in the editor to a gameplay layout. Note that when auto-evo uses this, it should use
    ///   the fast algorithm variant!
    /// </summary>
    public static void UpdateGameplayLayout(CellLayout<CellTemplate> targetGameplayLayout,
        IndividualHexLayout<CellTemplate> targetEditorLayout, IndividualHexLayout<CellTemplate> source,
        AlgorithmQuality algorithmQuality,
        List<Hex> hexTemporaryMemory, List<Hex> hexTemporaryMemory2)
    {
        targetEditorLayout.Clear();
        targetGameplayLayout.Clear();

        if (algorithmQuality is AlgorithmQuality.Low or AlgorithmQuality.Normal)
        {
            foreach (var hexWithData in source.AsModifiable())
            {
                // Add the hex to the remembered editor layout before changing anything
                // This needs to clone to avoid modifying the original hex
                targetEditorLayout.AddFast(hexWithData.Clone(), hexTemporaryMemory,
                    hexTemporaryMemory2);

                var direction = new Vector2(0, -1);
                if (hexWithData.Position != new Hex(0, 0))
                {
                    direction = new Vector2(hexWithData.Position.Q, hexWithData.Position.R).Normalized();
                }

                // Copy the data to the actual data instance as well (this is important for data consistency)
                hexWithData.Data!.Position = new Hex(0, 0);
                hexWithData.Data.Orientation = hexWithData.Orientation;

                int distance = 0;

                while (true)
                {
                    var positionVector = direction * distance;
                    var checkPosition = new Hex((int)positionVector.X, (int)positionVector.Y);
                    hexWithData.Data!.Position = checkPosition;
                    hexWithData.Position = checkPosition;

                    if (targetGameplayLayout.CanPlace(hexWithData.Data, hexTemporaryMemory, hexTemporaryMemory2))
                    {
                        targetGameplayLayout.AddFast(hexWithData.Data, hexTemporaryMemory, hexTemporaryMemory2);
                        break;
                    }

                    ++distance;
                }
            }
        }
        else
        {
            var modifiableSource = source.AsModifiable();

            foreach (var hexWithData in modifiableSource)
            {
                // Add the hex to the remembered editor layout before changing anything
                // This needs to clone to avoid modifying the original hex
                targetEditorLayout.AddFast(hexWithData.Clone(), hexTemporaryMemory,
                    hexTemporaryMemory2);

                hexWithData.Data!.Orientation = hexWithData.Orientation;
            }

            // More expensive algorithm that tries to keep relative cell positions intact
            int positionMultiplier = 1;

            // This more complex algorithm needs more temporary memory
            var visitedItems = new List<CellTemplate>();
            var islandHexes = new List<Hex>();
            var temp1 = new HashSet<Hex>();
            var temp3 = new Queue<Hex>();

            bool moveOnlyOneStepAtATime = false;
            bool removeAllIslandsBeforeMoving = true;
            bool moveTowardsOrigin = false;

            var blobScore = CalculateBlobFactor(source, temp1, hexTemporaryMemory);
            if (blobScore >= 0.65f || (source.Count > 20 && blobScore >= 0.51f))
            {
                GD.Print($"Using more blob-optimized shape for colony (blob score is: {blobScore})");
                moveOnlyOneStepAtATime = true;
            }

            if (blobScore > 0.5f && source.Count > 5)
            {
                moveTowardsOrigin = true;
            }

            if (moveOnlyOneStepAtATime && source.Count > 12)
            {
                removeAllIslandsBeforeMoving = false;
            }

            // We run the core algorithm multiple times in case we run into a failure
            while (true)
            {
                // First, find when cells no longer overlap given a specific multiplier
                FindPositionMultiplierWithNoOverlaps(ref positionMultiplier, targetGameplayLayout, targetEditorLayout,
                    modifiableSource, hexTemporaryMemory, hexTemporaryMemory2);

                // Sanity check before the next step
                if (targetGameplayLayout.Count != modifiableSource.Count)
                    throw new Exception("Not all cells were added");

                // Now we have placed each cell at its wanted position, but we need to next ensure that all cells are
                // touching without introducing overlaps
                if (MoveCellsToBeTouching(targetGameplayLayout, moveOnlyOneStepAtATime, removeAllIslandsBeforeMoving,
                        moveTowardsOrigin, visitedItems, islandHexes, temp1, temp3, hexTemporaryMemory,
                        hexTemporaryMemory2))
                {
                    // Success
                    break;
                }

                // If failed, use a bigger position multiplier and then try again
                ++positionMultiplier;
            }

            // Need to match the order so that the growth order is right after we have adjusted things
            ApplySameItemOrder(targetGameplayLayout, modifiableSource, hexTemporaryMemory, hexTemporaryMemory2);

            if (targetGameplayLayout.Count != modifiableSource.Count)
                throw new Exception("Not all cells were added");

            // Apply final positions to the main source data
            foreach (var hexWithData in modifiableSource)
            {
                hexWithData.Position = hexWithData.Data!.Position;
            }
        }

#if DEBUG
        targetGameplayLayout.ThrowIfCellsOverlap();
#endif
    }

    /// <summary>
    ///   Generates a cell layout from the gameplay layout. To be used if there's no editor layout yet for a species.
    /// </summary>
    public static void GenerateEditorLayoutFromGameplayLayout(IndividualHexLayout<CellTemplate> target,
        CellLayout<CellTemplate> source, List<Hex> hexTemporaryMemory, List<Hex> hexTemporaryMemory2)
    {
        foreach (var cell in source)
        {
            // We set the position below just before the can place check
            var hex = new HexWithData<CellTemplate>((CellTemplate)cell.Clone(), cell.Position, cell.Orientation);

            var originalPos = cell.Position;

            var direction = new Vector2(0, -1);

            if (originalPos != new Hex(0, 0))
            {
                direction = new Vector2(originalPos.Q, originalPos.R).Normalized();
            }

            float distance = 0;

            // Start at 0,0 and move towards the real position until an empty spot is found
            // TODO: need to make sure that this can't cause holes that the player would need to fix
            // distance is a float here to try to make the above TODO problem less likely
            while (true)
            {
                var positionVector = direction * distance;

                var checkPosition = new Hex((int)positionVector.X, (int)positionVector.Y);
                hex.Position = checkPosition;
                hex.Orientation = cell.Orientation;

                // This should never be null, but for extra safety this is done
                if (hex.Data != null)
                {
                    hex.Data.Position = checkPosition;

                    // Also preserve orientation in the different representation
                    hex.Data.Orientation = cell.Orientation;
                }

                if (target.CanPlace(hex, hexTemporaryMemory, hexTemporaryMemory2))
                {
                    target.AddFast(hex, hexTemporaryMemory, hexTemporaryMemory2);
                    break;
                }

                distance += 0.8f;
            }
        }
    }

    private static void FindPositionMultiplierWithNoOverlaps(ref int positionMultiplier,
        CellLayout<CellTemplate> targetGameplayLayout, IndividualHexLayout<CellTemplate> targetEditorLayout,
        HexLayout<HexWithData<CellTemplate>> modifiableSource, List<Hex> hexTemporaryMemory,
        List<Hex> hexTemporaryMemory2)
    {
        int count = modifiableSource.Count;

        while (true)
        {
            targetGameplayLayout.Clear();
            bool fitAll = true;

            for (int i = 0; i < count; ++i)
            {
                var hexWithData = modifiableSource[i];

                var originalData = targetEditorLayout[i];

                // This needs to be able to run multiple times, so do not modify the originalData (yet)
                var checkPosition = originalData.Position * positionMultiplier;
                hexWithData.Data!.Position = checkPosition;

                if (targetGameplayLayout.CanPlace(hexWithData.Data, hexTemporaryMemory, hexTemporaryMemory2))
                {
                    targetGameplayLayout.AddFast(hexWithData.Data, hexTemporaryMemory, hexTemporaryMemory2);
                    continue;
                }

                fitAll = false;
                break;
            }

            if (fitAll)
                break;

            positionMultiplier += 1;

            if (positionMultiplier > 10000)
            {
                GD.PrintErr("Cannot find a cell layout where all are touching");
                throw new Exception(
                    "Position multiplier to fit all cells at their preferred positions would be extreme");
            }
        }
    }

    private static bool MoveCellsToBeTouching(CellLayout<CellTemplate> targetGameplayLayout,
        bool moveOnlyOneStepAtATime, bool removeAllIslandsBeforeMoving, bool moveTowardsOrigin,
        List<CellTemplate> visitedItems, List<Hex> islandHexes, HashSet<Hex> temp1, Queue<Hex> temp3,
        List<Hex> hexTemporaryMemory, List<Hex> hexTemporaryMemory2)
    {
        float moveDistance = 0.8f;
        int attempts = 0;

        while (true)
        {
            // Note: this only works if the primary cell is first in the list, which should be the case as growth
            // FindPositionMultiplierWithNoOverlaps adds things in order (and growth order should be set in the source
            // data already)
            targetGameplayLayout.GetIslandHexes(islandHexes, temp1, hexTemporaryMemory2, temp3);

            // Once all are touching, we can quit
            if (islandHexes.Count == 0)
                return true;

            visitedItems.Clear();

            // We need to move all islands
            foreach (var islandHex in islandHexes)
            {
                var item = targetGameplayLayout.GetElementAt(islandHex, hexTemporaryMemory);

                if (item == null)
                    throw new Exception("Island cell not found");

                // A single cell can have many island hexes reported for it
                if (visitedItems.Contains(item))
                    continue;

                visitedItems.Add(item);
            }

            if (visitedItems.Count < 1)
                throw new Exception("Couldn't find items to move");

            // Remove all visited items from the layout so that they don't block other moves
            if (removeAllIslandsBeforeMoving)
            {
                foreach (var item in visitedItems)
                {
                    if (!targetGameplayLayout.Remove(item))
                        throw new Exception("Failed to temporarily remove a cell");
                }
            }

#if FALSE
            // Try moving non-rotated items first and then closest to origin
            visitedItems.Sort((a, b) =>
            {
                // Put non-rotated (Orientation == 0) items first
                var aBucket = a.Orientation == 0 ? 0 : 1;
                int bBucket = b.Orientation == 0 ? 0 : 1;

                int bucketCompare = aBucket.CompareTo(bBucket);
                return bucketCompare != 0 ? bucketCompare : a.Position.CompareTo(b.Position);
            });
#else

            // Try moving closest to the origin first
            visitedItems.Sort((a, b) => a.Position.CompareTo(b.Position));
#endif

            // Once collecting all, then move to know exactly what we should move
            for (int i = 0; i < visitedItems.Count; ++i)
            {
                var item = visitedItems[i];

                if (!removeAllIslandsBeforeMoving)
                {
                    if (!targetGameplayLayout.Remove(item))
                        throw new Exception("Failed to temporarily remove a cell");
                }

                var originalPosition = item.Position;

                bool addedBack = false;
                bool hasTarget = false;
                Hex targetHex = new Hex(0, 0);

                if (moveTowardsOrigin)
                {
                    // Move towards the origin
                    // TODO: should this check if the first cell is actually at origin or not? We can probably assume
                    // due to layout shifting that it is
                    hasTarget = true;
                }
                else
                {
                    // Move towards the closest non-island cell

                    float minDistance = float.MaxValue;

                    foreach (var cellTemplate in targetGameplayLayout)
                    {
                        // Don't move towards islands (this is an extra safety check for now as we removed the islands
                        // already)
                        if (visitedItems.Contains(cellTemplate))
                            continue;

                        var distance = cellTemplate.Position.DistanceTo(item.Position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            targetHex = cellTemplate.Position;
                            hasTarget = true;
                        }
                    }
                }

                if (hasTarget)
                {
                    var itemPos = new Vector2(item.Position.Q, item.Position.R);
                    var shift = itemPos.DirectionTo(new Vector2(targetHex.Q, targetHex.R));

                    // Move as far as possible towards the target so that we fully handle closest to origin cells first
                    // and only then other ones so they'll have closer cells to move towards
                    float effectiveMoveDistance = moveDistance;
                    const float stepSize = 0.8f;

                    if (moveOnlyOneStepAtATime)
                    {
                        // Increase step size until it results in a difference
                        while (true)
                        {
                            var newPositionRaw = itemPos + shift * effectiveMoveDistance;
                            var newPosition = new Hex((int)Math.Round(newPositionRaw.X),
                                (int)Math.Round(newPositionRaw.Y));

                            if (newPosition != originalPosition)
                            {
                                item.Position = newPosition;
                                if (targetGameplayLayout.CanPlace(item, hexTemporaryMemory, hexTemporaryMemory2))
                                {
                                    targetGameplayLayout.AddFast(item, hexTemporaryMemory, hexTemporaryMemory2);
                                    addedBack = true;
                                }

                                break;
                            }

                            effectiveMoveDistance += stepSize;
                        }
                    }
                    else
                    {
                        bool foundTarget = false;
                        while (effectiveMoveDistance < 1000)
                        {
                            var newPositionRaw = itemPos + shift * effectiveMoveDistance;
                            var newPosition = new Hex((int)Math.Round(newPositionRaw.X),
                                (int)Math.Round(newPositionRaw.Y));

                            item.Position = newPosition;
                            if (targetGameplayLayout.CanPlace(item, hexTemporaryMemory, hexTemporaryMemory2))
                            {
                                if (foundTarget)
                                {
                                    targetGameplayLayout.AddFast(item, hexTemporaryMemory, hexTemporaryMemory2);
                                    addedBack = true;
                                    break;
                                }

                                effectiveMoveDistance += stepSize;
                                continue;
                            }

                            if (foundTarget)
                            {
                                GD.PrintErr("Moving back a step failed for a cell, this should not happen");
                                break;
                            }

                            // We are now too far, so move one step back and then place
                            foundTarget = true;
                            effectiveMoveDistance -= stepSize;
                        }
                    }
                }

                if (!addedBack)
                {
                    // Couldn't shift this cell, hopefully can move something else, and then this can fit in later

                    // Add back to where we removed this from
                    item.Position = originalPosition;
                    if (targetGameplayLayout.CanPlace(item, hexTemporaryMemory, hexTemporaryMemory2))
                    {
                        targetGameplayLayout.AddFast(item, hexTemporaryMemory, hexTemporaryMemory2);
                        addedBack = true;
                    }
                    else
                    {
                        // If something moved and blocked this, try to find a new position in a small radius
                        for (int radius = 1; radius < 4; ++radius)
                        {
                            for (var side = Hex.HexSide.Top; side <= Hex.HexSide.TopLeft; ++side)
                            {
                                var shift = Hex.HexNeighbourOffset[side] * radius;
                                item.Position = originalPosition + shift;

                                if (targetGameplayLayout.CanPlace(item, hexTemporaryMemory, hexTemporaryMemory2))
                                {
                                    targetGameplayLayout.AddFast(item, hexTemporaryMemory, hexTemporaryMemory2);
                                    addedBack = true;
                                    break;
                                }
                            }

                            if (addedBack)
                                break;
                        }
                    }
                }
                else
                {
                    // As this was successfully moved, this is now a valid move target for other cells
                    if (visitedItems.Remove(item))
                    {
                        --i;
                    }
                    else
                    {
                        GD.PrintErr("Expected item remove in layout conversion failed");
                    }
                }

                if (!addedBack)
                {
                    // Could not add a cell back, so we have to fail this entire attempt
                    return false;
                }
            }

            ++attempts;

            // As we slowly can shift the cells, we want to use gentle move distance for a bit before increasing it
            if (attempts > 20)
            {
                moveDistance += 0.8f;
                attempts = 0;

                // Algorithm fails if we just cannot shift things
                if (moveDistance > 10)
                {
                    // This is not going to work, so go back to the earlier phase of the algorithm to try again
                    return false;
                }
            }
        }
    }

    private static void ApplySameItemOrder(CellLayout<CellTemplate> targetGameplayLayout,
        HexLayout<HexWithData<CellTemplate>> modifiableSource, List<Hex> hexTemporaryMemory,
        List<Hex> hexTemporaryMemory2)
    {
        if (targetGameplayLayout.Count != modifiableSource.Count)
            throw new InvalidOperationException("Layout sizes do not match for cell order restoring");

        // More temporary memory use (for the expensive algorithm variant)
        var indexMapping = new Dictionary<CellTemplate, int>();
        var temp = new List<CellTemplate>(modifiableSource.Count);

        int index = 0;
        foreach (var original in modifiableSource)
        {
            indexMapping.Add(original.Data!, index++);
        }

        foreach (var item in targetGameplayLayout)
        {
            temp.Add(item);
        }

        targetGameplayLayout.Clear();

        temp.Sort((a, b) => indexMapping[a].CompareTo(indexMapping[b]));

        foreach (var cellTemplate in temp)
        {
            targetGameplayLayout.AddFast(cellTemplate, hexTemporaryMemory, hexTemporaryMemory2);
        }
    }

    private static float CalculateBlobFactor(HexLayout<HexWithData<CellTemplate>> modifiableSource, HashSet<Hex> temp1,
        List<Hex> temp2)
    {
        var minQ = int.MaxValue;
        var maxQ = int.MinValue;

        var minR = int.MaxValue;
        var maxR = int.MinValue;

        foreach (var hexWithData in modifiableSource)
        {
            var pos = hexWithData.Position;
            minQ = Math.Min(minQ, pos.Q);
            maxQ = Math.Max(maxQ, pos.Q);
            minR = Math.Min(minR, pos.R);
            maxR = Math.Max(maxR, pos.R);
        }

        int filled = 0;
        int empty = 0;

        var cache = temp1;
        modifiableSource.ComputeHexCache(cache, temp2);

        for (int q = minQ; q <= maxQ; ++q)
        {
            for (int r = minR; r < maxR; ++r)
            {
                var pos = new Hex(q, r);

                if (cache.Contains(pos))
                {
                    ++filled;
                }
                else
                {
                    ++empty;
                }
            }
        }

        if (empty == 0)
            return 1;

        return (float)filled / (filled + empty);
    }
}
