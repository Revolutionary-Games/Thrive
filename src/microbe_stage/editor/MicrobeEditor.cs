using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SharedBase.Archive;
using Systems;
using UnlockConstraints;

/// <summary>
///   Main class of the microbe editor
/// </summary>
public partial class MicrobeEditor : EditorBase<EditorAction, MicrobeStage>, IEditorReportData, ICellEditorData
{
    public const ushort SERIALIZATION_VERSION = 1;

    private const string ADVANCED_TABS_SHOWN_BEFORE = "editor_advanced_tabs";

    private const double EMERGENCY_SHOW_TABS_AFTER_SECONDS = 4;

    // TODO: this prevents directly starting the editor scene due to depending on the simulation parameters being
    // loaded
    private readonly MicrobeSpeciesComparer speciesComparer = new();

#pragma warning disable CA2213
    [Export]
    private MicrobeEditorReportComponent reportTab = null!;

    [Export]
    private MicrobeEditorPatchMap patchMapTab = null!;

    [Export]
    private CellEditorComponent cellEditorTab = null!;

    private MicrobeEditorTutorialGUI tutorialGUI = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   The species that is being edited, changes are applied to it on exit
    /// </summary>
    private MicrobeSpecies? editedSpecies;

    /// <summary>
    ///   Used to cache full edited status for <see cref="speciesComparer"/> usage
    /// </summary>
    private MicrobeEditsFacade? editsFacade;

    private bool checkingTabVisibility;
    private double tabCheckVisibilityTimer = 10;

    private Dictionary<OrganelleDefinition, int> tempMemory1 = new();

    public override bool CanCancelAction => cellEditorTab.Visible && cellEditorTab.CanCancelAction;

    public override Species EditedBaseSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    public ICellDefinition EditedCellProperties =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    public IReadOnlyList<OrganelleTemplate> EditedCellOrganelles => cellEditorTab.GetLatestEditedOrganelles();

    public Patch CurrentPatch => patchMapTab.CurrentPatch;

    public Patch? TargetPatch => patchMapTab.TargetPatch;

    public Patch? SelectedPatch => patchMapTab.SelectedPatch;

    public WorldAndPlayerDataSource UnlocksDataSource =>
        new(CurrentGame.GameWorld, CurrentPatch, GetPlayerDataSource());

    public override MainGameState GameState => MainGameState.MicrobeEditor;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.MicrobeEditor;

    protected override string MusicCategory => "MicrobeEditor";

