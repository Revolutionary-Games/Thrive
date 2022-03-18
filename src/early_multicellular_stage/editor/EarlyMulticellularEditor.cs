using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/early_multicellular_stage/editor/EarlyMulticellularEditor.tscn")]
public class EarlyMulticellularEditor : EditorBase<CellEditorAction, MicrobeStage>, IEditorWithPatches, IHexEditor,
    IEditorWithActions, IEditorReportData, ICellEditorData
{
    [Export]
    public NodePath ReportTabPath = null!;

    [Export]
    public NodePath PatchMapTabPath = null!;

    [Export]
    public NodePath BodyPlanEditorTabPath = null!;

    [Export]
    public NodePath CellEditorTabPath = null!;

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


    // TODO: implement this
    [JsonIgnore]
    public ICellProperties EditedCellProperties { get => throw new NotImplementedException(); }

    // TODO: add multicellular music tracks
    protected override string MusicCategory => "MicrobeEditor";

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

        cellEditorTab.SetEditorWorldGuideObjectVisibility(shown);
    }

    public void OnCurrentPatchUpdated(Patch patch)
    {
        cellEditorTab.CalculateOrganelleEffectivenessInPatch(patch);
        cellEditorTab.UpdatePatchDependentBalanceData();

        reportTab.UpdateReportTabPatchSelectorSelection(patch.ID);
        cellEditorTab.UpdateBackgroundImage(patch.BiomeTemplate);
    }

    public override bool CancelCurrentAction()
    {
        if (!cellEditorTab.Visible)
        {
            GD.PrintErr("No action to cancel");
            return false;
        }

        return cellEditorTab.CancelCurrentAction();
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
        reportTab = GetNode<MicrobeEditorReportComponent>(ReportTabPath);
        patchMapTab = GetNode<MicrobeEditorPatchMap>(PatchMapTabPath);
        bodyPlanEditorTab = GetNode<CellBodyPlanEditorComponent>(BodyPlanEditorTabPath);
        cellEditorTab = GetNode<CellEditorComponent>(CellEditorTabPath);
    }

    protected override void UpdateHistoryCallbackTargets(ActionHistory<CellEditorAction> actionHistory)
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
            // The error conditions here probably shouldn't be able to trigger at all
            SendAutoEvoResultsToReportComponent();

            reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

            reportTab.UpdateReportTabStatistics(CurrentPatch);
            reportTab.UpdateTimeline(patchMapTab.SelectedPatch);
        }

        cellEditorTab.UpdateBackgroundImage(CurrentPatch.BiomeTemplate);
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
                cellEditorTab.SetEditorWorldGuideObjectVisibility(false);
                bodyPlanEditorTab.SetEditorWorldGuideObjectVisibility(true);

                bodyPlanEditorTab.UpdateCamera();
                break;
            }

            case EditorTab.CellTypeEditor:
            {
                // TODO: show the "select a cell type" text if not selected yet instead of the cell editor
                throw new NotImplementedException();

                cellEditorTab.Show();
                SetEditorObjectVisibility(true);
                cellEditorTab.SetEditorWorldGuideObjectVisibility(true);
                bodyPlanEditorTab.SetEditorWorldGuideObjectVisibility(false);

                cellEditorTab.UpdateCamera();
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

}
