using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SharedBase.Archive;
using Systems;
using UnlockConstraints;

/// <summary>
///   The multicellular stage editor main class
/// </summary>
public partial class MulticellularEditor : EditorBase<EditorAction, MicrobeStage>, IEditorReportData,
    ICellEditorData
{
    public const ushort SERIALIZATION_VERSION = 2;

    private readonly MulticellularSpeciesComparer speciesComparer = new();

    private readonly CellTypeEditsHolder cellTypeEditsHolder = new();

#pragma warning disable CA2213
    [Export]
    private MicrobeEditorReportComponent reportTab = null!;

    [Export]
    private MicrobeEditorPatchMap patchMapTab = null!;

    [Export]
    private CellBodyPlanEditorComponent bodyPlanEditorTab = null!;

    [Export]
    private CellEditorComponent cellEditorTab = null!;

    [Export]
    private Control noCellTypeSelected = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Used to cache full edited status for <see cref="speciesComparer"/> usage
    /// </summary>
    private MulticellularEditsFacade? editsFacade;

    private MulticellularSpecies? editedSpecies;

    /// <summary>
    ///   If not null, this is the cell type that is being edited. Note that this is always a temporary holder from
    ///   <see cref="cellTypeEditsHolder"/>, so it can be modified without affecting the original cell type until
    ///   editor exit. This complicates things when all users need to know if they want the latest edits or original
    ///   data when reading this.
    /// </summary>
    private CellType? selectedCellTypeToEdit;

    private Dictionary<OrganelleDefinition, int> tempMemory1 = new();

    public override bool CanCancelAction
    {
        get
        {
            if (bodyPlanEditorTab.Visible)
                return bodyPlanEditorTab.CanCancelAction;

            if (cellEditorTab.Visible)
                return cellEditorTab.CanCancelAction;

            return false;
        }
    }

    public override Species EditedBaseSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    public MulticellularSpecies EditedSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    public Patch CurrentPatch => patchMapTab.CurrentPatch;

    public Patch? TargetPatch => patchMapTab.TargetPatch;

    public Patch? SelectedPatch => patchMapTab.SelectedPatch;

    public ICellDefinition? EditedCellProperties => selectedCellTypeToEdit;

    // TODO: could implement this if desired but for now this is always null (might be needed for multicellular
    // tolerances implementation)
    public IReadOnlyList<OrganelleTemplate>? EditedCellOrganelles => null;

    public override MainGameState GameState => MainGameState.MulticellularEditor;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MulticellularEditor;

    public WorldAndPlayerDataSource UnlocksDataSource =>
        new(CurrentGame.GameWorld, CurrentPatch, GetPlayerDataSource());

    protected override string MusicCategory => "MulticellularEditor";

    protected override MainGameState ReturnToState => MainGameState.MicrobeStage;

    protected override string EditorLoadingMessage =>
        Localization.Translate("LOADING_MULTICELLULAR_EDITOR");

    protected override bool HasInProgressAction => CanCancelAction;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.MulticellularEditor)
            throw new NotSupportedException();

        writer.WriteObject((MulticellularEditor)obj);
    }

    public static MulticellularEditor ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var scene = GD.Load<PackedScene>("res://src/multicellular_stage/editor/MulticellularEditor.tscn");

        var instance = scene.Instantiate<MulticellularEditor>();

        instance.ResolveNodeReferences();

        // This is needed first in case we are on the cell type editor tab
        instance.selectedCellTypeToEdit = reader.ReadObjectOrNull<CellType>();

        if (version > 1)
        {
            reader.ReadObjectProperties(instance.cellTypeEditsHolder);
        }

        // Set this first so that this is available immediately
        instance.bodyPlanEditorTab.CellTypeVisualsOverride = instance.cellTypeEditsHolder;

        reader.ReadObjectProperties(instance.reportTab);
        reader.ReadObjectProperties(instance.patchMapTab);
        reader.ReadObjectProperties(instance.bodyPlanEditorTab);
        reader.ReadObjectProperties(instance.cellEditorTab);

        // Base version is different
        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        instance.editedSpecies = reader.ReadObjectOrNull<MulticellularSpecies>();

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectOrNull(selectedCellTypeToEdit);

        writer.WriteObjectProperties(cellTypeEditsHolder);

        writer.WriteObjectProperties(reportTab);
        writer.WriteObjectProperties(patchMapTab);
        writer.WriteObjectProperties(bodyPlanEditorTab);
        writer.WriteObjectProperties(cellEditorTab);

        // Don't call base as it is the base abstract one
        writer.Write(SERIALIZATION_VERSION_BASE);
        WriteBasePropertiesToArchive(writer);

        writer.WriteObjectOrNull(editedSpecies);
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        CheatManager.OnRevealAllPatches += OnRevealAllPatchesCheatUsed;
        CheatManager.OnUnlockAllOrganelles += OnUnlockAllOrganellesCheatUsed;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        CheatManager.OnRevealAllPatches -= OnRevealAllPatchesCheatUsed;
        CheatManager.OnUnlockAllOrganelles -= OnUnlockAllOrganellesCheatUsed;
    }

    public void SendAutoEvoResultsToReportComponent()
    {
        if (autoEvoResults == null)
        {
            GD.PrintErr("Unexpectedly missing auto-evo results");
            return;
        }

        UpdateAutoEvoToReportTab();
    }

    public override void SetEditorObjectVisibility(bool shown)
    {
        base.SetEditorObjectVisibility(shown);

        bodyPlanEditorTab.SetEditorWorldGuideObjectVisibility(shown);
        cellEditorTab.SetEditorWorldGuideObjectVisibility(shown);
    }

    public void OnCurrentPatchUpdated(Patch patch)
    {
        bodyPlanEditorTab.OnCurrentPatchUpdated(patch);

        cellEditorTab.OnCurrentPatchUpdated(patch);

        cellEditorTab.UpdateBackgroundImage(patch);
    }

    public override void AddContextToAction(CombinableActionData action)
    {
        // If a cell type is being edited, add its type to each action data so that we can use it for undoing and
        // redoing later. And this makes the MP system work.
        if (selectedCellTypeToEdit != null)
        {
            if (action is EditorCombinableActionData<CellType> cellTypeData && cellTypeData.Context == null)
            {
                // For MP comparisons of new cell types, we want a consistent reference to the original cell type,
                // so we reverse map this back to the original
                cellTypeData.Context = cellTypeEditsHolder.GetOriginalType(selectedCellTypeToEdit);
            }
        }
    }

    public override bool CancelCurrentAction()
    {
        if (bodyPlanEditorTab.Visible)
        {
            return bodyPlanEditorTab.CancelCurrentAction();
        }

        if (cellEditorTab.Visible)
        {
            return cellEditorTab.CancelCurrentAction();
        }

        GD.PrintErr("No action to cancel");
        return false;
    }

    public override void Redo()
    {
        var cellType = history.GetRedoContext<CellType>();

        // If the action we're redoing should be done on another cell type,
        // save our changes to the current cell type, then switch to the other one
        SwapEditingCellIfNeeded(cellType != null ? cellTypeEditsHolder.GetCellType(cellType) : null);

        // If the action we're redoing should be done on another editor tab, switch to that tab
        SwapEditorTabIfNeeded(history.ActionToRedo());

        base.Redo();
    }

    public bool IsNewCellTypeNameValid(string newName)
    {
        // Name is invalid if it is empty or a duplicate
        // TODO: should this ensure the name doesn't have trailing whitespace?
        // If so, CellTemplate.UpdateNameIfValid should be updated as well
        return !string.IsNullOrWhiteSpace(newName) && !EditedSpecies.ModifiableCellTypes.Any(c =>
            c.CellTypeName.Equals(newName, StringComparison.InvariantCultureIgnoreCase));
    }

    public override void Undo()
    {
        var cellType = history.GetUndoContext<CellType>();

        // If the action we're undoing should be done on another cell type,
        // save our changes to the current cell type, then switch to the other one
        SwapEditingCellIfNeeded(cellType != null ? cellTypeEditsHolder.GetCellType(cellType) : null);

        // If the action we're undoing should be done on another editor tab, switch to that tab
        SwapEditorTabIfNeeded(history.ActionToUndo());

        base.Undo();
    }

    public ToleranceResult CalculateRawTolerances(bool excludePositiveBuffs = false)
    {
        return bodyPlanEditorTab.CalculateRawTolerances(excludePositiveBuffs);
    }

    public void OnTolerancesChanged(EnvironmentalTolerances newTolerances)
    {
        cellEditorTab.OnTolerancesChanged(newTolerances);
    }

    public EnvironmentalTolerances GetOptimalTolerancesForCurrentPatch()
    {
        return CurrentPatch.GenerateTolerancesForMicrobe(bodyPlanEditorTab.GetCurrentCellsWithLatestTypes());
    }

    public ToleranceResult CalculateCurrentTolerances(EnvironmentalTolerances calculationTolerances)
    {
        return MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(calculationTolerances,
            bodyPlanEditorTab.GetCurrentCellsWithLatestTypes(), CurrentPatch.Biome);
    }

    public void GetCurrentToleranceSummaryByElement(ToleranceModifier toleranceCategory,
        Dictionary<IPlayerReadableName, float> result)
    {
        MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByCell(
            bodyPlanEditorTab.GetCurrentCellsWithLatestTypes(), toleranceCategory, result);
    }

    public void CalculateBodyEffectOnTolerances(
        ref MicrobeEnvironmentalToleranceCalculations.ToleranceValues modifiedTolerances)
    {
        MicrobeEnvironmentalToleranceCalculations.ApplyCellEffectsOnTolerances(
            bodyPlanEditorTab.GetCurrentCellsWithLatestTypes(), ref modifiedTolerances);
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
    }

    protected override void InitEditor(bool fresh)
    {
        patchMapTab.SetMap(CurrentGame.GameWorld.Map, CurrentGame.GameWorld.PlayerSpecies.ID);

        // Set first so that data can be immediately used in the cell editor tab
        bodyPlanEditorTab.CellTypeVisualsOverride = cellTypeEditsHolder;

        base.InitEditor(fresh);

        reportTab.UpdateReportTabPatchSelector();

        if (fresh)
        {
            CurrentGame.SetBool("edited_multicellular", true);
        }
        else
        {
            SendAutoEvoResultsToReportComponent();

            reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

            reportTab.UpdatePatchDetails(CurrentPatch, TargetPatch);

            reportTab.UpdateEvents(CurrentGame.GameWorld.EventsLog, CurrentGame.GameWorld.TotalPassedTime);
        }

        cellEditorTab.UpdateBackgroundImage(CurrentPatch);
    }

    protected override void InitEditorGUI(bool fresh)
    {
        reportTab.OnNextTab = () => SetEditorTab(EditorTab.PatchMap);
        patchMapTab.OnNextTab = () => SetEditorTab(EditorTab.CellEditor);
        bodyPlanEditorTab.OnFinish = ForwardEditorComponentFinishRequest;
        cellEditorTab.OnNextTab = () => SetEditorTab(EditorTab.CellEditor);
        cellEditorTab.ValidateNewCellTypeName = IsNewCellTypeNameValid;

        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.Init(this, fresh);
        }
    }

    protected override void OnEnterEditor()
    {
        base.OnEnterEditor();

        if (!IsLoadedFromSave)
            TutorialState.SendEvent(TutorialEventType.EnteredMulticellularEditor, EventArgs.Empty, this);
    }

    protected override void UpdateHistoryCallbackTargets(ActionHistory<EditorAction> actionHistory)
    {
        // See TODO comment in MicrobeEditor.UpdateHistoryCallbackTargets
    }

    protected override IEnumerable<IEditorComponent> GetAllEditorComponents()
    {
        yield return reportTab;
        yield return patchMapTab;
        yield return bodyPlanEditorTab;
        yield return cellEditorTab;
    }

    protected override void OnEditorReady()
    {
        // The base method stores the data, so we just need to update the GUI here (in case of failure)
        var run = CurrentGame.GameWorld.GetAutoEvoRun();

        if (run.Results == null)
        {
            reportTab.DisplayAutoEvoFailure(run.Status);
        }

        base.OnEditorReady();

        reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

        if (autoEvoResults != null && autoEvoExternal != null)
        {
            UpdateAutoEvoToReportTab();
        }

        reportTab.UpdatePatchDetails(CurrentPatch, TargetPatch);

        reportTab.UpdateEvents(CurrentGame.GameWorld.EventsLog, CurrentGame.GameWorld.TotalPassedTime);

        patchMapTab.UpdatePatchEvents();
        patchMapTab.MarkDrawerDirty();
    }

    protected override void OnUndoPerformed()
    {
        base.OnUndoPerformed();

        CheckDidActionAffectCellTypes(history.ActionToRedo());
    }

    protected override void OnRedoPerformed()
    {
        base.OnRedoPerformed();

        CheckDidActionAffectCellTypes(history.ActionToUndo());
    }

    protected override void UpdatePatchDetails()
    {
        // Patch events are able to change the stage's background, so it needs to be updated here.
        cellEditorTab.UpdateBackgroundImage(CurrentPatch);
    }

    protected override double CalculateUsedMutationPoints(List<EditorCombinableActionData> performedActionData)
    {
        editsFacade ??=
            new MulticellularEditsFacade(editedSpecies ??
                throw new Exception("Species not initialized before calculating MP"));

        editsFacade.SetActiveActions(performedActionData);

        return speciesComparer.Compare(editedSpecies!, editsFacade, Constants.MAX_SINGLE_EDIT_MP_COST,
            CurrentGame.GameWorld.WorldSettings.MPMultiplier);
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewMulticellularGame(new WorldGenerationSettings());
    }

    protected override void PerformAutoSave()
    {
        SaveHelper.AutoSave(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.QuickSave(this);
    }

    protected override void SaveGame(string name)
    {
        SaveHelper.Save(name, this);
    }

    protected override void ApplyEditorTab()
    {
        // Hide all
        reportTab.Hide();
        patchMapTab.Hide();
        bodyPlanEditorTab.Hide();
        cellEditorTab.Hide();
        noCellTypeSelected.Hide();

        // Show selected
        switch (selectedEditorTab)
        {
            case EditorTab.Report:
            {
                reportTab.Show();
                SetEditorObjectVisibility(false);
                reportTab.UpdatePatchDetailsIfNeeded(SelectedPatch ?? CurrentPatch);
                break;
            }

            case EditorTab.PatchMap:
            {
                patchMapTab.Show();
                SetEditorObjectVisibility(false);
                break;
            }

            case EditorTab.CellEditor:
            {
                // This must be set visible before CheckAndApplyCellTypeEdit otherwise it won't update already placed
                // visuals
                bodyPlanEditorTab.Show();

                // If we have an edited cell type, then we can apply those changes when we go back to the main editor
                // tab as that's the only exit point and the point where we actually need to use the edited cell
                // type information
                // TODO: write an explanation here why this needs to be before the visibility adjustment
                // See: https://github.com/Revolutionary-Games/Thrive/pull/3457
                CheckAndApplyCellTypeEdit();

                SetEditorObjectVisibility(true);
                cellEditorTab.SetEditorWorldTabSpecificObjectVisibility(false);
                bodyPlanEditorTab.SetEditorWorldTabSpecificObjectVisibility(true);

                bodyPlanEditorTab.UpdateArrow();
                bodyPlanEditorTab.UpdateCamera();

                break;
            }

            case EditorTab.CellTypeEditor:
            {
                if (selectedCellTypeToEdit == null)
                {
                    // Show the "select a cell type" text if not selected yet instead of the cell editor
                    noCellTypeSelected.Show();
                    SetEditorObjectVisibility(false);
                }
                else
                {
                    cellEditorTab.Show();
                    SetEditorObjectVisibility(true);
                    bodyPlanEditorTab.SetEditorWorldTabSpecificObjectVisibility(false);
                    cellEditorTab.SetEditorWorldTabSpecificObjectVisibility(true);

                    cellEditorTab.UpdateArrow();
                    cellEditorTab.UpdateCamera();
                }

                break;
            }

            default:
                throw new Exception("Invalid editor tab");
        }
    }

    protected override void SetupEditedSpecies()
    {
        var species = (MulticellularSpecies?)CurrentGame.GameWorld.PlayerSpecies;
        editedSpecies = species ?? throw new NullReferenceException("didn't find edited species");
        cellTypeEditsHolder.Reset();

        base.SetupEditedSpecies();
    }

    protected override void OnEditorExitTransitionFinished()
    {
        // Clear the edited cell type to avoid the cell editor applying the changes unnecessarily
        selectedCellTypeToEdit = null;

        base.OnEditorExitTransitionFinished();
    }

    private void OnRevealAllPatchesCheatUsed(object? sender, EventArgs args)
    {
        CurrentGame.GameWorld.Map.RevealAllPatches();
        patchMapTab.MarkDrawerDirty();
    }

    private void OnUnlockAllOrganellesCheatUsed(object? sender, EventArgs args)
    {
        if (CurrentGame.GameWorld.UnlockProgress.UnlockAll)
            return;

        CurrentGame.GameWorld.UnlockProgress.UnlockAll = true;
        cellEditorTab.UnlockAllOrganelles();
    }

    private void UpdateAutoEvoToReportTab()
    {
        if (autoEvoResults == null)
            throw new InvalidOperationException("May not be called without report");

        // This creates a new callable each time, but the garbage amount should be negligible
        reportTab.UpdateAutoEvoResults(autoEvoResults, autoEvoExternal?.ToString() ?? "error",
            () => autoEvoResults.MakeSummary(true));
    }

    private void OnStartEditingCellType(string? name, bool switchTab)
    {
        if (CanCancelAction)
        {
            ToolTipManager.Instance.ShowPopup(Localization.Translate("ACTION_BLOCKED_WHILE_ANOTHER_IN_PROGRESS"),
                1.5f);
            return;
        }

        // If there is a null name, that means there is no selected cell,
        // so clear the selectedCellTypeToEdit and return early
        if (string.IsNullOrEmpty(name))
        {
            selectedCellTypeToEdit = null;
            GD.Print("Cleared editing cell type");
            return;
        }

        var newTypeToEdit = EditedSpecies.ModifiableCellTypes.First(c => c.CellTypeName == name);

        // Only reinitialize the editor when required
        if (selectedCellTypeToEdit == null ||
            cellTypeEditsHolder.GetOriginalType(selectedCellTypeToEdit) != newTypeToEdit)
        {
            selectedCellTypeToEdit = cellTypeEditsHolder.BeginOrContinueEdit(newTypeToEdit);

            GD.Print("Start editing cell type: ", selectedCellTypeToEdit.CellTypeName);

            // Reinitialize the cell editor to be able to edit the new cell type
            cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
        }

        if (switchTab)
            SetEditorTab(EditorTab.CellTypeEditor);
    }

    private void CheckAndApplyCellTypeEdit()
    {
        if (selectedCellTypeToEdit == null)
            return;

        GD.Print("Saving temporary changes made to cell type: ", selectedCellTypeToEdit.CellTypeName);

        // Apply any changes made to the selected cell (but only to the edit holder to not break MP)
        FinishEditingSelectedCell();
    }

    /// <summary>
    ///   Detects if a cell edit was done that requires us to re-apply cell graphics
    /// </summary>
    private void CheckDidActionAffectCellTypes(EditorAction? action)
    {
        if (action == null)
        {
            GD.PrintErr("Action to check for cell type changes is null");
            return;
        }

        // We don't need to care while in the cell editor tab
        if (selectedEditorTab == EditorTab.CellTypeEditor)
            return;

        // Or if no type is selected to edit
        if (selectedCellTypeToEdit == null)
            return;

        bool affectedACell = false;

        foreach (var actionData in action.Data)
        {
            switch (actionData)
            {
                case OrganelleMoveActionData:
                case OrganelleRemoveActionData:
                case OrganellePlacementActionData:
                case MembraneActionData:
                case RigidityActionData:
                case NewMicrobeActionData:
                case ColourActionData:
                case OrganelleUpgradeActionData:
                    affectedACell = true;
                    break;
            }

            if (affectedACell)
                break;
        }

        if (affectedACell)
        {
            GD.Print("Undone / redone action affected cell types");
            cellEditorTab.OnFinishEditing(false);

            // cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
            bodyPlanEditorTab.OnCellTypeEdited(cellTypeEditsHolder.GetOriginalType(selectedCellTypeToEdit));
        }
    }

    private void FinishEditingSelectedCell()
    {
        if (selectedCellTypeToEdit == null)
            return;

        var oldName = selectedCellTypeToEdit.CellTypeName;

        cellEditorTab.OnFinishEditing(false);

        // Revert to the old name if the name is a duplicate
        if (EditedSpecies.ModifiableCellTypes.Any(c =>
                c != selectedCellTypeToEdit && c.CellTypeName == selectedCellTypeToEdit.CellTypeName))
        {
            if (oldName != selectedCellTypeToEdit.CellTypeName)
            {
                GD.Print("Cell editor renamed a cell type to a duplicate name, reverting");
                selectedCellTypeToEdit.CellTypeName = oldName;
            }
            else
            {
                GD.Print("Cell type name was not edited");
            }
        }
        else
        {
            // Apply the name immediately to the original species so that MP comparison works better
            GD.Print("Applying rename to original cell type immediately");
            cellTypeEditsHolder.GetOriginalType(selectedCellTypeToEdit).CellTypeName =
                selectedCellTypeToEdit.CellTypeName;
        }

        bodyPlanEditorTab.OnCellTypeEdited(cellTypeEditsHolder.GetOriginalType(selectedCellTypeToEdit));
    }

    private void SwapEditingCellIfNeeded(CellType? newCell)
    {
        if (selectedCellTypeToEdit == newCell || newCell == null)
            return;

        // If we're switching to a new cell type, apply any changes made to the old one
        if (selectedEditorTab == EditorTab.CellTypeEditor && selectedCellTypeToEdit != null)
            FinishEditingSelectedCell();

        // The edit should be to an already started cell type edit
        if (ReferenceEquals(cellTypeEditsHolder.GetOriginalType(newCell), newCell))
            GD.PrintErr("Trying to edit a cell type that hasn't been started yet, this will corrupt MP state");

        // This fixes complex cases where multiple types are undoing and redoing actions
        selectedCellTypeToEdit = newCell;
        cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
    }

    private void SwapEditorTabIfNeeded(EditorAction? editorAction)
    {
        if (editorAction == null)
            return;

        var actionData = editorAction.Data.FirstOrDefault();

        var targetTab = actionData switch
        {
            // If the action was performed on a single Cell Type, target the Cell Type Editor tab
            EditorCombinableActionData<CellType> => EditorTab.CellTypeEditor,

            // If the action was performed on the species as a whole, target the Cell Editor tab
            EditorCombinableActionData<MulticellularSpecies> => EditorTab.CellEditor,

            // If the action wasn't performed in any specific context, just stay on the currently selected tab
            _ => selectedEditorTab,
        };

        // If we're already on the selected tab, there's no need to do anything
        if (targetTab == selectedEditorTab)
            return;

        SetEditorTab(targetTab);
    }

    private IPlayerDataSource GetPlayerDataSource()
    {
        if (editedSpecies == null)
        {
            throw new Exception("Tried to get player unlocks data source without an edited species being set");
        }

        var energyBalance = new EnergyBalanceInfoSimple();

        var tolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
            MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(editedSpecies, CurrentPatch.Biome));

        foreach (var cellType in editedSpecies.ModifiableCellTypes)
        {
            var cellEnergyBalance = new EnergyBalanceInfoSimple();

            // TODO: specialization from positions (GetAdjacencySpecializationBonus)
            var specialization =
                MicrobeInternalCalculations.CalculateSpecializationBonus(cellType.ModifiableOrganelles.Organelles,
                    tempMemory1);

            ProcessSystem.ComputeEnergyBalanceSimple(cellType.ModifiableOrganelles.Organelles, CurrentPatch.Biome,
                in tolerances, specialization, cellType.MembraneType, Vector3.Zero, false, true,
                CurrentGame.GameWorld.WorldSettings, CompoundAmountType.Maximum, null, cellEnergyBalance);

            GetBestEnergyBalanceProperties(energyBalance, cellEnergyBalance);
        }

        return new MulticellularUnlocksData(editedSpecies.ModifiableEditorCells, energyBalance);
    }

    private void GetBestEnergyBalanceProperties(EnergyBalanceInfoSimple energyBalance, EnergyBalanceInfoSimple toAdd)
    {
        energyBalance.BaseMovement = MathF.Max(energyBalance.BaseMovement, toAdd.BaseMovement);
        energyBalance.Flagella = MathF.Max(energyBalance.Flagella, toAdd.Flagella);
        energyBalance.Cilia = MathF.Max(energyBalance.Cilia, toAdd.Cilia);
        energyBalance.TotalMovement = MathF.Max(energyBalance.TotalMovement, toAdd.TotalMovement);

        energyBalance.Osmoregulation = MathF.Max(energyBalance.Osmoregulation, toAdd.Osmoregulation);

        energyBalance.TotalProduction = MathF.Max(energyBalance.TotalProduction, toAdd.TotalProduction);
        energyBalance.TotalConsumption = MathF.Max(energyBalance.TotalConsumption, toAdd.TotalConsumption);
        energyBalance.TotalConsumptionStationary = MathF.Max(energyBalance.TotalConsumptionStationary,
            toAdd.TotalConsumptionStationary);

        energyBalance.FinalBalance = MathF.Max(energyBalance.FinalBalance, toAdd.FinalBalance);
        energyBalance.FinalBalanceStationary = MathF.Max(energyBalance.FinalBalanceStationary,
            toAdd.FinalBalanceStationary);
    }

    private class MulticellularUnlocksData : IPlayerDataSource
    {
        public IReadOnlyList<HexWithData<CellTemplate>>? CellLayout;

        public MulticellularUnlocksData(IReadOnlyList<HexWithData<CellTemplate>>? cellLayout,
            EnergyBalanceInfoSimple? energyBalance)
        {
            CellLayout = cellLayout;
            EnergyBalance = energyBalance;
        }

        public EnergyBalanceInfoSimple? EnergyBalance { get; set; }

        public float Speed
        {
            get
            {
                if (CellLayout == null)
                    return 0;

                return MicrobeInternalCalculations.SpeedToUserReadableNumber(
                    CellBodyPlanInternalCalculations.CalculateSpeed(CellLayout));
            }
        }
    }
}
