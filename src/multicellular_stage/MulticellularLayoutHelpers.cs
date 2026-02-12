using System.Collections.Generic;
using Godot;

/// <summary>
///   Conversion helpers between the full (gameplay) and editor layouts of a multicellular species
/// </summary>
public static class MulticellularLayoutHelpers
{
    public static void UpdateGameplayLayout(CellLayout<CellTemplate> targetGameplayLayout,
        IndividualHexLayout<CellTemplate> targetEditorLayout, IndividualHexLayout<CellTemplate> source,
        List<Hex> hexTemporaryMemory, List<Hex> hexTemporaryMemory2)
    {
        targetEditorLayout.Clear();
        targetGameplayLayout.Clear();

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

                if (targetGameplayLayout.CanPlace(hexWithData.Data, hexTemporaryMemory,
                        hexTemporaryMemory2))
                {
                    targetGameplayLayout.AddFast(hexWithData.Data, hexTemporaryMemory,
                        hexTemporaryMemory2);
                    break;
                }

                ++distance;
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
}
