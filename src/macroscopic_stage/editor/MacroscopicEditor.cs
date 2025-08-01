﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Environment = Godot.Environment;

/// <summary>
///   Macroscopic main editor class
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/macroscopic_stage/editor/MacroscopicEditor.tscn", UsesEarlyResolve = false)]
[DeserializedCallbackTarget]
public partial class MacroscopicEditor : EditorBase<EditorAction, MacroscopicStage>, IEditorReportData,
    ICellEditorData
{
#pragma warning disable CA2213
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    [Export]
    private MicrobeEditorReportComponent reportTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    [Export]
    private MicrobeEditorPatchMap patchMapTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    [Export]
    private MetaballBodyEditorComponent bodyPlanEditorTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    [Export]
    private CellEditorComponent cellEditorTab = null!;

    [Export]
    private MicrobeCamera cellEditorCamera = null!;

    [Export]
    private Light3D cellEditorLight = null!;

    [Export]
    private Camera3D body3DEditorCamera = null!;

    [Export]
    private Light3D bodyEditorLight = null!;

    [Export]
    private WorldEnvironment worldEnvironmentNode = null!;

    private Environment? environment;

    [Export]
    private Control noCellTypeSelected = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private MacroscopicSpecies? editedSpecies;

    [JsonProperty]
    private CellType? selectedCellTypeToEdit;

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
    public MacroscopicSpecies EditedSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    [JsonIgnore]
    public Patch CurrentPatch => patchMapTab.CurrentPatch;

    [JsonIgnore]
    public Patch? TargetPatch => patchMapTab.TargetPatch;

    [JsonIgnore]
    public Patch? SelectedPatch => patchMapTab.SelectedPatch;

    [JsonIgnore]
    public ICellDefinition? EditedCellProperties => selectedCellTypeToEdit;

    // TODO: same as multicellular editor, might be needed in the future to support tolerances editing
    [JsonIgnore]
    public IReadOnlyList<OrganelleTemplate>? EditedCellOrganelles => null;

    protected override string MusicCategory => "MacroscopicEditor";

    protected override MainGameState ReturnToState => MainGameState.MacroscopicStage;
    protected override string EditorLoadingMessage => Localization.Translate("LOADING_MACROSCOPIC_EDITOR");
    protected override bool HasInProgressAction => CanCancelAction;

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

        cellEditorTab.SetEditorWorldGuideObjectVisibility(shown && selectedEditorTab == EditorTab.CellTypeEditor);
        cellEditorTab.SetEditorWorldTabSpecificObjectVisibility(shown && selectedEditorTab == EditorTab.CellTypeEditor);

        bodyPlanEditorTab.SetEditorWorldGuideObjectVisibility(shown && selectedEditorTab == EditorTab.CellEditor);
        bodyPlanEditorTab.SetEditorWorldTabSpecificObjectVisibility(shown && selectedEditorTab == EditorTab.CellEditor);
    }

    public void OnCurrentPatchUpdated(Patch patch)
    {
        cellEditorTab.OnCurrentPatchUpdated(patch);

        UpdateBackgrounds(patch);
    }

    public override void AddContextToActions(IEnumerable<CombinableActionData> actions)
    {
        // If a cell type is being edited, add its type to each action data
        // so we can use it for undoing and redoing later
        if (selectedEditorTab == EditorTab.CellTypeEditor && selectedCellTypeToEdit != null)
        {
            foreach (var actionData in actions)
            {
                if (actionData is EditorCombinableActionData<CellType> cellTypeData && cellTypeData.Context == null)
                    cellTypeData.Context = selectedCellTypeToEdit;
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
        SwapEditingCellIfNeeded(cellType);

        // If the action we're redoing should be done on another editor tab, switch to that tab
        SwapEditorTabIfNeeded(history.ActionToRedo());

        base.Redo();
    }

    public override void Undo()
    {
        var cellType = history.GetUndoContext<CellType>();

        // If the action we're undoing should be done on another cell type,
        // save our changes to the current cell type, then switch to the other one
        SwapEditingCellIfNeeded(cellType);

        // If the action we're undoing should be done on another editor tab, switch to that tab
        SwapEditorTabIfNeeded(history.ActionToUndo());

        base.Undo();
    }

    public bool IsNewCellTypeNameValid(string newName)
    {
        // Name is invalid if it is empty or a duplicate
        // TODO: should this ensure the name doesn't have trailing whitespace?
        // If so, CellTemplate.UpdateNameIfValid should be updated as well
        return !string.IsNullOrWhiteSpace(newName) && !EditedSpecies.CellTypes.Any(c =>
            c.TypeName.Equals(newName, StringComparison.InvariantCultureIgnoreCase));
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
    }

    protected override void InitEditor(bool fresh)
    {
        patchMapTab.SetMap(CurrentGame.GameWorld.Map);

        base.InitEditor(fresh);

        reportTab.UpdateReportTabPatchSelector();

        if (fresh)
        {
            CurrentGame.SetBool("edited_macroscopic", true);
        }
        else
        {
            SendAutoEvoResultsToReportComponent();

            reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

            reportTab.UpdatePatchDetails(CurrentPatch, TargetPatch);

            reportTab.UpdateEvents(CurrentGame.GameWorld.EventsLog, CurrentGame.GameWorld.TotalPassedTime);
        }

        UpdateBackgrounds(CurrentPatch);

        // TODO: as we are a prototype we don't want to auto save
        wantsToSave = false;
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
    }

    protected override void OnUndoPerformed()
    {
        base.OnUndoPerformed();

        CheckDidActionAffectTissueTypes(history.ActionToRedo());
    }

    protected override void OnRedoPerformed()
    {
        base.OnRedoPerformed();

        CheckDidActionAffectTissueTypes(history.ActionToUndo());
    }

    protected override void UpdatePatchDetails()
    {
        // Patch events are able to change the stage's background so it needs to be updated here.
        cellEditorTab.UpdateBackgroundImage(CurrentPatch);
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewMacroscopicGame(new WorldGenerationSettings());
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

        RememberEnvironment();

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
                // If we have an edited cell type, then we can apply those changes when we go back to the main editor
                // tab as that's the only exit point and the point where we actually need to use the edited cell
                // type information
                // TODO: write an explanation here why this needs to be before the visibility adjustment
                // See: https://github.com/Revolutionary-Games/Thrive/pull/3457
                CheckAndApplyCellTypeEdit();

                // This doesn't need to be before CheckAndApplyCellTypeEdit like in multicellular editor as
                // this doesn't skip anything even if the tab is not visible yet
                bodyPlanEditorTab.Show();
                SetEditorObjectVisibility(true);

                bodyPlanEditorTab.UpdateArrow();

                // TODO: camera position saving
                // bodyPlanEditorTab.UpdateCamera();

                SetWorldSceneObjectVisibilityWeControl();

                ResetEnvironment();

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

                    cellEditorTab.UpdateArrow();
                    cellEditorTab.UpdateCamera();

                    SetWorldSceneObjectVisibilityWeControl();
                }

                worldEnvironmentNode.Environment = null;

                break;
            }

            default:
                throw new Exception("Invalid editor tab");
        }
    }

    protected override void SetupEditedSpecies()
    {
        var species = (MacroscopicSpecies?)CurrentGame.GameWorld.PlayerSpecies;
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
            environment?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateAutoEvoToReportTab()
    {
        if (autoEvoResults == null)
            throw new InvalidOperationException("May not be called without report");

        // This creates a new callable each time, but the garbage amount should be negligible
        reportTab.UpdateAutoEvoResults(autoEvoResults, autoEvoExternal?.ToString() ?? "error",
            () => autoEvoResults.MakeSummary(true));
    }

    private void UpdateBackgrounds(Patch patch)
    {
        cellEditorTab.UpdateBackgroundImage(patch);

        UpdateBackgroundPanorama(patch.BiomeTemplate);
    }

    private void UpdateBackgroundPanorama(Biome biome)
    {
        var sky = worldEnvironmentNode.Environment.Sky;
        var skyMaterial = (PanoramaSkyMaterial)sky.SkyMaterial;

        skyMaterial.Panorama = GD.Load<Texture2D>(biome.Panorama);

        // TODO: update colour properties if really wanted (right now white ambient light is used to see things better
        // in the editor)
    }

    private void SetWorldSceneObjectVisibilityWeControl()
    {
        bool cellEditor = selectedEditorTab == EditorTab.CellTypeEditor;
        bool bodyEditor = selectedEditorTab == EditorTab.CellEditor;

        // Set the right active camera
        if (cellEditor)
        {
            body3DEditorCamera.Current = false;
            cellEditorCamera.SetCustomCurrentStatus(true);
        }
        else
        {
            cellEditorCamera.SetCustomCurrentStatus(false);
            body3DEditorCamera.Current = true;
        }

        cellEditorLight.Visible = cellEditor;

        bodyEditorLight.Visible = bodyEditor;
    }

    private void OnStartEditingCellType(string? name)
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

        var newTypeToEdit = EditedSpecies.CellTypes.First(c => c.TypeName == name);

        // Only reinitialize the editor when required
        if (selectedCellTypeToEdit == null || selectedCellTypeToEdit != newTypeToEdit)
        {
            selectedCellTypeToEdit = newTypeToEdit;

            GD.Print("Start editing tissue type (cell type): ", selectedCellTypeToEdit.TypeName);

            // Reinitialize the cell editor to be able to edit the new cell type
            cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
        }

        SetEditorTab(EditorTab.CellTypeEditor);
    }

    private void CheckAndApplyCellTypeEdit()
    {
        if (selectedCellTypeToEdit == null)
            return;

        // TODO: only apply if there were changes
        GD.Print("Applying changes made to cell type: ", selectedCellTypeToEdit.TypeName);

        // Apply any changes made to the selected cell
        FinishEditingSelectedCell();
    }

    /// <summary>
    ///   Detects if a cell edit was done that requires us to re-apply metaball properties
    /// </summary>
    private void CheckDidActionAffectTissueTypes(EditorAction? action)
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
            GD.Print("Undone / redone action affected tissue types");
            cellEditorTab.OnFinishEditing();

            // cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
            bodyPlanEditorTab.OnTissueTypeEdited(selectedCellTypeToEdit);
        }
    }

    private void FinishEditingSelectedCell()
    {
        if (selectedCellTypeToEdit == null)
            return;

        // We need to handle the renaming here as the cell editor doesn't really know what other cell types exist
        // so it can't check if the name is unique or not
        // TODO: would be nice to re-architecture this so that the cell editor could show if the new name is valid
        // or not
        var oldName = selectedCellTypeToEdit.TypeName;

        cellEditorTab.OnFinishEditing();

        // Revert to old name if the name is a duplicate
        if (EditedSpecies.CellTypes.Any(c =>
                c != selectedCellTypeToEdit && c.TypeName == selectedCellTypeToEdit.TypeName))
        {
            GD.Print("Cell editor renamed a cell type to a duplicate name, reverting");
            selectedCellTypeToEdit.TypeName = oldName;
        }

        bodyPlanEditorTab.OnTissueTypeEdited(selectedCellTypeToEdit);
    }

    private void SwapEditingCellIfNeeded(CellType? newCell)
    {
        if (selectedCellTypeToEdit == newCell || newCell == null)
            return;

        // If we're switching to a new cell type, apply any changes made to the old one
        if (selectedEditorTab == EditorTab.CellTypeEditor && selectedCellTypeToEdit != null)
            FinishEditingSelectedCell();

        // This fixes complex cases where multiple types are undoing and redoing actions
        selectedCellTypeToEdit = newCell;
        cellEditorTab.OnEditorSpeciesSetup(EditedBaseSpecies);
    }

    private void SwapEditorTabIfNeeded(EditorAction? editorAction)
    {
        if (editorAction == null)
            return;

        var actionData = editorAction.Data.FirstOrDefault();

        EditorTab targetTab;

        // If the action was performed on a single Cell Type, target the Cell Type Editor tab
        if (actionData is EditorCombinableActionData<CellType>)
        {
            targetTab = EditorTab.CellTypeEditor;
        }
        else if (actionData != null && bodyPlanEditorTab.IsMetaballAction(actionData))
        {
            targetTab = EditorTab.CellEditor;
        }
        else
        {
            return;
        }

        // If we're already on the selected tab, there's no need to do anything
        if (targetTab == selectedEditorTab)
            return;

        SetEditorTab(targetTab);
    }

    private void RememberEnvironment()
    {
        if (worldEnvironmentNode.Environment != null)
        {
            environment = worldEnvironmentNode.Environment;
        }
    }

    private void ResetEnvironment()
    {
        if (environment != null)
        {
            worldEnvironmentNode.Environment = environment;
        }
    }
}
