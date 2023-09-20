using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/early_multicellular_stage/editor/EarlyMulticellularEditor.tscn")]
[DeserializedCallbackTarget]
public class EarlyMulticellularEditor : EditorBase<EditorAction, MicrobeStage>, IEditorReportData, ICellEditorData
{
    [Export]
    public NodePath? ReportTabPath;

    [Export]
    public NodePath PatchMapTabPath = null!;

    [Export]
    public NodePath BodyPlanEditorTabPath = null!;

    [Export]
    public NodePath CellEditorTabPath = null!;

    [Export]
    public NodePath NoCellTypeSelectedPath = null!;

#pragma warning disable CA2213
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorReportComponent reportTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorPatchMap patchMapTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellBodyPlanEditorComponent bodyPlanEditorTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellEditorComponent cellEditorTab = null!;

    private Control noCellTypeSelected = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private EarlyMulticellularSpecies? editedSpecies;

    [JsonProperty]
    private CellType? selectedCellTypeToEdit;

    [JsonProperty]
    private bool newCellTypeEditHasStarted;

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

    [JsonIgnore]
    public override Species EditedBaseSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    [JsonIgnore]
    public EarlyMulticellularSpecies EditedSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    [JsonIgnore]
    public Patch CurrentPatch => patchMapTab.CurrentPatch;

    [JsonIgnore]
    public Patch? SelectedPatch => patchMapTab.SelectedPatch;

    [JsonIgnore]
    public ICellProperties? EditedCellProperties => selectedCellTypeToEdit;

    protected override string MusicCategory => "EarlyMulticellularEditor";

    protected override MainGameState ReturnToState => MainGameState.MicrobeStage;

    protected override string EditorLoadingMessage =>
        TranslationServer.Translate("LOADING_EARLY_MULTICELLULAR_EDITOR");

    protected override bool HasInProgressAction => CanCancelAction;

    public void SendAutoEvoResultsToReportComponent()
    {
        reportTab.UpdateAutoEvoResults(autoEvoSummary?.ToString() ?? "error", autoEvoExternal?.ToString() ?? "error");
    }

    public override void SetEditorObjectVisibility(bool shown)
    {
        base.SetEditorObjectVisibility(shown);

        bodyPlanEditorTab.SetEditorWorldGuideObjectVisibility(shown);
        cellEditorTab.SetEditorWorldGuideObjectVisibility(shown);
    }

    public void OnCurrentPatchUpdated(Patch patch)
    {
        cellEditorTab.OnCurrentPatchUpdated(patch);

        reportTab.UpdatePatchDetails(patch);

        cellEditorTab.UpdateBackgroundImage(patch.BiomeTemplate);
    }

    public void UpdateReportTabPatchSelector()
    {
        reportTab.UpdateReportTabPatchSelector();
    }

    public override int WhatWouldActionsCost(IEnumerable<EditorCombinableActionData> actions)
    {
        return history.WhatWouldActionsCost(actions);
    }