    protected override MainGameState ReturnToState => MainGameState.MicrobeStage;
    protected override string EditorLoadingMessage => Localization.Translate("LOADING_MICROBE_EDITOR");
    protected override bool HasInProgressAction => CanCancelAction;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.MicrobeEditor)
            throw new NotSupportedException();

        writer.WriteObject((MicrobeEditor)obj);
    }

    public static MicrobeEditor ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var scene = GD.Load<PackedScene>("res://src/microbe_stage/editor/MicrobeEditor.tscn");

        var instance = scene.Instantiate<MicrobeEditor>();

        instance.ResolveNodeReferences();

        reader.ReadObjectProperties(instance.reportTab);
        reader.ReadObjectProperties(instance.patchMapTab);
        reader.ReadObjectProperties(instance.cellEditorTab);

        // Base version is different
        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        instance.editedSpecies = reader.ReadObjectOrNull<MicrobeSpecies>();

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        // Due to callbacks in history, subcomponents need to be written first
        writer.WriteObjectProperties(reportTab);
        writer.WriteObjectProperties(patchMapTab);
        writer.WriteObjectProperties(cellEditorTab);

        // Don't call base as it is the base abstract one
        writer.Write(SERIALIZATION_VERSION_BASE);
        WriteBasePropertiesToArchive(writer);

        writer.WriteObjectOrNull(editedSpecies);
    }

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

    public override void _Process(double delta)
    {
        base._Process(delta);
        CheckTabVisibilityAfterTutorial(delta);
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

            if (!history.CanUndo())
            {
                // Nothing done in the whole editor cycle
                AchievementEvents.ReportExitEditorWithoutChanges();
            }
        }

        return result;
    }

    public override void AddContextToAction(CombinableActionData editorActions)
    {
        // Microbe editor doesn't require any context data in actions
    }

    public ToleranceResult CalculateRawTolerances(bool excludePositiveBuffs = false)
    {
        return cellEditorTab.CalculateRawTolerances(excludePositiveBuffs);
    }

    public void OnTolerancesChanged(EnvironmentalTolerances newTolerances)
    {
        cellEditorTab.OnTolerancesChanged(newTolerances);
    }

    public EnvironmentalTolerances GetOptimalTolerancesForCurrentPatch()
    {
        return CurrentPatch.GenerateTolerancesForMicrobe(EditedCellOrganelles);
    }

    public ToleranceResult CalculateCurrentTolerances(EnvironmentalTolerances calculationTolerances)
    {
        return MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(calculationTolerances,
            EditedCellOrganelles, CurrentPatch.Biome);
    }

    public void GetCurrentToleranceSummaryByElement(ToleranceModifier toleranceCategory,
        Dictionary<IPlayerReadableName, float> result)
    {
        MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByOrganelle(EditedCellOrganelles,
            toleranceCategory, result);
    }

    public void CalculateBodyEffectOnTolerances(
        ref MicrobeEnvironmentalToleranceCalculations.ToleranceValues modifiedTolerances)
    {
        MicrobeEnvironmentalToleranceCalculations.ApplyOrganelleEffectsOnTolerances(EditedCellOrganelles,
            ref modifiedTolerances);
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
        tutorialGUI = GetNode<MicrobeEditorTutorialGUI>("TutorialGUI");
    }

    protected override void InitEditor(bool fresh)
    {
        patchMapTab.SetMap(CurrentGame.GameWorld.Map, CurrentGame.GameWorld.PlayerSpecies.ID);

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
        patchMapTab.MarkDrawerDirty();

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

    protected override double CalculateUsedMutationPoints(List<EditorCombinableActionData> performedActionData)
    {
        editsFacade ??=
            new MicrobeEditsFacade(
                editedSpecies ?? throw new Exception("Species not initialized before calculating MP"));

        editsFacade.SetActiveActions(performedActionData);

        // This doesn't use the cell editor CostMultiplier as this cost is purely used in the microbe stage, so we
        //  don't need to apply the additional considerations on top of this
        return speciesComparer.Compare(editedSpecies!, editsFacade, Constants.MAX_SINGLE_EDIT_MP_COST,
            CurrentGame.GameWorld.WorldSettings.MPMultiplier);
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

                StartTimerForSafetyTabShow();
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

    private void StartTimerForSafetyTabShow()
    {
        checkingTabVisibility = true;
        tabCheckVisibilityTimer = EMERGENCY_SHOW_TABS_AFTER_SECONDS;
    }

    /// <summary>
    ///   Makes sure that due to some tutorials not triggering when they have been done out of order in different
    ///   saves, won't leave editor tabs permanently hidden during some tutorial cycles.
    /// </summary>
    private void CheckTabVisibilityAfterTutorial(double delta)
    {
        if (!checkingTabVisibility)
            return;

        tabCheckVisibilityTimer -= delta;

        if (tabCheckVisibilityTimer > 0)
            return;

        checkingTabVisibility = false;

        if (TutorialState.Enabled)
        {
            if (!TutorialState.TutorialActive())
            {
                // Tutorial is likely sequence broken, so it won't continue, show tabs to not get the player stuck
                if (editorTabSelector == null || !editorTabSelector.Visible)
                    GD.Print("Showing tabs as tutorial is not active while it probably should be");
                ShowTabBar(true);
            }
        }
        else
        {
            // Make sure tabs are shown if the tutorial is turned off
            ShowTabBar(true);
        }
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

        var specialization =
            MicrobeInternalCalculations.CalculateSpecializationBonus(editedSpecies.ModifiableOrganelles.Organelles,
                tempMemory1);

        ProcessSystem.ComputeEnergyBalanceSimple(editedSpecies.ModifiableOrganelles.Organelles,
            CurrentPatch.Biome, in tolerances, specialization, editedSpecies.MembraneType, Vector3.Zero, false, true,
            CurrentGame.GameWorld.WorldSettings, CompoundAmountType.Maximum, null, energyBalance);

        return new MicrobeUnlocksData(editedSpecies, energyBalance);
    }

    private class MicrobeUnlocksData : IPlayerDataSource
    {
        public ICellDefinition? CellDefinition;

        public MicrobeUnlocksData(ICellDefinition? cellDefinition, EnergyBalanceInfoSimple? energyBalance)
        {
            CellDefinition = cellDefinition;
            EnergyBalance = energyBalance;
        }

        public EnergyBalanceInfoSimple? EnergyBalance { get; set; }

        public float Speed
        {
            get
            {
                if (CellDefinition == null)
                    return 0;

                return MicrobeInternalCalculations.SpeedToUserReadableNumber(MicrobeInternalCalculations.CalculateSpeed(
                    CellDefinition.ModifiableOrganelles.Organelles, CellDefinition.MembraneType,
                    CellDefinition.MembraneRigidity, CellDefinition.IsBacteria));
            }
        }
    }
}
