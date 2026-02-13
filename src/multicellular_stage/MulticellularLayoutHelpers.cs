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

            int count = modifiableSource.Count;

            // First, find when cells no longer overlap given a specific multiplier
            while (true)
            {
                targetGameplayLayout.Clear();
                bool fitAll = true;

                for (int i = 0; i < count; ++i)
                {
                    var hexWithData = modifiableSource[i];

                    var originalData = targetEditorLayout[i];

                    var checkPosition = originalData.Position * positionMultiplier;
                    hexWithData.Data!.Position = checkPosition;
                    hexWithData.Position = checkPosition;

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
                    throw new Exception(
                        "Position multiplier to fit all cells at their preferred positions would be extreme");
                }
            }

            if (targetGameplayLayout.Count != count)
                throw new Exception("Not all cells were added");

            // Now we have placed each cell at its wanted position, but we need to next ensure that all cells are
            // touching without introducing overlaps

            // This more complex algorithm needs more temporary memory
            var visitedItems = new List<CellTemplate>();
            var islandHexes = new List<Hex>();
            var temp1 = new HashSet<Hex>();
            var temp3 = new Queue<Hex>();

            float moveDistance = 0.8f;
            int attempts = 0;

            while (true)
            {
                // Note: this only works if the primary cell is first in the list, which should be the case as growth
                // order should have been applied already
                targetGameplayLayout.GetIslandHexes(islandHexes, temp1, hexTemporaryMemory2, temp3);

                // Once all are touching, we can quit
                if (islandHexes.Count == 0)
                    break;

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

                // Try moving non-rotated items first and then closest to origin
                visitedItems.Sort((a, b) =>
                {
                    // Put non-rotated (Orientation == 0) items first
                    var aBucket = a.Orientation == 0 ? 0 : 1;
                    int bBucket = b.Orientation == 0 ? 0 : 1;

                    int bucketCompare = aBucket.CompareTo(bBucket);
                    return bucketCompare != 0 ? bucketCompare : a.Position.CompareTo(b.Position);
                });

                // Once collecting all, then move to know exactly what we should move
                for (int i = 0; i < visitedItems.Count; ++i)
                {
                    var item = visitedItems[i];

                    if (!targetGameplayLayout.Remove(item))
                        throw new Exception("Failed to temporarily remove a cell");

                    var originalPosition = item.Position;

                    bool addedBack = false;

                    // Move towards the closest non-island cell
                    bool hasTarget = false;
                    float minDistance = float.MaxValue;
                    Hex targetHex = new Hex(0, 0);

                    foreach (var cellTemplate in targetGameplayLayout)
                    {
                        // Don't move towards islands
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

                    if (hasTarget)
                    {
                        var itemPos = new Vector2(item.Position.Q, item.Position.R);
                        var shift = itemPos.DirectionTo(new Vector2(targetHex.Q, targetHex.R));

                        var newPositionRaw = itemPos + shift * moveDistance;
                        var newPosition = new Hex((int)Math.Round(newPositionRaw.X), (int)Math.Round(newPositionRaw.Y));

                        item.Position = newPosition;
                        if (targetGameplayLayout.CanPlace(item, hexTemporaryMemory, hexTemporaryMemory2))
                        {
                            targetGameplayLayout.AddFast(item, hexTemporaryMemory, hexTemporaryMemory2);
                            addedBack = true;
                        }
                    }

                    if (!addedBack)
                    {
                        // Couldn't shift this cell, hopefully can move something else, and then this can fit

                        // Add back to where we removed this from
                        item.Position = originalPosition;
                        targetGameplayLayout.AddFast(item, hexTemporaryMemory, hexTemporaryMemory2);
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
                }

                ++attempts;

                // As we slowly can shift the cells, we want to use gentle move distance for a bit before increasing it
                if (attempts > 10)
                {
                    moveDistance += 0.8f;
                    attempts = 0;

                    // Algorithm fails if we just cannot shift things
                    if (moveDistance > 10)
                    {
                        GD.PrintErr("Cannot find a cell layout where all are touching");
                        throw new Exception("Cannot find a cell layout where all are touching and not overlapping");
                    }
                }
            }

            if (targetGameplayLayout.Count != count)
                throw new Exception("We lost a cell somehow in adjusting positions");
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
}
