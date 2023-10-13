using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class of the microbe editor
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditor.tscn")]
public class MicrobeEditor : EditorBase<EditorAction, MicrobeStage>, IEditorReportData, ICellEditorData
{
    [Export]
    public NodePath? ReportTabPath;

    [Export]
    public NodePath PatchMapTabPath = null!;

    [Export]
    public NodePath CellEditorTabPath = null!;

#pragma warning disable CA2213
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorReportComponent reportTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorPatchMap patchMapTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellEditorComponent cellEditorTab = null!;

    private MicrobeEditorTutorialGUI tutorialGUI = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   The species that is being edited, changes are applied to it on exit
    /// </summary>
    [JsonProperty]
    private MicrobeSpecies? editedSpecies;

    public override bool CanCancelAction => cellEditorTab.Visible && cellEditorTab.CanCancelAction;

    [JsonIgnore]
    public override Species EditedBaseSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    [JsonIgnore]
    public ICellProperties EditedCellProperties =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    [JsonIgnore]
    public Patch CurrentPatch => patchMapTab.CurrentPatch;

    [JsonIgnore]
    public Patch? SelectedPatch => patchMapTab.SelectedPatch;

    protected override string MusicCategory => "MicrobeEditor";

    protected override MainGameState ReturnToState => MainGameState.MicrobeStage;
    protected override string EditorLoadingMessage => TranslationServer.Translate("LOADING_MICROBE_EDITOR");
    protected override bool HasInProgressAction => CanCancelAction;

    public override void _Ready()
    {
        base._Ready();

        tutorialGUI.Visible = true;
    }

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
        cellEditorTab.OnCurrentPatchUpdated(patch);

        reportTab.UpdatePatchDetails(patch);

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

    public override void AddContextToActions(IEnumerable<CombinableActionData> editorActions)
    {
        // Microbe editor doesn't require any context data in actions
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
        reportTab = GetNode<MicrobeEditorReportComponent>(ReportTabPath);
        patchMapTab = GetNode<MicrobeEditorPatchMap>(PatchMapTabPath);
        cellEditorTab = GetNode<CellEditorComponent>(CellEditorTabPath);
        tutorialGUI = GetNode<MicrobeEditorTutorialGUI>("TutorialGUI");
    }

    protected override void InitEditor(bool fresh)
    {
        patchMapTab.SetMap(CurrentGame.GameWorld.Map);

        base.InitEditor(fresh);

        reportTab.UpdateReportTabPatchSelector();

        reportTab.UpdateGlucoseReduction(CurrentGame.GameWorld.WorldSettings.GlucoseDecay);

        if (fresh)
        {
            CurrentGame.SetBool("edited_microbe", true);
        }
        else
        {
            SendAutoEvoResultsToReportComponent();

            reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

            reportTab.UpdatePatchDetails(CurrentPatch, patchMapTab.SelectedPatch);
        }

        cellEditorTab.UpdateBackgroundImage(CurrentPatch.BiomeTemplate);

        // Make tutorials run
        cellEditorTab.TutorialState = TutorialState;
        tutorialGUI.EventReceiver = TutorialState;
        pauseMenu.GameProperties = CurrentGame;

        // Send undo button to the tutorial system
        cellEditorTab.SendUndoRedoToTutorial(TutorialState);
    }

    protected override void InitEditorGUI(bool fresh)
    {
        reportTab.OnNextTab = () => SetEditorTab(EditorTab.PatchMap);
        patchMapTab.OnNextTab = () => SetEditorTab(EditorTab.CellEditor);
        cellEditorTab.OnFinish = ForwardEditorComponentFinishRequest;

        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.Init(this, fresh);
        }

        patchMapTab.OnSelectedPatchChanged = OnSelectPatchForReportTab;
    }

    protected override void OnEnterEditor()
    {
        base.OnEnterEditor();

        if (!IsLoadedFromSave)
            TutorialState.SendEvent(TutorialEventType.EnteredMicrobeEditor, EventArgs.Empty, this);
    }

    protected override void UpdateHistoryCallbackTargets(ActionHistory<EditorAction> actionHistory)
    {
        // TODO: figure out why the callbacks are correctly pointing to the cell editor instance even without this
        // actionHistory.ReTargetCallbacksInHistory(cellEditorTab);
    }

    protected override IEnumerable<IEditorComponent> GetAllEditorComponents()
    {
        yield return reportTab;
        yield return patchMapTab;
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

    protected override void ElapseEditorEntryTime()
    {
        // TODO: select which units will be used for the master elapsed time counter
        CurrentGame.GameWorld.OnTimePassed(1);
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
    }

    protected override void OnUndoPerformed()
    {
        base.OnUndoPerformed();
        TutorialState.SendEvent(TutorialEventType.MicrobeEditorUndo, EventArgs.Empty, this);
    }

    protected override void OnRedoPerformed()
    {
        base.OnRedoPerformed();
        TutorialState.SendEvent(TutorialEventType.MicrobeEditorRedo, EventArgs.Empty, this);
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
        // This now triggers also when loading the editor initially, but no tutorial relies on the player going back
        // to the report tab so this shouldn't matter
        TutorialState.SendEvent(TutorialEventType.MicrobeEditorTabChanged,
            new StringEventArgs(selectedEditorTab.ToString()),
            this);

        // Hide all
        reportTab.Hide();
        patchMapTab.Hide();
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
                cellEditorTab.Show();
                SetEditorObjectVisibility(true);
                cellEditorTab.UpdateCamera();
                break;
            }

            default:
                throw new Exception("Invalid editor tab");
        }
    }

    protected override void SetupEditedSpecies()
    {
        var species = (MicrobeSpecies?)CurrentGame.GameWorld.PlayerSpecies;
        editedSpecies = species ?? throw new NullReferenceException("didn't find edited species");

#pragma warning disable 162

        // Disabled warning as this is a tweak constant
        // ReSharper disable ConditionIsAlwaysTrueOrFalse HeuristicUnreachableCode
        if (Constants.CREATE_COPY_OF_EDITED_SPECIES)
        {
            // Create a mutated version of the current species code to compete against the player
            CreateMutatedSpeciesCopy(species);
        }

        // ReSharper restore ConditionIsAlwaysTrueOrFalse HeuristicUnreachableCode
#pragma warning restore 162

        base.SetupEditedSpecies();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ReportTabPath != null)
            {
                ReportTabPath.Dispose();
                PatchMapTabPath.Dispose();
                CellEditorTabPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnSelectPatchForReportTab(Patch patch)
    {
        reportTab.UpdatePatchDetails(patch, patch);
    }

    private void CreateMutatedSpeciesCopy(Species species)
    {
        var newSpecies = CurrentGame.GameWorld.CreateMutatedSpecies(species);

        var random = new Random();

        var population = random.Next(Constants.INITIAL_SPLIT_POPULATION_MIN,
            Constants.INITIAL_SPLIT_POPULATION_MAX + 1);

        if (!CurrentGame.GameWorld.Map.CurrentPatch!.AddSpecies(newSpecies, population))
        {
            GD.PrintErr("Failed to create a mutated version of the edited species");
        }
    }
}
