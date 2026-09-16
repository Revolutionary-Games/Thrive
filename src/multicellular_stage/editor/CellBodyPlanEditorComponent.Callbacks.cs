using System;
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
    }

    [ArchiveAllowedMethod]
    private void DeleteCellType(DuplicateDeleteCellTypeData data)
    {
        if (!Editor.EditedSpecies.ModifiableCellTypes.Remove(data.CellType))
            GD.PrintErr("Failed to delete cell type from species");

        if (!data.Delete)
        {
            CellTypeVisualsOverride?.ForgetChanges(data.CellType);
        }

        UpdateCellTypeSelections();

        Editor.DirtyMutationPointsCache();

        if (ReferenceEquals(data.CellType, SporeCellType))
        {
            SporeCellType = null;
        }

        if (ReferenceEquals(data.CellType, GameteACellType))
        {
            GameteACellType = null;
        }

        if (ReferenceEquals(data.CellType, GameteBCellType))
        {
            GameteBCellType = null;
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
            UpdateSpecialCellTypeDisplays();
        }

        if (ReproductionMethod is MulticellularReproductionMethod.SexualIsogamy
            or MulticellularReproductionMethod.SexualAnisogamy)
        {
            UpdateSpecialCellTypeDisplays();
        }

        if (ReproductionMethod == MulticellularReproductionMethod.MassBudding)
        {
            // Make sure mass budding is selecting at least the minimum count
            DesiredMassBuddingCellCount =
                Math.Max(DesiredMassBuddingCellCount, Constants.MASS_BUDDING_MINIMUM_BUD_SIZE);
            UpdateMassBuddingCellCountSlider();
        }

        OnReproductionMethodChangedToSexual();

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
            UpdateSpecialCellTypeDisplays();
        }

        OnReproductionMethodChangedToSexual();

        UpdateReproductionMethodChoice();
        UpdateAnisogamyStateAndCost();
    }

    [ArchiveAllowedMethod]
    private void DoSpecialCellChangeAction(SpecialCellTypeChangeActionData data)
    {
        ChangeCellType(data.OldCellType, data.NewCellType, data.CellArchetype);
    }

    [ArchiveAllowedMethod]
    private void UndoSpecialCellChangeAction(SpecialCellTypeChangeActionData data)
    {
        if (data.NewCellType != null)
        {
            CellTypeVisualsOverride?.ForgetChanges(data.NewCellType);
        }

        ChangeCellType(data.NewCellType, data.OldCellType, data.CellArchetype);
    }

    private void ChangeCellType(CellType? oldCellType, CellType? newCellType, SpecialCellArchetype specialCellArchetype)
    {
        if (oldCellType != null)
        {
            if (!Editor.EditedSpecies.ModifiableCellTypes.Remove(oldCellType))
                GD.PrintErr("Failed to delete a special cell type from species");
        }

        SetSpecialCellType(specialCellArchetype, newCellType);

        if (newCellType != null)
        {
            OnCellTypeAdded(newCellType);

            UpdateSpecialCellTypeDisplays();
        }
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

    // These next 7 functions are only here for save compatibility and are otherwise unused
    [ArchiveAllowedMethod]
    private void DoGameteACellChangeAction(GameteACellTypeChangeActionData data)
    {
        // As mentioned above, this and other of the 7 actions here and below are obsolete functions kept for save
        // compatibility and shouldn't be used otherwise
        SwitchGameteCellTypeLegacy(data.OldCellType, data.NewCellType, SpecialCellArchetype.GameteA);
    }

    [ArchiveAllowedMethod]
    private void UndoGameteACellChangeAction(GameteACellTypeChangeActionData data)
    {
        SwitchGameteCellTypeLegacy(data.NewCellType, data.OldCellType, SpecialCellArchetype.GameteA);
    }

    [ArchiveAllowedMethod]
    private void DoGameteBCellChangeAction(GameteBCellTypeChangeActionData data)
    {
        SwitchGameteCellTypeLegacy(data.OldCellType, data.NewCellType, SpecialCellArchetype.GameteB);
    }

    [ArchiveAllowedMethod]
    private void UndoGameteBCellChangeAction(GameteBCellTypeChangeActionData data)
    {
        SwitchGameteCellTypeLegacy(data.NewCellType, data.OldCellType, SpecialCellArchetype.GameteB);
    }

    /// <summary>
    ///   Only for save compatibility reasons
    /// </summary>
    private void SwitchGameteCellTypeLegacy(CellType? oldCellType, CellType? newCellType,
        SpecialCellArchetype specialCellArchetype)
    {
        var specialCellType = GetSpecialCellType(specialCellArchetype);

        // A dirty hack to determine whether the previous gamete cell type was added in a previous editor session
        if (!ReferenceEquals(oldCellType, specialCellType)
            && specialCellType != null)
        {
            Editor.EditedSpecies.ModifiableCellTypes.Remove(specialCellType);
        }

        CellType? cellTypeToUpdateTo = null;

        if (newCellType != null)
        {
            cellTypeToUpdateTo = (CellType)GetEditedCellDataIfEdited(newCellType).Clone();
            cellTypeToUpdateTo.SplitFromTypeName = newCellType.CellTypeName;
        }

        SetSpecialCellType(specialCellArchetype, cellTypeToUpdateTo);

        if (cellTypeToUpdateTo != null)
            OnCellTypeAdded(cellTypeToUpdateTo);

        UpdateSpecialCellTypeDisplays();
    }

    [ArchiveAllowedMethod]
    private void DoSporeCellChangeAction(SpecialCellTypeChangeActionData data)
    {
        ChangeCellType(data.OldCellType, data.NewCellType, SpecialCellArchetype.Spore);
    }

    [ArchiveAllowedMethod]
    private void UndoSporeCellChangeAction(SpecialCellTypeChangeActionData data)
    {
        if (data.NewCellType != null)
        {
            CellTypeVisualsOverride?.ForgetChanges(data.NewCellType);
        }

        ChangeCellType(data.NewCellType, data.OldCellType, SpecialCellArchetype.Spore);
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

    private void OnReproductionMethodChangedToSexual()
    {
        // If the player hasn't set a sex type, force one to be set here now
        if (SelectedGameteTypeForPlayer == GameteType.All)
        {
            GD.Print("Forcing gamete type for player after switching to a sexual reproduction method");
            SelectedGameteTypeForPlayer = GameteType.A;
        }
    }
}
