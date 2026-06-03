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
        OnCellTypeAdded(data.CellType);
    }

    [ArchiveAllowedMethod]
    private void DeleteCellType(DuplicateDeleteCellTypeData data)
    {
        if (!Editor.EditedSpecies.ModifiableCellTypes.Remove(data.CellType))
            GD.PrintErr("Failed to delete cell type from species");

        UpdateCellTypeSelections();

        Editor.DirtyMutationPointsCache();

        if (data.CellType == SporeCellType)
        {
            SporeCellType = Editor.EditedSpecies.ModifiableCellTypes[0];
        }
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

            UpdateSpecializationDisplay();
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
        UpdateSpecializationDisplay();
    }

    [ArchiveAllowedMethod]
    private void DoReproductionMethodChangeAction(MulticellularReproductionActionData data)
    {
        ReproductionMethod = data.NewReproductionMethod;

        if (ReproductionMethod == MulticellularReproductionMethod.Sporulation)
        {
            sporeCellTypeMakerButton.UpdateDisplayedCellType(SporeCellType);
        }

        UpdateReproductionMethodChoice();
    }

    [ArchiveAllowedMethod]
    private void UndoReproductionMethodChangeAction(MulticellularReproductionActionData data)
    {
        ReproductionMethod = data.OldReproductionMethod;

        if (ReproductionMethod == MulticellularReproductionMethod.Sporulation)
        {
            sporeCellTypeMakerButton.UpdateDisplayedCellType(SporeCellType);
        }

        UpdateReproductionMethodChoice();
    }

    [ArchiveAllowedMethod]
    private void DoSporeCellAddAction(SporeCellTypeAddActionData data)
    {
        OnCellTypeAdded(data.SporeCell);

        SporeCellType = data.SporeCell;

        sporeCellTypeMakerButton.UpdateDisplayedCellType(SporeCellType);
    }

    [ArchiveAllowedMethod]
    private void UndoSporeCellAddAction(SporeCellTypeAddActionData data)
    {
        if (!Editor.EditedSpecies.ModifiableCellTypes.Remove(data.SporeCell))
            GD.PrintErr("Failed to delete cell type from species");

        UpdateCellTypeSelections();

        Editor.DirtyMutationPointsCache();

        SporeCellType = null;

        sporeCellTypeMakerButton.UpdateDisplayedCellType(SporeCellType);
    }

    private void OnCellTypeAdded(CellType added)
    {
        var originalName = added.CellTypeName;
        var count = 1;

        // Renaming a cell doesn't create an editor action, so it's possible for someone to duplicate a cell type, undo
        // the duplication, change another cell type's name to the old duplicate's name, then redo the duplication,
        // which would lead to duplicate names, so this loop ensures the duplicated cell's name will be unique
        while (!Editor.IsNewCellTypeNameValid(added.CellTypeName))
        {
            added.CellTypeName = $"{originalName} {count++}";
        }

        Editor.EditedSpecies.ModifiableCellTypes.Add(added);
        GD.Print("New cell type created: ", added.CellTypeName);

        EmitSignal(SignalName.OnCellTypeToEditSelected, added.CellTypeName, false);

        UpdateCellTypeSelections();

        UpdateCellTypesSecondaryInfo();

        OnCellToPlaceSelected(added.CellTypeName);

        Editor.DirtyMutationPointsCache();
    }
}
