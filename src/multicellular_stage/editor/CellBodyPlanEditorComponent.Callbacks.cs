using Godot;
using SharedBase.Archive;

/// <summary>
///   Callbacks of the cell body plan editor
/// </summary>
public partial class CellBodyPlanEditorComponent
{
    [ArchiveAllowedMethod]
    private void OnCellAdded(HexWithData<CellTemplate> hexWithData)
    {
        cellDataDirty = true;
    }

    [ArchiveAllowedMethod]
    private void OnCellRemoved(HexWithData<CellTemplate> hexWithData)
    {
        cellDataDirty = true;
    }

    [ArchiveAllowedMethod]
    private void DoCellRemoveAction(CellRemoveActionData data)
    {
        editedMicrobeCells.Remove(data.RemovedHex);
    }

    [ArchiveAllowedMethod]
    private void UndoCellRemoveAction(CellRemoveActionData data)
    {
        editedMicrobeCells.AddFast(data.RemovedHex, hexTemporaryMemory, hexTemporaryMemory2);
    }

    [ArchiveAllowedMethod]
    private void DoCellPlaceAction(CellPlacementActionData data)
    {
        editedMicrobeCells.AddFast(data.PlacedHex, hexTemporaryMemory, hexTemporaryMemory2);
    }

    [ArchiveAllowedMethod]
    private void UndoCellPlaceAction(CellPlacementActionData data)
    {
        if (!editedMicrobeCells.Remove(data.PlacedHex))
            GD.PrintErr("Failed to remove placed cell from layout");
    }

    [ArchiveAllowedMethod]
    private void DuplicateCellType(DuplicateDeleteCellTypeData data)
    {
        var originalName = data.CellType.CellTypeName;
        var count = 1;

        // Renaming a cell doesn't create an editor action, so it's possible for someone to duplicate a cell type, undo
        // the duplication, change another cell type's name to the old duplicate's name, then redo the duplication,
        // which would lead to duplicate names, so this loop ensures the duplicated cell's name will be unique
        while (!Editor.IsNewCellTypeNameValid(data.CellType.CellTypeName))
        {
            data.CellType.CellTypeName = $"{originalName} {count++}";
        }

        Editor.EditedSpecies.ModifiableCellTypes.Add(data.CellType);
        GD.Print("New cell type created: ", data.CellType.CellTypeName);

        EmitSignal(SignalName.OnCellTypeToEditSelected, data.CellType.CellTypeName, false);

        UpdateCellTypeSelections();

        UpdateCellTypesSecondaryInfo();

        OnCellToPlaceSelected(data.CellType.CellTypeName);

        Editor.DirtyMutationPointsCache();
    }

    [ArchiveAllowedMethod]
    private void DeleteCellType(DuplicateDeleteCellTypeData data)
    {
        if (!Editor.EditedSpecies.ModifiableCellTypes.Remove(data.CellType))
            GD.PrintErr("Failed to delete cell type from species");

        UpdateCellTypeSelections();

        Editor.DirtyMutationPointsCache();
    }

    [ArchiveAllowedMethod]
    private void DoCellMoveAction(CellMoveActionData data)
    {
        data.MovedHex.Position = data.NewLocation;
        data.MovedHex.Orientation = data.NewRotation;
        data.MovedHex.Data!.Orientation = data.NewRotation;
        data.MovedHex.Data.Position = data.NewLocation;

        if (editedMicrobeCells.Contains(data.MovedHex))
        {
            UpdateAlreadyPlacedVisuals();

            // TODO: notify auto-evo prediction once that is done
        }
        else
        {
            editedMicrobeCells.AddFast(data.MovedHex, hexTemporaryMemory, hexTemporaryMemory2);
        }
    }

    [ArchiveAllowedMethod]
    private void UndoCellMoveAction(CellMoveActionData data)
    {
        data.MovedHex.Position = data.OldLocation;
        data.MovedHex.Orientation = data.OldRotation;
        data.MovedHex.Data!.Orientation = data.OldRotation;
        data.MovedHex.Data.Position = data.OldLocation;

        UpdateAlreadyPlacedVisuals();
    }
}