    public override bool EnqueueAction(EditorAction action)
    {
        // If we performed the first action on the cell editor tab, we want to combine that with the start edit action
        // so that we can keep undo working in complex situations
        if (selectedEditorTab == EditorTab.CellTypeEditor && newCellTypeEditHasStarted)
        {
            if (selectedCellTypeToEdit != null)
            {
                action = new CombinedEditorAction(
                    new SingleEditorAction<StartCellTypeEditActionData>(DoStartCellTypeEditAction,
                        UndoStartCellTypeEditAction, new StartCellTypeEditActionData(selectedCellTypeToEdit)), action);
            }

            newCellTypeEditHasStarted = false;
        }

        return base.EnqueueAction(action);
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

    protected override void ResolveDerivedTypeNodeReferences()
    {
        reportTab = GetNode<MicrobeEditorReportComponent>(ReportTabPath);
        patchMapTab = GetNode<MicrobeEditorPatchMap>(PatchMapTabPath);
        bodyPlanEditorTab = GetNode<CellBodyPlanEditorComponent>(BodyPlanEditorTabPath);
        cellEditorTab = GetNode<CellEditorComponent>(CellEditorTabPath);
        noCellTypeSelected = GetNode<Control>(NoCellTypeSelectedPath);
    }

    protected override void InitEditor(bool fresh)
    {
        patchMapTab.SetMap(CurrentGame.GameWorld.Map);

        base.InitEditor(fresh);

        reportTab.UpdateReportTabPatchSelector();

        reportTab.UpdateGlucoseReduction(CurrentGame.GameWorld.WorldSettings.GlucoseDecay);

        if (fresh)
        {
            CurrentGame.SetBool("edited_early_multicellular", true);
        }
        else
        {
            SendAutoEvoResultsToReportComponent();

            reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

            reportTab.UpdatePatchDetails(CurrentPatch, patchMapTab.SelectedPatch);
        }

        cellEditorTab.UpdateBackgroundImage(CurrentPatch.BiomeTemplate);

        // TODO: as we are a prototype we don't want to auto save
        wantsToSave = false;
    }

    protected override void InitEditorGUI(bool fresh)
    {
        reportTab.OnNextTab = () => SetEditorTab(EditorTab.PatchMap);
        patchMapTab.OnNextTab = () => SetEditorTab(EditorTab.CellEditor);
        bodyPlanEditorTab.OnFinish = ForwardEditorComponentFinishRequest;
        cellEditorTab.OnNextTab = () => SetEditorTab(EditorTab.CellEditor);

        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.Init(this, fresh);
        }

        patchMapTab.OnSelectedPatchChanged = OnSelectPatchForReportTab;
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
            reportTab.UpdateAutoEvoResults(TranslationServer.Translate("AUTO_EVO_FAILED"),
                TranslationServer.Translate("AUTO_EVO_RUN_STATUS") + " " + run.Status);
        }

        base.OnEditorReady();

        reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

        if (autoEvoSummary != null && autoEvoExternal != null)
        {
            reportTab.UpdateAutoEvoResults(autoEvoSummary.ToString(), autoEvoExternal.ToString());
        }

        reportTab.UpdatePatchDetails(CurrentPatch, patchMapTab.SelectedPatch);
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

    protected override void ElapseEditorEntryTime()
    {
        // TODO: select which units will be used for the master elapsed time counter
        CurrentGame.GameWorld.OnTimePassed(1);
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewEarlyMulticellularGame(new WorldGenerationSettings());
    }

    protected override void PerformAutoSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void SaveGame(string name)
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
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

                // TODO: fix the arrow positioning when switching tabs (it fixes itself only when placing something)
                // This line (and also in CellTypeEditor) doesn't help:
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

                    // TODO: check if this now has fixed the arrow positioning after tab change (see comment in the
                    // above case)
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
        var species = (EarlyMulticellularSpecies?)CurrentGame.GameWorld.PlayerSpecies;
        editedSpecies = species ?? throw new NullReferenceException("didn't find edited species");

