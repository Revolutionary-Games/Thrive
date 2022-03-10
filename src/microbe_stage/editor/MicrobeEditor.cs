using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class of the microbe editor
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditor.tscn")]
[DeserializedCallbackTarget]
public class MicrobeEditor : EditorBase<MicrobeEditorAction, MicrobeStage>, IEditorWithPatches, IHexEditor,
    IEditorWithActions
{
    [Export]
    public NodePath ReportTabPath = null!;

    [Export]
    public NodePath PatchMapTabPath = null!;

    [Export]
    public NodePath CellEditorTabPath = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorReportComponent reportTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private PatchMapEditorComponent<MicrobeEditor> patchMapTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellEditorComponent cellEditorTab = null!;

    private MicrobeEditorTutorialGUI tutorialGUI = null!;

    /// <summary>
    ///   The species that is being edited, changes are applied to it on exit
    /// </summary>
    [JsonProperty]
    private MicrobeSpecies? editedSpecies;

    [JsonIgnore]
    public TutorialState TutorialState => CurrentGame?.TutorialState ??
        throw new InvalidOperationException("Editor doesn't have current game set yet");

    public override bool CanCancelAction => cellEditorTab.Visible && cellEditorTab.CanCancelAction;

    [JsonIgnore]
    public override Species EditedBaseSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    public override bool CancelCurrentAction()
    {
        if (!cellEditorTab.Visible)
        {
            GD.PrintErr("No action to cancel");
            return false;
        }

        return cellEditorTab.CancelCurrentAction();
    }

    [JsonIgnore]
    public MicrobeSpecies EditedSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    protected override string MusicCategory => "MicrobeEditor";

    protected override MainGameState ReturnToState => MainGameState.MicrobeStage;
    protected override string EditorLoadingMessage => TranslationServer.Translate("LOADING_MICROBE_EDITOR");
    protected override bool HasInProgressAction { get; }

    public override void _Ready()
    {
        base._Ready();

        tutorialGUI.Visible = true;

        // Make starting from the editor work
        // InitEditor();
    }

    protected override IEnumerable<IEditorComponent> GetAllEditorComponents()
    {
        yield return reportTab;
        yield return patchMapTab;
        yield return cellEditorTab;
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

        // Set the right tabs if they aren't the defaults
        ApplyEditorTab();
    }

    public Patch CurrentPatch => patchMapTab.CurrentPatch;
    public Patch? SelectedPatch => patchMapTab.SelectedPatch;

    public void OnCurrentPatchUpdated(Patch patch)
    {
        cellEditorTab.CalculateOrganelleEffectivenessInPatch(patch);
        cellEditorTab.UpdatePatchDependentBalanceData();

        reportTab.UpdateReportTabPatchSelectorSelection(patch.ID);
        cellEditorTab.UpdateBackgroundImage(patch.BiomeTemplate);
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
        reportTab = GetNode<MicrobeEditorReportComponent>(ReportTabPath);
        patchMapTab = GetNode<MicrobeEditorPatchMap>(PatchMapTabPath);
        cellEditorTab = GetNode<CellEditorComponent>(CellEditorTabPath);
        tutorialGUI = GetNode<MicrobeEditorTutorialGUI>("TutorialGUI");
    }

    protected override void OnEnterEditor()
    {
        base.OnEnterEditor();

        if (!IsLoadedFromSave)
            TutorialState.SendEvent(TutorialEventType.EnteredMicrobeEditor, EventArgs.Empty, this);
    }

    protected override void InitEditor(bool fresh)
    {
        base.InitEditor(fresh);

        patchMapTab.SetMap(CurrentGame.GameWorld.Map);

        reportTab.UpdateReportTabPatchSelector();

        // TODO: this should be saved so that the text can be accurate if this is updated
        reportTab.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);

        if (fresh)
        {
            CurrentGame.SetBool("edited_microbe", true);
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

        // Make tutorials run
        cellEditorTab.TutorialState = TutorialState;
        tutorialGUI.EventReceiver = TutorialState;
        pauseMenu.GameProperties = CurrentGame;

        // Send undo button to the tutorial system
        cellEditorTab.SendUndoToTutorial(TutorialState);
    }

    public void SendAutoEvoResultsToReportComponent()
    {
        reportTab.UpdateAutoEvoResults(autoEvoSummary?.ToString() ?? "error", autoEvoExternal?.ToString() ?? "error");
    }

    protected override void OnEditorReady()
    {
        // The base method stores the data, so we just need to update the GUI here (in case of failure)
        var run = CurrentGame!.GameWorld.GetAutoEvoRun();

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
        CurrentGame!.GameWorld.OnTimePassed(1);
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewMicrobeGame();
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
