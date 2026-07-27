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

        UpdateSporeCellDropdown();
        UpdateGameteDropdowns();
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

        if (data.CellType == GameteACellType)
        {
            GameteACellType = Editor.EditedSpecies.ModifiableCellTypes[0];
        }

        if (data.CellType == GameteBCellType)
        {
            GameteBCellType = Editor.EditedSpecies.ModifiableCellTypes[0];
        }

        UpdateSporeCellDropdown();
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
            OnReproductionMethodChangedToSpore();

        if (ReproductionMethod is MulticellularReproductionMethod.SexualIsogamy
            or MulticellularReproductionMethod.SexualAnisogamy)
        {
            OnReproductionMethodChangedToSexual();
        }

        UpdateReproductionMethodChoice();
    }

    [ArchiveAllowedMethod]
    private void UndoReproductionMethodChangeAction(MulticellularReproductionActionData data)
    {
        ReproductionMethod = data.OldReproductionMethod;

        if (ReproductionMethod == MulticellularReproductionMethod.Sporulation)
            OnReproductionMethodChangedToSpore();

        if (ReproductionMethod is MulticellularReproductionMethod.SexualIsogamy
            or MulticellularReproductionMethod.SexualAnisogamy)
        {
            OnReproductionMethodChangedToSexual();
        }

        UpdateReproductionMethodChoice();
    }

    [ArchiveAllowedMethod]
    private void DoSporeCellChangeAction(SporeCellTypeChangeActionData data)
    {
        SporeCellType = data.NewCellType;

        UpdateSporeCellDropdown();
    }

    [ArchiveAllowedMethod]
    private void UndoSporeCellChangeAction(SporeCellTypeChangeActionData data)
    {
        SporeCellType = data.OldCellType;

        UpdateSporeCellDropdown();
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

    private void OnReproductionMethodChangedToSpore()
    {
        // Set a default spore cell type
        SporeCellType ??= Editor.EditedSpecies.ModifiableCellTypes[0];

        UpdateSporeCellDropdown();
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
