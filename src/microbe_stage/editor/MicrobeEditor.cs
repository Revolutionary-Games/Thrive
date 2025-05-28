﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class of the microbe editor
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditor.tscn", UsesEarlyResolve = false)]
public partial class MicrobeEditor : EditorBase<EditorAction, MicrobeStage>, IEditorReportData, ICellEditorData
{
    private const string ADVANCED_TABS_SHOWN_BEFORE = "editor_advanced_tabs";

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
    public ICellDefinition EditedCellProperties =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    [JsonIgnore]
    public IReadOnlyList<OrganelleTemplate> EditedCellOrganelles => cellEditorTab.GetLatestEditedOrganelles();

    [JsonIgnore]
    public Patch CurrentPatch => patchMapTab.CurrentPatch;

    [JsonIgnore]
    public Patch? TargetPatch => patchMapTab.TargetPatch;

    [JsonIgnore]
    public Patch? SelectedPatch => patchMapTab.SelectedPatch;

    protected override string MusicCategory => "MicrobeEditor";

    protected override MainGameState ReturnToState => MainGameState.MicrobeStage;
    protected override string EditorLoadingMessage => Localization.Translate("LOADING_MICROBE_EDITOR");
    protected override bool HasInProgressAction => CanCancelAction;

    public override void _Ready()
    {
        base._Ready();

        tutorialGUI.Visible = true;
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

        if (currentGame != null)
        {
            TutorialState.EditorRedoTutorial.OnOpened -= OnShowStatisticsForTutorial;
            TutorialState.EditorTutorialEnd.OnOpened -= cellEditorTab.ShowBasicEditingTabs;
            TutorialState.EditorTutorialEnd.OnClosed -= OnShowConfirmForTutorial;

            // Tab bar fiddling
            TutorialState.AtpBalanceIntroduction.OnClosed -= ShowTabBarAfterTutorial;
            TutorialState.AutoEvoPrediction.OnClosed -= ShowTabBarAfterTutorial;
            TutorialState.StaySmallTutorial.OnClosed -= ShowTabBarAfterTutorial;
        }
    }

    public void SendAutoEvoResultsToReportComponent()
    {
        if (autoEvoResults == null)
        {
            reportTab.ShowErrorAboutOldSave();
            return;
        }

        UpdateAutoEvoToReportTab();
    }

    public override void SetEditorObjectVisibility(bool shown)
    {
        base.SetEditorObjectVisibility(shown);

        cellEditorTab.SetEditorWorldGuideObjectVisibility(shown);
    }

