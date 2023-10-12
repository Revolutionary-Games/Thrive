using Godot;

/// <summary>
///   Callbacks of the cell body plan editor
/// </summary>
[DeserializedCallbackTarget]
public partial class CellBodyPlanEditorComponent
{
    [DeserializedCallbackAllowed]
    private void OnCellAdded(HexWithData<CellTemplate> hexWithData)
    {
        cellDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnCellRemoved(HexWithData<CellTemplate> hexWithData)
    {
        cellDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void DoCellRemoveAction(CellRemoveActionData data)
    {
        editedMicrobeCells.Remove(data.RemovedHex);
    }

    [DeserializedCallbackAllowed]
    private void UndoCellRemoveAction(CellRemoveActionData data)
    {
        editedMicrobeCells.Add(data.RemovedHex);
    }

    [DeserializedCallbackAllowed]
    private void DoCellPlaceAction(CellPlacementActionData data)
    {
        editedMicrobeCells.Add(data.PlacedHex);
    }

    [DeserializedCallbackAllowed]
    private void UndoCellPlaceAction(CellPlacementActionData data)
    {
        editedMicrobeCells.Remove(data.PlacedHex);
    }

    [DeserializedCallbackAllowed]
    private void DuplicateCellType(DuplicateDeleteCellTypeData data)
    {
        var originalName = data.CellType.TypeName;
        var count = 1;

        // Renaming a cell doesn't create an editor action, so it's possible for someone to duplicate a cell type, undo
        // the duplication, change another cell type's name to the old duplicate's name, then redo the duplication,
        // which would lead to duplicate names, so this loop ensures the duplicated cell's name will be unique
        while (!IsNewCellTypeNameValid(data.CellType.TypeName))
        {
            data.CellType.TypeName = $"{originalName} {count++}";
        }

        Editor.EditedSpecies.CellTypes.Add(data.CellType);
        GD.Print("New cell type created: ", data.CellType.TypeName);

        EmitSignal(nameof(OnCellTypeToEditSelected), data.CellType.TypeName, false);

        UpdateCellTypeSelections();

        OnCellToPlaceSelected(data.CellType.TypeName);
    }

    private void DeleteCellType(DuplicateDeleteCellTypeData data)
    {
        if (!Editor.EditedSpecies.CellTypes.Remove(data.CellType))
            GD.PrintErr("Failed to delete cell type from species");

        UpdateCellTypeSelections();
    }

    [DeserializedCallbackAllowed]
    private void DoCellMoveAction(CellMoveActionData data)
    {
        data.MovedHex.Position = data.NewLocation;
        data.MovedHex.Data!.Orientation = data.NewRotation;

        if (editedMicrobeCells.Contains(data.MovedHex))
        {
            UpdateAlreadyPlacedVisuals();

            // TODO: notify auto-evo prediction once that is done
        }
        else
        {
            editedMicrobeCells.Add(data.MovedHex);
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoCellMoveAction(CellMoveActionData data)
    {
        data.MovedHex.Position = data.OldLocation;
        data.MovedHex.Data!.Orientation = data.OldRotation;

        UpdateAlreadyPlacedVisuals();
    }
}
