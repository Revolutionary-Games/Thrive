using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/late_multicellular_stage/editor/LateMulticellularEditor.tscn")]
[DeserializedCallbackTarget]
public class LateMulticellularEditor : EditorBase<EditorAction, MulticellularStage>, IEditorReportData, ICellEditorData
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

    [Export]
    public NodePath CellEditorCameraPath = null!;

    [Export]
    public NodePath CellEditorLightPath = null!;

    [Export]
    public NodePath Body3DEditorCameraPath = null!;

    [Export]
    public NodePath BodyEditorLightPath = null!;

    [Export]
    public NodePath WorldEnvironmentNodePath = null!;

#pragma warning disable CA2213
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorReportComponent reportTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorPatchMap patchMapTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MetaballBodyEditorComponent bodyPlanEditorTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellEditorComponent cellEditorTab = null!;

    private MicrobeCamera cellEditorCamera = null!;
    private Light cellEditorLight = null!;

    private Camera body3DEditorCamera = null!;
    private Light bodyEditorLight = null!;

    private WorldEnvironment worldEnvironmentNode = null!;

    private Control noCellTypeSelected = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private LateMulticellularSpecies? editedSpecies;

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
    public LateMulticellularSpecies EditedSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    [JsonIgnore]
    public Patch CurrentPatch => patchMapTab.CurrentPatch;

    [JsonIgnore]
    public Patch? SelectedPatch => patchMapTab.SelectedPatch;

    [JsonIgnore]
    public ICellProperties? EditedCellProperties => selectedCellTypeToEdit;

    protected override string MusicCategory => "LateMulticellularEditor";

    protected override MainGameState ReturnToState => MainGameState.MulticellularStage;
    protected override string EditorLoadingMessage => TranslationServer.Translate("LOADING_MULTICELLULAR_EDITOR");
    protected override bool HasInProgressAction => CanCancelAction;

    public void SendAutoEvoResultsToReportComponent()
    {
        reportTab.UpdateAutoEvoResults(autoEvoSummary?.ToString() ?? "error", autoEvoExternal?.ToString() ?? "error");
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

        reportTab.UpdatePatchDetails(patch);

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

        base.Redo();
    }

    public override void Undo()
    {
        var cellType = history.GetUndoContext<CellType>();

        // If the action we're undoing should be done on another cell type,
        // save our changes to the current cell type, then switch to the other one
        SwapEditingCellIfNeeded(cellType);

        base.Undo();
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
        reportTab = GetNode<MicrobeEditorReportComponent>(ReportTabPath);
        patchMapTab = GetNode<MicrobeEditorPatchMap>(PatchMapTabPath);
        bodyPlanEditorTab = GetNode<MetaballBodyEditorComponent>(BodyPlanEditorTabPath);
        cellEditorTab = GetNode<CellEditorComponent>(CellEditorTabPath);
        noCellTypeSelected = GetNode<Control>(NoCellTypeSelectedPath);

        cellEditorCamera = GetNode<MicrobeCamera>(CellEditorCameraPath);
        cellEditorLight = GetNode<Light>(CellEditorLightPath);

        worldEnvironmentNode = GetNode<WorldEnvironment>(WorldEnvironmentNodePath);

        body3DEditorCamera = GetNode<Camera>(Body3DEditorCameraPath);
        bodyEditorLight = GetNode<Light>(BodyEditorLightPath);
    }

    protected override void InitEditor(bool fresh)
    {
        patchMapTab.SetMap(CurrentGame.GameWorld.Map);

        base.InitEditor(fresh);

        reportTab.UpdateReportTabPatchSelector();

        reportTab.UpdateGlucoseReduction(CurrentGame.GameWorld.WorldSettings.GlucoseDecay);

        if (fresh)
        {
            CurrentGame.SetBool("edited_late_multicellular", true);
        }
        else
        {
            SendAutoEvoResultsToReportComponent();

            reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

            reportTab.UpdatePatchDetails(CurrentPatch, patchMapTab.SelectedPatch);
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

        CheckDidActionAffectTissueTypes(history.ActionToRedo());
    }

    protected override void OnRedoPerformed()
    {
        base.OnRedoPerformed();

        CheckDidActionAffectTissueTypes(history.ActionToUndo());
    }

    protected override void ElapseEditorEntryTime()
    {
        // TODO: select which units will be used for the master elapsed time counter
        CurrentGame.GameWorld.OnTimePassed(1);
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewLateMulticellularGame(new WorldGenerationSettings());
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
                // If we have an edited cell type, then we can apply those changes when we go back to the main editor
                // tab as that's the only exit point and the point where we actually need to use the edited cell
                // type information
                // TODO: write an explanation here why this needs to be before the visibility adjustment
                // See: https://github.com/Revolutionary-Games/Thrive/pull/3457
                CheckAndApplyCellTypeEdit();

                // This doesn't need to be before CheckAndApplyCellTypeEdit like in early multicellular editor as
                // this doesn't skip anything even if the tab is not visible yet
                bodyPlanEditorTab.Show();
                SetEditorObjectVisibility(true);

                bodyPlanEditorTab.UpdateArrow();

                // TODO: camera position saving
                // bodyPlanEditorTab.UpdateCamera();

                SetWorldSceneObjectVisibilityWeControl();

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

                break;
            }

            default:
                throw new Exception("Invalid editor tab");
        }
    }

    protected override void SetupEditedSpecies()
    {
        var species = (LateMulticellularSpecies?)CurrentGame.GameWorld.PlayerSpecies;
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
                CellEditorCameraPath.Dispose();
                CellEditorLightPath.Dispose();
                WorldEnvironmentNodePath.Dispose();
                Body3DEditorCameraPath.Dispose();
                BodyEditorLightPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnSelectPatchForReportTab(Patch patch)
    {
        reportTab.UpdatePatchDetails(patch, patch);
    }

    private void UpdateBackgrounds(Patch patch)
    {
        cellEditorTab.UpdateBackgroundImage(patch.BiomeTemplate);

        UpdateBackgroundPanorama(patch.BiomeTemplate);
    }

    private void UpdateBackgroundPanorama(Biome biome)
    {
        var worldPanoramaSky = (PanoramaSky)worldEnvironmentNode.Environment.BackgroundSky;

        worldPanoramaSky.Panorama = GD.Load<Texture>(biome.Panorama);

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
}