    public void OnCurrentPatchUpdated(Patch patch)
    {
        cellEditorTab.OnCurrentPatchUpdated(patch);

        cellEditorTab.UpdateBackgroundImage(patch);
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

    public override bool OnFinishEditing(List<EditorUserOverride>? overrides = null)
    {
        var result = base.OnFinishEditing(overrides);

        if (result)
        {
            // Remember if advanced cell editor tabs have been seen for tutorial purposes
            if (cellEditorTab.AreAdvancedTabsVisible())
                CurrentGame.SetBool(ADVANCED_TABS_SHOWN_BEFORE, true);
        }

        return result;
    }

    public override void AddContextToActions(IEnumerable<CombinableActionData> editorActions)
    {
        // Microbe editor doesn't require any context data in actions
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
        tutorialGUI = GetNode<MicrobeEditorTutorialGUI>("TutorialGUI");
    }

    protected override void InitEditor(bool fresh)
    {
        patchMapTab.SetMap(CurrentGame.GameWorld.Map);

        // Register showing certain parts of the GUI as the tutorial progresses
        TutorialState.EditorRedoTutorial.OnOpened += OnShowStatisticsForTutorial;
        TutorialState.EditorTutorialEnd.OnOpened += cellEditorTab.ShowBasicEditingTabs;
        TutorialState.EditorTutorialEnd.OnClosed += OnShowConfirmForTutorial;

        // Tab bar fiddling
        TutorialState.AtpBalanceIntroduction.OnClosed += ShowTabBarAfterTutorial;
        TutorialState.AutoEvoPrediction.OnClosed += ShowTabBarAfterTutorial;
        TutorialState.StaySmallTutorial.OnClosed += ShowTabBarAfterTutorial;

        base.InitEditor(fresh);

        reportTab.UpdateReportTabPatchSelector();

        if (fresh)
        {
            CurrentGame.SetBool("edited_microbe", true);
        }
        else
        {
            SendAutoEvoResultsToReportComponent();

            reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

            reportTab.UpdatePatchDetails(CurrentPatch, TargetPatch);

            reportTab.UpdateEvents(CurrentGame.GameWorld.EventsLog, CurrentGame.GameWorld.TotalPassedTime);
        }

        ProceduralDataCache.Instance.OnEnterState(MainGameState.MicrobeEditor);

        cellEditorTab.UpdateBackgroundImage(CurrentPatch);

        // Make tutorials run
        cellEditorTab.TutorialState = TutorialState;
        tutorialGUI.EventReceiver = TutorialState;
        pauseMenu.GameProperties = CurrentGame;

        // Send highlighted controls to the tutorial system
        cellEditorTab.SendObjectsToTutorials(TutorialState, tutorialGUI);
    }

    protected override void InitEditorGUI(bool fresh)
    {
        if (TutorialState.Enabled && !TutorialState.EditorReportWelcome.Complete)
        {
            GD.Print("Will skip patch map tab for tutorial purposes");
            reportTab.OnNextTab = () => SetEditorTab(EditorTab.CellEditor);
        }
        else
        {
            reportTab.OnNextTab = () => SetEditorTab(EditorTab.PatchMap);
        }

        patchMapTab.OnNextTab = () => SetEditorTab(EditorTab.CellEditor);
        cellEditorTab.OnFinish = ForwardEditorComponentFinishRequest;

        foreach (var editorComponent in GetAllEditorComponents())
        {
            editorComponent.Init(this, fresh);
        }
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
            reportTab.DisplayAutoEvoFailure(run.Status);
        }
        else
        {
            // Need to pass the auto-evo
            // TODO: in the future when the report tab is redone, it will need the full info so this is for now a bit
            // non-extendable way to get this one piece of data stored

            cellEditorTab.PreviousPlayerGatheredEnergy = run.Results.GetPatchEnergyResults(EditedBaseSpecies)
                .Sum(p => p.Value.TotalEnergyGathered);
        }

        base.OnEditorReady();

        reportTab.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

        if (autoEvoResults != null && autoEvoExternal != null)
        {
            UpdateAutoEvoToReportTab();
        }
        else if (autoEvoExternal != null)
        {
            // This condition should never happen, but I'll leave this print here in case anyone ever hits this and
            // sends us a bug report -hhyyrylainen
            GD.PrintErr("Somehow auto-evo results are null but external effects text exists");
            reportTab.DisplayAutoEvoFailure("Only external effects is set but auto-evo results are missing");
        }

        reportTab.UpdatePatchDetails(CurrentPatch, TargetPatch);

        reportTab.UpdateEvents(CurrentGame.GameWorld.EventsLog, CurrentGame.GameWorld.TotalPassedTime);

        patchMapTab.UpdatePatchEvents();

        if (TutorialState.Enabled)
        {
            if (editorTabSelector == null)
                throw new InvalidOperationException("Editor GUI not setup");

            // Tutorial handling
            // On the first go, go directly to the cell editor tab
            if (!TutorialState.CellEditorIntroduction.Complete && !TutorialState.TutorialActive())
            {
                GD.Print("Going to cell editor tab for tutorial purposes (and hiding other tabs)");
                SetEditorTab(EditorTab.CellEditor);

                editorTabSelector.ShowMapTab = false;
                editorTabSelector.ShowReportTab = false;

                cellEditorTab.HideGUIElementsForInitialTutorial();
                HideTabBar();
            }
            else if (TutorialState.EarlyGameGoalTutorial is { CanTrigger: false, Complete: false })
            {
                // On the second go, hide the patch map
                GD.Print("Hiding patch map tab for tutorial purposes");
                editorTabSelector.ShowMapTab = false;

                cellEditorTab.HideAutoEvoPredictionForTutorial();

                // Only hide the advanced tabs if they have not been seen before. This should hopefully reduce player
                // confusion and the potential for useless bug reports to be submitted.
                if (!CurrentGame.IsBoolSet(ADVANCED_TABS_SHOWN_BEFORE))
                {
                    cellEditorTab.HideAdvancedTabs();
                }

                HideTabBar();
            }
            else if (!TutorialState.AutoEvoPrediction.Complete)
            {
                // Third editor cycle
                if (!CurrentGame.IsBoolSet(ADVANCED_TABS_SHOWN_BEFORE))
                {
                    cellEditorTab.HideAdvancedTabs();
                }

                HideTabBar();
            }
            else if (!TutorialState.MigrationTutorial.Complete && !TutorialState.StaySmallTutorial.Complete)
            {
                // Until the last tutorial from other tabs is complete, we hide the tab bar each editor cycle so the
                // player cannot skip stuff and cause problems
                HideTabBar();
            }
        }
    }

    protected override void UpdatePatchDetails()
    {
        // Patch events are able to change the stage's background, so it needs to be updated here.
        cellEditorTab.UpdateBackgroundImage(CurrentPatch);
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
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
        // to the report tab, so this shouldn't matter
        TutorialState.SendEvent(TutorialEventType.MicrobeEditorTabChanged,
            new StringEventArgs(selectedEditorTab.ToString()), this);

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

        base.SetupEditedSpecies();
    }

    private void UpdateAutoEvoToReportTab()
    {
        if (autoEvoResults == null)
            throw new InvalidOperationException("May not be called without report");

        // This creates a new callable each time, but the garbage amount should be negligible
        reportTab.UpdateAutoEvoResults(autoEvoResults, autoEvoExternal?.ToString() ?? "error",
            () => autoEvoResults.MakeSummary(true));
    }

    private void OnShowStatisticsForTutorial()
    {
        cellEditorTab.ShowStatisticsPanel(true);
    }

    private void OnShowConfirmForTutorial()
    {
        cellEditorTab.ShowConfirmButton(true);
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
}