        base.SetupEditedSpecies();
    }

    protected override void OnEditorExitTransitionFinished()
    {
        // Clear the edited cell type to avoid the cell editor applying the changes unnecessarily
        selectedCellTypeToEdit = null;

        base.OnEditorExitTransitionFinished();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ReportTabPath != null)
            {
                ReportTabPath.Dispose();
                PatchMapTabPath.Dispose();
                BodyPlanEditorTabPath.Dispose();
                CellEditorTabPath.Dispose();
                NoCellTypeSelectedPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnSelectPatchForReportTab(Patch patch)
    {
        reportTab.UpdatePatchDetails(patch, patch);
    }

    private void OnStartEditingCellType(string name)
    {
        if (CanCancelAction)
        {
            ToolTipManager.Instance.ShowPopup(
                TranslationServer.Translate("ACTION_BLOCKED_WHILE_ANOTHER_IN_PROGRESS"), 1.5f);
            return;
        }

        var newTypeToEdit = EditedSpecies.CellTypes.First(c => c.TypeName == name);

        // Only reinitialize the editor when required
        if (selectedCellTypeToEdit == null || selectedCellTypeToEdit != newTypeToEdit)
        {
            selectedCellTypeToEdit = newTypeToEdit;
            newCellTypeEditHasStarted = true;

            GD.Print("Start editing cell type: ", selectedCellTypeToEdit.TypeName);

            // Reinitialize the cell editor to be able to edit the new cell type
            cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
        }

        SetEditorTab(EditorTab.CellTypeEditor);
    }

    private void CheckAndApplyCellTypeEdit()
    {
        if (selectedCellTypeToEdit == null)
            return;

        // Only do something if the user has done any action in the past
        if (!history.CanUndo())
            return;

        // TODO: only apply if there were changes
        GD.Print("Creating cell type change action for type: ", selectedCellTypeToEdit.TypeName);

        // Combine the topmost action in the stack with this new one to make sure finishing editing a cell doesn't
        // cause a separate step
        var action = new CombinedEditorAction(history.PopTopAction(),
            new SingleEditorAction<EndCellTypeEditActionData>(DoEndCellTypeEditAction, UndoEndCellTypeEditAction,
                new EndCellTypeEditActionData(selectedCellTypeToEdit)));

        // We need to do this here to free up the MP that is now in the undone action, otherwise it won't succeed in
        // all cases
        DirtyMutationPointsCache();

        if (!EnqueueAction(action))
            GD.PrintErr("Combined action, with 0 cost added cell edit finish, could not be performed again");
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
            cellEditorTab.OnFinishEditing();

            // cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
            bodyPlanEditorTab.OnCellTypeEdited(selectedCellTypeToEdit);
        }
    }

    [DeserializedCallbackAllowed]
    private void DoEndCellTypeEditAction(EndCellTypeEditActionData data)
    {
        if (selectedCellTypeToEdit != data.FinishedCellType)
        {
            if (selectedCellTypeToEdit != null)
            {
                GD.Print("Previous cell type to edit needs cleaning up before applying this action");
                cellEditorTab.OnFinishEditing();
                bodyPlanEditorTab.OnCellTypeEdited(selectedCellTypeToEdit);
            }

            selectedCellTypeToEdit = data.FinishedCellType;
            newCellTypeEditHasStarted = true;
            cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
        }

        // We need to handle the renaming here as the cell editor doesn't really know what other cell types exist
        // so it can't check if the name is unique or not
        // TODO: would be nice to re-architecture this so that the cell editor could show if the new name is valid
        // or not
        var oldName = data.FinishedCellType.TypeName;

        cellEditorTab.OnFinishEditing();

        // Revert to old name if the name is a duplicate
        if (EditedSpecies.CellTypes.Any(c =>
                c != selectedCellTypeToEdit && c.TypeName == data.FinishedCellType.TypeName))
        {
            GD.Print("Cell editor renamed a cell type to a duplicate name, reverting");
            data.FinishedCellType.TypeName = oldName;
        }

        bodyPlanEditorTab.OnCellTypeEdited(data.FinishedCellType);
    }

    [DeserializedCallbackAllowed]
    private void UndoEndCellTypeEditAction(EndCellTypeEditActionData data)
    {
        if (selectedCellTypeToEdit == data.FinishedCellType)
            return;

        // Reinitialize the cell editor to be able to apply further undo operations to the previous type
        selectedCellTypeToEdit = data.FinishedCellType;
        cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
    }

    [DeserializedCallbackAllowed]
    private void DoStartCellTypeEditAction(StartCellTypeEditActionData data)
    {
        if (selectedCellTypeToEdit == data.StartedCellTypeEdit)
            return;

        // This fixes complex cases where multiple types are redoing actions
        selectedCellTypeToEdit = data.StartedCellTypeEdit;
        cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
    }

    [DeserializedCallbackAllowed]
    private void UndoStartCellTypeEditAction(StartCellTypeEditActionData data)
    {
    }
}
