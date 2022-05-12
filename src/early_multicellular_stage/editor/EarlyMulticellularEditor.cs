using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/early_multicellular_stage/editor/EarlyMulticellularEditor.tscn")]
public class EarlyMulticellularEditor : EditorBase<EditorAction, MicrobeStage>, IEditorReportData, ICellEditorData
{
    [Export]
    public NodePath ReportTabPath = null!;

    [Export]
    public NodePath PatchMapTabPath = null!;

    [Export]
    public NodePath BodyPlanEditorTabPath = null!;

    [Export]
    public NodePath CellEditorTabPath = null!;

    [Export]
    public NodePath NoCellTypeSelectedPath = null!;

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

    [JsonProperty]
    private EarlyMulticellularSpecies? editedSpecies;

    private Control noCellTypeSelected = null!;

    [JsonProperty]
    private CellType? selectedCellTypeToEdit;

    public override bool CanCancelAction => cellEditorTab.Visible && cellEditorTab.CanCancelAction;

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
    protected override string EditorLoadingMessage => TranslationServer.Translate("LOADING_MICROBE_EDITOR");
    protected override bool HasInProgressAction { get; }

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
        cellEditorTab.CalculateOrganelleEffectivenessInPatch(patch);
        cellEditorTab.UpdatePatchDependentBalanceData();

        reportTab.UpdateReportTabPatchSelectorSelection(patch.ID);
        cellEditorTab.UpdateBackgroundImage(patch.BiomeTemplate);
    }

    public override int WhatWouldActionsCost(IEnumerable<EditorCombinableActionData> actions)
    {
        // TODO: action history. Once we have also body plan actions we need a way to only apply the discount to one
        // type
        return (int)(history.WhatWouldActionsCost(actions) * Constants.MULTICELLULAR_EDITOR_COST_FACTOR);
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

    protected override void UpdateHistoryCallbackTargets(ActionHistory<EditorAction> actionHistory)
    {
        // See TODO comment in MicrobeEditor.UpdateHistoryCallbackTargets
    }

    protected override void InitEditor(bool fresh)
    {
        patchMapTab.SetMap(CurrentGame.GameWorld.Map);

        base.InitEditor(fresh);

        reportTab.UpdateReportTabPatchSelector();

        // TODO: this should be saved so that the text can be accurate if this is updated
        reportTab.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);

        if (fresh)
        {
            CurrentGame.SetBool("edited_early_multicellular", true);
        }
        else
        {
            SendAutoEvoResultsToReportComponent();

            reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

            reportTab.UpdateReportTabStatistics(CurrentPatch);
            reportTab.UpdateTimeline(patchMapTab.SelectedPatch);
        }

        cellEditorTab.UpdateBackgroundImage(CurrentPatch.BiomeTemplate);

        // TODO: as we are a prototype we don't want to auto save
        wantsToSave = false;
    }

    protected override IEnumerable<IEditorComponent> GetAllEditorComponents()
    {
        yield return reportTab;
        yield return patchMapTab;
        yield return bodyPlanEditorTab;
        yield return cellEditorTab;
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

        reportTab.UpdateReportTabStatistics(CurrentPatch);
        reportTab.UpdateTimeline(patchMapTab.SelectedPatch);
    }

    protected override void ElapseEditorEntryTime()
    {
        // TODO: select which units will be used for the master elapsed time counter
        CurrentGame.GameWorld.OnTimePassed(1);
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewEarlyMulticellularGame();
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
                bodyPlanEditorTab.Show();
                SetEditorObjectVisibility(true);
                cellEditorTab.SetEditorWorldTabSpecificObjectVisibility(false);
                bodyPlanEditorTab.SetEditorWorldTabSpecificObjectVisibility(true);

                // TODO: fix the arrow positioning when switching tabs (it fixes itself only when placing something)
                // This line (and also in CellTypeEditor) doesn't help:
                bodyPlanEditorTab.UpdateArrow();
                bodyPlanEditorTab.UpdateCamera();

                // If we have an edited cell type, then we can apply those changes when we go back to the main editor
                // tab as that's the only exit point and the point where we actually need to use the edited cell
                // type information
                CheckAndApplyCellTypeEdit();

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

    private void OnStartEditingCellType(string name)
    {
        selectedCellTypeToEdit = EditedSpecies.CellTypes.First(c => c.TypeName == name);

        GD.Print("Start editing cell type: ", selectedCellTypeToEdit.TypeName);

        SetEditorTab(EditorTab.CellTypeEditor);

        // Reinitialize the cell editor to be able to edit the new cell type
        cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
    }

    private void CheckAndApplyCellTypeEdit()
    {
        if (selectedCellTypeToEdit == null)
            return;

        // TODO: only apply if there were changes
        GD.Print("Applying changes to cell type: ", selectedCellTypeToEdit.TypeName);

        // We need to handle the renaming here as the cell editor doesn't really know what other cell types exist
        // so it can't check if the name is unique or not
        // TODO: would be nice to re-architecture this so that the cell editor could show if the new name is valid
        // or not
        var oldName = selectedCellTypeToEdit.TypeName;

        // TODO: this should be converted to be an editor action to not crash when undoing / redoing organelle
        // placements after re-entering the cell editor
        // For now just nuke the history
        history.Nuke();
        NotifyUndoRedoStateChanged();

        cellEditorTab.OnFinishEditing();

        // Revert to old name if the name is a duplicate
        if (EditedSpecies.CellTypes.Any(c =>
                c != selectedCellTypeToEdit && c.TypeName == selectedCellTypeToEdit.TypeName))
        {
            GD.Print("Cell editor renamed a cell type to a duplicate name, reverting");
            selectedCellTypeToEdit.TypeName = oldName;
        }

        bodyPlanEditorTab.OnCellTypeEdited(selectedCellTypeToEdit);
    }
}
