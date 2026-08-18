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

        OnCellToPlaceSelected(data.CellType.CellTypeName);

        Editor.DirtyMutationPointsCache();

        UpdateGameteDropdowns();
    }

    [ArchiveAllowedMethod]
    private void DeleteCellType(DuplicateDeleteCellTypeData data)
    {
        if (!Editor.EditedSpecies.ModifiableCellTypes.Remove(data.CellType))
            GD.PrintErr("Failed to delete cell type from species");

        UpdateCellTypeSelections();

        Editor.DirtyMutationPointsCache();

        if (ReferenceEquals(data.CellType, SporeCellType))
        {
            SporeCellType = Editor.EditedSpecies.ModifiableCellTypes[0];
        }

        if (ReferenceEquals(data.CellType, GameteACellType))
        {
            GameteACellType = Editor.EditedSpecies.ModifiableCellTypes[0];
        }

        if (ReferenceEquals(data.CellType, GameteBCellType))
        {
            GameteBCellType = Editor.EditedSpecies.ModifiableCellTypes[0];
        }

        UpdateGameteDropdowns();
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
            UpdateSpecialCellTypeDisplays();
        }

        if (ReproductionMethod is MulticellularReproductionMethod.SexualIsogamy
            or MulticellularReproductionMethod.SexualAnisogamy)
        {
            OnReproductionMethodChangedToSexual();
        }

        UpdateReproductionMethodChoice();
        UpdateAnisogamyStateAndCost();
    }

    [ArchiveAllowedMethod]
    private void UndoReproductionMethodChangeAction(MulticellularReproductionActionData data)
    {
        ReproductionMethod = data.OldReproductionMethod;

        if (ReproductionMethod == MulticellularReproductionMethod.Sporulation)
        {
            UpdateSpecialCellTypeDisplays();
        }

        if (ReproductionMethod is MulticellularReproductionMethod.SexualIsogamy
            or MulticellularReproductionMethod.SexualAnisogamy)
        {
            OnReproductionMethodChangedToSexual();
        }

        UpdateReproductionMethodChoice();
        UpdateAnisogamyStateAndCost();
    }

    [ArchiveAllowedMethod]
    private void DoSporeCellChangeAction(SporeCellTypeChangeActionData data)
    {
        ChangeSporeCellType(data.OldCellType, data.NewCellType);
    }

    [ArchiveAllowedMethod]
    private void UndoSporeCellChangeAction(SporeCellTypeChangeActionData data)
    {
        ChangeSporeCellType(data.NewCellType, data.OldCellType);
    }

    private void ChangeSporeCellType(CellType? oldCellType, CellType? newCellType)
    {
        if (oldCellType != null)
        {
            if (!Editor.EditedSpecies.ModifiableCellTypes.Remove(oldCellType))
                GD.PrintErr("Failed to delete the spore cell type from species");
        }

        if (newCellType != null)
        {
            OnCellTypeAdded(newCellType);
        }

        SporeCellType = newCellType;
    }

    [ArchiveAllowedMethod]
    private void DoGameteACellChangeAction(GameteACellTypeChangeActionData data)
    {
        GameteACellType = data.NewCellType;

        UpdateGameteDropdowns();
    }

    [ArchiveAllowedMethod]
    private void UndoGameteACellChangeAction(GameteACellTypeChangeActionData data)
    {
        GameteACellType = data.OldCellType;

        UpdateGameteDropdowns();
    }

    [ArchiveAllowedMethod]
    private void DoGameteBCellChangeAction(GameteBCellTypeChangeActionData data)
    {
        GameteBCellType = data.NewCellType;

        UpdateGameteDropdowns();
    }

    [ArchiveAllowedMethod]
    private void UndoGameteBCellChangeAction(GameteBCellTypeChangeActionData data)
    {
        GameteBCellType = data.OldCellType;

        UpdateGameteDropdowns();
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

        Editor.DirtyMutationPointsCache();
    }

    private void OnReproductionMethodChangedToSexual()
    {
        // Set default gamete types
        GameteACellType ??= Editor.EditedSpecies.ModifiableCellTypes[0];

        // Gamete B needs to be set if the reproduction method is anisogamous otherwise the type A is used by all cells
        if (ReproductionMethod is MulticellularReproductionMethod.SexualAnisogamy)
            GameteBCellType ??= Editor.EditedSpecies.ModifiableCellTypes[0];

        UpdateGameteDropdowns();
    }

    [ArchiveAllowedMethod]
    private void DoMassBuddingCellCountChangeAction(MassBuddingCellCountActionData data)
    {
        DesiredMassBuddingCellCount = data.NewCellCount;

        UpdateMassBuddingCellCountSlider();
    }

    [ArchiveAllowedMethod]
    private void UndoMassBuddingCellCountChangeAction(MassBuddingCellCountActionData data)
    {
        DesiredMassBuddingCellCount = data.OldCellCount;

        UpdateMassBuddingCellCountSlider();
    }
}
