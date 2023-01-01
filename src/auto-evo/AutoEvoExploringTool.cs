using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

/// <summary>
///   Auto-evo exploring tool is a scene that contains numerous tools to debug, test, and explore auto-evo.
///   You can change the parameters to see how auto-evo responds, get a clearer view of how species evolves,
///   and start a game based on the simulation results.
///   Partial class: GUI, auto-evo, world control
/// </summary>
public partial class AutoEvoExploringTool : NodeWithInput
{
    // Tab paths

    [Export]
    public NodePath WorldEditorPath = null!;

    [Export]
    public NodePath ConfigEditorPath = null!;

    [Export]
    public NodePath HistoryReportSplitPath = null!;

    [Export]
    public NodePath SpeciesSelectPanelPath = null!;

    [Export]
    public NodePath MapPath = null!;

    [Export]
    public NodePath ReportPath = null!;

    [Export]
    public NodePath ViewerPath = null!;

    // World controls paths

    [Export]
    public NodePath AllWorldsStatisticsLabelPath = null!;

    [Export]
    public NodePath AllWorldsExportSettingsMenuPath = null!;

    [Export]
    public NodePath AllWorldsExportButtonPath = null!;

    [Export]
    public NodePath WorldsListMenuPath = null!;

    [Export]
    public NodePath CurrentWorldStatisticsLabelPath = null!;

    [Export]
    public NodePath CurrentWorldExportSettingsMenuPath = null!;

    [Export]
    public NodePath CurrentWorldExportButtonPath = null!;

    // Auto-evo parameters paths

    [Export]
    public NodePath AllowSpeciesToNotMutatePath = null!;

    [Export]
    public NodePath AllowSpeciesToNotMigratePath = null!;

    [Export]
    public NodePath BiodiversityAttemptFillChancePath = null!;

    [Export]
    public NodePath BiodiversityFromNeighbourPatchChancePath = null!;

    [Export]
    public NodePath BiodiversityNearbyPatchIsFreePopulationPath = null!;

    [Export]
    public NodePath BiodiversitySplitIsMutatedPath = null!;

    [Export]
    public NodePath LowBiodiversityLimitPath = null!;

    [Export]
    public NodePath MaximumSpeciesInPatchPath = null!;

    [Export]
    public NodePath MoveAttemptsPerSpeciesPath = null!;

    [Export]
    public NodePath MutationsPerSpeciesPath = null!;

    [Export]
    public NodePath NewBiodiversityIncreasingSpeciesPopulationPath = null!;

    [Export]
    public NodePath ProtectMigrationsFromSpeciesCapPath = null!;

    [Export]
    public NodePath ProtectNewCellsFromSpeciesCapPath = null!;

    [Export]
    public NodePath RefundMigrationsInExtinctionsPath = null!;

    [Export]
    public NodePath StrictNicheCompetitionPath = null!;

    [Export]
    public NodePath SpeciesSplitByMutationThresholdPopulationAmountPath = null!;

    [Export]
    public NodePath SpeciesSplitByMutationThresholdPopulationFractionPath = null!;

    [Export]
    public NodePath UseBiodiversityForceSplitPath = null!;

    // Status and control paths

    [Export]
    public NodePath CurrentGenerationLabelPath = null!;

    [Export]
    public NodePath RunStatusLabelPath = null!;

    [Export]
    public NodePath FinishXGenerationsSpinBoxPath = null!;

    [Export]
    public NodePath FinishXGenerationsButtonPath = null!;

    [Export]
    public NodePath RunGenerationButtonPath = null!;

    [Export]
    public NodePath RunStepButtonPath = null!;

    [Export]
    public NodePath AbortButtonPath = null!;

    [Export]
    public NodePath PlayWithCurrentSettingPath = null!;

    // Map paths

    [Export]
    public NodePath PatchMapDrawerPath = null!;

    [Export]
    public NodePath PatchDetailsPanelPath = null!;

    // Report paths

    [Export]
    public NodePath HistoryListMenuPath = null!;

    [Export]
    public NodePath ResultsLabelPath = null!;

    // Viewer paths

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexPreviewPath = null!;

    [Export]
    public NodePath SpeciesListMenuPath = null!;

    [Export]
    public NodePath SpeciesDetailsLabelPath = null!;

    [Export]
    public NodePath EvolutionaryTreePath = null!;

    // Dialog paths

    [Export]
    public NodePath ExitConfirmationDialogPath = null!;

    [Export]
    public NodePath ExportSuccessNotificationDialogPath = null!;

    private readonly List<AutoEvoExploringToolWorld> worldsList = new();

    // Tabs
    private Control worldTab = null!;
    private Control configTab = null!;
    private Control historyReportSplit = null!;
    private Control speciesSelectPanel = null!;
    private Control mapTab = null!;
    private Control reportTab = null!;
    private Control viewerTab = null!;

    // World controls

    private CustomRichTextLabel allWorldsStatisticsLabel = null!;
    private CustomDropDown allWorldsExportSettingsMenu = null!;
    private Button allWorldsExportButton = null!;
    private CustomDropDown worldsListMenu = null!;
    private CustomRichTextLabel currentWorldStatisticsLabel = null!;
    private CustomDropDown currentWorldExportSettingsMenu = null!;
    private Button currentWorldExportButton = null!;

    // Auto-evo parameters controls.
    private CustomCheckBox allowSpeciesToNotMutateCheckBox = null!;
    private CustomCheckBox allowSpeciesToNotMigrateCheckBox = null!;
    private SpinBox biodiversityAttemptFillChanceSpinBox = null!;
    private SpinBox biodiversityFromNeighbourPatchChanceSpinBox = null!;
    private CustomCheckBox biodiversityNearbyPatchIsFreePopulationCheckBox = null!;
    private CustomCheckBox biodiversitySplitIsMutatedCheckBox = null!;
    private SpinBox lowBiodiversityLimitSpinBox = null!;
    private SpinBox maximumSpeciesInPatchSpinBox = null!;
    private SpinBox moveAttemptsPerSpeciesSpinBox = null!;
    private SpinBox mutationsPerSpeciesSpinBox = null!;
    private SpinBox newBiodiversityIncreasingSpeciesPopulationSpinBox = null!;
    private CustomCheckBox protectMigrationsFromSpeciesCapCheckBox = null!;
    private CustomCheckBox protectNewCellsFromSpeciesCapCheckBox = null!;
    private CustomCheckBox refundMigrationsInExtinctionsCheckBox = null!;
    private CustomCheckBox strictNicheCompetitionCheckBox = null!;
    private SpinBox speciesSplitByMutationThresholdPopulationAmountSpinBox = null!;
    private SpinBox speciesSplitByMutationThresholdPopulationFractionSpinBox = null!;
    private CustomCheckBox useBiodiversityForceSplitCheckBox = null!;

    // Status controls
    private Label currentGenerationLabel = null!;
    private Label runStatusLabel = null!;
    private SpinBox finishXGenerationsSpinBox = null!;
    private Button finishXGenerationsButton = null!;
    private Button finishOneGenerationButton = null!;
    private Button runOneStepButton = null!;
    private Button abortButton = null!;
    private Button playWithCurrentSettingButton = null!;

    // Report controls
    private CustomRichTextLabel autoEvoResultsLabel = null!;
    private CustomDropDown historyListMenu = null!;

    // Map controls
    private PatchMapDrawer patchMapDrawer = null!;
    private PatchDetailsPanel patchDetailsPanel = null!;

    // Viewer controls
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;
    private CustomDropDown speciesListMenu = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private EvolutionaryTree evolutionaryTree = null!;

    private CustomConfirmationDialog exitConfirmationDialog = null!;
    private CustomConfirmationDialog exportSuccessNotificationDialog = null!;

    private AutoEvoExploringToolWorld world = null!;

    private AutoEvoRun? autoEvoRun;

    private AllWorldsExportSettings allWorldsExportSettings;

    private CurrentWorldExportSettings currentWorldExportSettings;

    /// <summary>
    ///   The generation that report and viewer tab is displaying,
    ///   which equals to the selected popup item index of <see cref="historyListMenu"/>
    /// </summary>
    private int generationDisplayed;

    private int generationsPendingToRun;

    private bool ready;

    private enum TabIndex
    {
        World,
        Config,
        Map,
        Report,
        Viewer,
    }

    private enum RunControlState
    {
        Ready,
        Running,
        Paused,
    }

    [Flags]
    private enum AllWorldsExportSettings
    {
        A = 1,
        B = 2,
        C = 4,
        D = 8,
        E = 16,
    }

    [Flags]
    private enum CurrentWorldExportSettings
    {
        CurrentSpeciesDetails = 1,
        CurrentPatchDetails = 2,
        PerSpeciesDetailedHistory = 4,
        PerPatchHistory = 8,
    }

    public override void _Ready()
    {
        base._Ready();

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.1f, null, false);

        // Retrieve all node paths

        worldTab = GetNode<Control>(WorldEditorPath);
        configTab = GetNode<Control>(ConfigEditorPath);
        historyReportSplit = GetNode<Control>(HistoryReportSplitPath);
        speciesSelectPanel = GetNode<Control>(SpeciesSelectPanelPath);
        mapTab = GetNode<Control>(MapPath);
        reportTab = GetNode<Control>(ReportPath);
        viewerTab = GetNode<Control>(ViewerPath);

        allWorldsStatisticsLabel = GetNode<CustomRichTextLabel>(AllWorldsStatisticsLabelPath);
        allWorldsExportSettingsMenu = GetNode<CustomDropDown>(AllWorldsExportSettingsMenuPath);
        allWorldsExportButton = GetNode<Button>(AllWorldsExportButtonPath);
        worldsListMenu = GetNode<CustomDropDown>(WorldsListMenuPath);
        currentWorldStatisticsLabel = GetNode<CustomRichTextLabel>(CurrentWorldStatisticsLabelPath);
        currentWorldExportSettingsMenu = GetNode<CustomDropDown>(CurrentWorldExportSettingsMenuPath);
        currentWorldExportButton = GetNode<Button>(CurrentWorldExportButtonPath);

        allowSpeciesToNotMutateCheckBox = GetNode<CustomCheckBox>(AllowSpeciesToNotMutatePath);
        allowSpeciesToNotMigrateCheckBox = GetNode<CustomCheckBox>(AllowSpeciesToNotMigratePath);
        biodiversityAttemptFillChanceSpinBox = GetNode<SpinBox>(BiodiversityAttemptFillChancePath);
        biodiversityFromNeighbourPatchChanceSpinBox = GetNode<SpinBox>(BiodiversityFromNeighbourPatchChancePath);
        biodiversitySplitIsMutatedCheckBox = GetNode<CustomCheckBox>(BiodiversitySplitIsMutatedPath);
        biodiversityNearbyPatchIsFreePopulationCheckBox =
            GetNode<CustomCheckBox>(BiodiversityNearbyPatchIsFreePopulationPath);
        lowBiodiversityLimitSpinBox = GetNode<SpinBox>(LowBiodiversityLimitPath);
        maximumSpeciesInPatchSpinBox = GetNode<SpinBox>(MaximumSpeciesInPatchPath);
        moveAttemptsPerSpeciesSpinBox = GetNode<SpinBox>(MoveAttemptsPerSpeciesPath);
        mutationsPerSpeciesSpinBox = GetNode<SpinBox>(MutationsPerSpeciesPath);
        newBiodiversityIncreasingSpeciesPopulationSpinBox =
            GetNode<SpinBox>(NewBiodiversityIncreasingSpeciesPopulationPath);
        protectMigrationsFromSpeciesCapCheckBox = GetNode<CustomCheckBox>(ProtectMigrationsFromSpeciesCapPath);
        protectNewCellsFromSpeciesCapCheckBox = GetNode<CustomCheckBox>(ProtectNewCellsFromSpeciesCapPath);
        refundMigrationsInExtinctionsCheckBox = GetNode<CustomCheckBox>(RefundMigrationsInExtinctionsPath);
        strictNicheCompetitionCheckBox = GetNode<CustomCheckBox>(StrictNicheCompetitionPath);
        speciesSplitByMutationThresholdPopulationAmountSpinBox =
            GetNode<SpinBox>(SpeciesSplitByMutationThresholdPopulationAmountPath);
        speciesSplitByMutationThresholdPopulationFractionSpinBox =
            GetNode<SpinBox>(SpeciesSplitByMutationThresholdPopulationFractionPath);
        useBiodiversityForceSplitCheckBox = GetNode<CustomCheckBox>(UseBiodiversityForceSplitPath);

        currentGenerationLabel = GetNode<Label>(CurrentGenerationLabelPath);
        runStatusLabel = GetNode<Label>(RunStatusLabelPath);
        finishXGenerationsSpinBox = GetNode<SpinBox>(FinishXGenerationsSpinBoxPath);
        finishXGenerationsButton = GetNode<Button>(FinishXGenerationsButtonPath);
        finishOneGenerationButton = GetNode<Button>(RunGenerationButtonPath);
        runOneStepButton = GetNode<Button>(RunStepButtonPath);
        abortButton = GetNode<Button>(AbortButtonPath);
        playWithCurrentSettingButton = GetNode<Button>(PlayWithCurrentSettingPath);

        patchMapDrawer = GetNode<PatchMapDrawer>(PatchMapDrawerPath);
        patchDetailsPanel = GetNode<PatchDetailsPanel>(PatchDetailsPanelPath);

        autoEvoResultsLabel = GetNode<CustomRichTextLabel>(ResultsLabelPath);
        historyListMenu = GetNode<CustomDropDown>(HistoryListMenuPath);

        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexPreviewPath);
        speciesListMenu = GetNode<CustomDropDown>(SpeciesListMenuPath);
        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
        evolutionaryTree = GetNode<EvolutionaryTree>(EvolutionaryTreePath);

        exitConfirmationDialog = GetNode<CustomConfirmationDialog>(ExitConfirmationDialogPath);
        exportSuccessNotificationDialog = GetNode<CustomConfirmationDialog>(ExportSuccessNotificationDialogPath);

        patchMapDrawer.OnSelectedPatchChanged += UpdatePatchDetailPanel;

        // Init button translation
        OnFinishXGenerationsSpinBoxValueChanged((float)finishXGenerationsSpinBox.Value);

        // Connect custom dropdown handler
        historyListMenu.Popup.Connect("index_pressed", this, nameof(HistoryListMenuIndexChanged));
        speciesListMenu.Popup.Connect("index_pressed", this, nameof(SpeciesListMenuIndexChanged));
        worldsListMenu.Popup.Connect("index_pressed", this, nameof(WorldsListMenuIndexChanged));

        InitAllWorldsExportSettingsMenu();
        InitCurrentWorldExportSettingsMenu();

        InitNewGame();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (autoEvoRun != null)
        {
            runStatusLabel.Text = autoEvoRun.Status;

            if (autoEvoRun.WasSuccessful)
            {
                ApplyAutoEvoRun();

                // Clear autoEvoRun and enable buttons to allow the next run to start.
                autoEvoRun = null;
                SetControlButtonsState(RunControlState.Ready);
            }
            else if (autoEvoRun.Aborted)
            {
                autoEvoRun = null;
                SetControlButtonsState(RunControlState.Ready);
            }
        }

        if (autoEvoRun == null && generationsPendingToRun > 0)
        {
            FinishOneGeneration();
            --generationsPendingToRun;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Abort the current run to avoid problems
        autoEvoRun?.Abort();
    }

    [RunOnKeyDown("ui_cancel")]
    public void AskExit()
    {
        exitConfirmationDialog.PopupCenteredShrink();
    }

    private static string GetSettingsTranslation(CurrentWorldExportSettings settings)
    {
        return settings switch
        {
            CurrentWorldExportSettings.CurrentSpeciesDetails =>
                TranslationServer.Translate("CURRENT_SPECIES_DETAILS"),
            CurrentWorldExportSettings.CurrentPatchDetails =>
                TranslationServer.Translate("CURRENT_PATCH_DETAILS"),
            CurrentWorldExportSettings.PerSpeciesDetailedHistory =>
                TranslationServer.Translate("PER_SPECIES_DETAILED_HISTORY"),
            CurrentWorldExportSettings.PerPatchHistory =>
                TranslationServer.Translate("PER_PATCH_HISTORY"),
            _ => throw new ArgumentException($"{settings} is not a valid {nameof(CurrentWorldExportSettings)} value."),
        };
    }

    private void InitNewGame()
    {
        worldsList.Add(new AutoEvoExploringToolWorld());
        WorldsListMenuIndexChanged(worldsList.Count - 1);

        worldsListMenu.AddItem((worldsList.Count - 1).ToString(), false, Colors.White);
        worldsListMenu.CreateElements();
    }

    private void InitAutoEvoConfigControls()
    {
        allowSpeciesToNotMutateCheckBox.Pressed = world.AutoEvoConfiguration.AllowSpeciesToNotMutate;
        allowSpeciesToNotMigrateCheckBox.Pressed = world.AutoEvoConfiguration.AllowSpeciesToNotMigrate;
        biodiversityAttemptFillChanceSpinBox.Value = world.AutoEvoConfiguration.BiodiversityAttemptFillChance;
        biodiversityFromNeighbourPatchChanceSpinBox.Value =
            world.AutoEvoConfiguration.BiodiversityFromNeighbourPatchChance;
        biodiversitySplitIsMutatedCheckBox.Pressed = world.AutoEvoConfiguration.BiodiversityNearbyPatchIsFreePopulation;
        biodiversityNearbyPatchIsFreePopulationCheckBox.Pressed = world.AutoEvoConfiguration.BiodiversitySplitIsMutated;
        lowBiodiversityLimitSpinBox.Value = world.AutoEvoConfiguration.LowBiodiversityLimit;
        maximumSpeciesInPatchSpinBox.Value = world.AutoEvoConfiguration.MaximumSpeciesInPatch;
        moveAttemptsPerSpeciesSpinBox.Value = world.AutoEvoConfiguration.MoveAttemptsPerSpecies;
        mutationsPerSpeciesSpinBox.Value = world.AutoEvoConfiguration.MutationsPerSpecies;
        newBiodiversityIncreasingSpeciesPopulationSpinBox.Value =
            world.AutoEvoConfiguration.NewBiodiversityIncreasingSpeciesPopulation;
        protectMigrationsFromSpeciesCapCheckBox.Pressed = world.AutoEvoConfiguration.ProtectMigrationsFromSpeciesCap;
        protectNewCellsFromSpeciesCapCheckBox.Pressed = world.AutoEvoConfiguration.ProtectNewCellsFromSpeciesCap;
        refundMigrationsInExtinctionsCheckBox.Pressed = world.AutoEvoConfiguration.RefundMigrationsInExtinctions;
        strictNicheCompetitionCheckBox.Pressed = world.AutoEvoConfiguration.StrictNicheCompetition;
        speciesSplitByMutationThresholdPopulationAmountSpinBox.Value =
            world.AutoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationAmount;
        speciesSplitByMutationThresholdPopulationFractionSpinBox.Value =
            world.AutoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationFraction;
        useBiodiversityForceSplitCheckBox.Pressed = world.AutoEvoConfiguration.UseBiodiversityForceSplit;
    }

    private void InitAllWorldsExportSettingsMenu()
    {
        allWorldsExportSettingsMenu.AddItem(TranslationServer.Translate("Dummy"), true, Colors.White);
        allWorldsExportSettingsMenu.AddItem(TranslationServer.Translate("."), true, Colors.White);
        allWorldsExportSettingsMenu.AddItem(TranslationServer.Translate("."), true, Colors.White);
        allWorldsExportSettingsMenu.AddItem(TranslationServer.Translate("."), true, Colors.White);
        allWorldsExportSettingsMenu.AddItem(TranslationServer.Translate("."), true, Colors.White);
        allWorldsExportSettingsMenu.CreateElements();

        allWorldsExportSettingsMenu.Popup.HideOnCheckableItemSelection = false;
        allWorldsExportSettingsMenu.Popup.Connect("index_pressed", this,
            nameof(AllWorldsExportSettingsMenuIndexPressed));
    }

    private void AllWorldsExportSettingsMenuIndexPressed(int index)
    {
        var bit = (AllWorldsExportSettings)(1 << index);
        if ((allWorldsExportSettings & bit) != 0)
        {
            allWorldsExportSettings &= ~bit;
        }
        else
        {
            allWorldsExportSettings |= bit;
        }

        allWorldsExportSettingsMenu.Popup.SetItemChecked(index, (allWorldsExportSettings & bit) != 0);
    }

    private void InitCurrentWorldExportSettingsMenu()
    {
        currentWorldExportSettingsMenu.AddItem(
            GetSettingsTranslation(CurrentWorldExportSettings.CurrentSpeciesDetails), true, Colors.White);
        currentWorldExportSettingsMenu.AddItem(
            GetSettingsTranslation(CurrentWorldExportSettings.CurrentPatchDetails), true, Colors.White);
        currentWorldExportSettingsMenu.AddItem(
            GetSettingsTranslation(CurrentWorldExportSettings.PerSpeciesDetailedHistory), true, Colors.White);
        currentWorldExportSettingsMenu.AddItem(
            GetSettingsTranslation(CurrentWorldExportSettings.PerPatchHistory), true, Colors.White);
        currentWorldExportSettingsMenu.CreateElements();

        currentWorldExportSettingsMenu.Popup.HideOnCheckableItemSelection = false;
        currentWorldExportSettingsMenu.Popup.Connect("index_pressed", this,
            nameof(CurrentWorldExportSettingsMenuIndexPressed));
    }

    private void CurrentWorldExportSettingsMenuIndexPressed(int index)
    {
        var bit = (CurrentWorldExportSettings)(1 << index);
        if ((currentWorldExportSettings & bit) != 0)
        {
            currentWorldExportSettings &= ~bit;
        }
        else
        {
            currentWorldExportSettings |= bit;
        }

        currentWorldExportSettingsMenu.Popup.SetItemChecked(index, (currentWorldExportSettings & bit) != 0);
    }

    private void SetControlButtonsState(RunControlState runControlState)
    {
        switch (runControlState)
        {
            case RunControlState.Ready:
            {
                finishXGenerationsSpinBox.Editable = true;
                finishXGenerationsButton.Disabled = false;
                finishOneGenerationButton.Disabled = false;
                runOneStepButton.Disabled = false;
                playWithCurrentSettingButton.Disabled = false;
                abortButton.Disabled = true;
                break;
            }

            case RunControlState.Running:
            {
                finishXGenerationsSpinBox.Editable = false;
                finishXGenerationsButton.Disabled = true;
                finishOneGenerationButton.Disabled = true;
                runOneStepButton.Disabled = true;
                playWithCurrentSettingButton.Disabled = true;
                abortButton.Disabled = false;
                break;
            }

            case RunControlState.Paused:
            {
                finishXGenerationsSpinBox.Editable = true;
                finishXGenerationsButton.Disabled = false;
                finishOneGenerationButton.Disabled = false;
                runOneStepButton.Disabled = false;
                playWithCurrentSettingButton.Disabled = false;
                abortButton.Disabled = false;
                break;
            }
        }
    }

    private void OnBackButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        AskExit();
    }

    private void ConfirmExit()
    {
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f,
            SceneManager.Instance.ReturnToMenu, false);
    }

    private void ChangeTab(string tab)
    {
        GUICommon.Instance.PlayButtonPressSound();

        switch ((TabIndex)Enum.Parse(typeof(TabIndex), tab))
        {
            case TabIndex.World:
            {
                configTab.Visible = false;
                historyReportSplit.Visible = false;
                worldTab.Visible = true;
                break;
            }

            case TabIndex.Config:
            {
                worldTab.Visible = false;
                historyReportSplit.Visible = false;
                configTab.Visible = true;
                break;
            }

            case TabIndex.Map:
            {
                worldTab.Visible = false;
                configTab.Visible = false;
                reportTab.Visible = false;
                viewerTab.Visible = false;
                speciesSelectPanel.Visible = false;
                historyReportSplit.Visible = true;
                mapTab.Visible = true;
                break;
            }

            case TabIndex.Report:
            {
                worldTab.Visible = false;
                configTab.Visible = false;
                mapTab.Visible = false;
                viewerTab.Visible = false;
                speciesSelectPanel.Visible = false;
                historyReportSplit.Visible = true;
                reportTab.Visible = true;
                break;
            }

            case TabIndex.Viewer:
            {
                worldTab.Visible = false;
                reportTab.Visible = false;
                mapTab.Visible = false;
                configTab.Visible = false;
                speciesSelectPanel.Visible = true;
                historyReportSplit.Visible = true;
                viewerTab.Visible = true;
                break;
            }
        }
    }

    /// <summary>
    ///   This function updates all configurations in a row to avoid adding numerous separate callback functions.
    /// </summary>
    /// <param name="value">
    ///   Godot Signal parameter, 'state' from Button::toggled or 'value' from SpinBox::value_changed.
    /// </param>
    private void UpdateAutoEvoConfiguration(object? value = null)
    {
        _ = value;

        if (!ready)
            return;

        world.AutoEvoConfiguration.AllowSpeciesToNotMutate = allowSpeciesToNotMutateCheckBox.Pressed;
        world.AutoEvoConfiguration.AllowSpeciesToNotMigrate = allowSpeciesToNotMigrateCheckBox.Pressed;
        world.AutoEvoConfiguration.BiodiversityAttemptFillChance = (int)biodiversityAttemptFillChanceSpinBox.Value;
        world.AutoEvoConfiguration.BiodiversityFromNeighbourPatchChance =
            (float)biodiversityFromNeighbourPatchChanceSpinBox.Value;
        world.AutoEvoConfiguration.BiodiversityNearbyPatchIsFreePopulation = biodiversitySplitIsMutatedCheckBox.Pressed;
        world.AutoEvoConfiguration.BiodiversitySplitIsMutated = biodiversityNearbyPatchIsFreePopulationCheckBox.Pressed;
        world.AutoEvoConfiguration.LowBiodiversityLimit = (int)lowBiodiversityLimitSpinBox.Value;
        world.AutoEvoConfiguration.MaximumSpeciesInPatch = (int)maximumSpeciesInPatchSpinBox.Value;
        world.AutoEvoConfiguration.MoveAttemptsPerSpecies = (int)moveAttemptsPerSpeciesSpinBox.Value;
        world.AutoEvoConfiguration.MutationsPerSpecies = (int)mutationsPerSpeciesSpinBox.Value;
        world.AutoEvoConfiguration.NewBiodiversityIncreasingSpeciesPopulation =
            (int)newBiodiversityIncreasingSpeciesPopulationSpinBox.Value;
        world.AutoEvoConfiguration.ProtectMigrationsFromSpeciesCap = protectMigrationsFromSpeciesCapCheckBox.Pressed;
        world.AutoEvoConfiguration.ProtectNewCellsFromSpeciesCap = protectNewCellsFromSpeciesCapCheckBox.Pressed;
        world.AutoEvoConfiguration.RefundMigrationsInExtinctions = refundMigrationsInExtinctionsCheckBox.Pressed;
        world.AutoEvoConfiguration.StrictNicheCompetition = strictNicheCompetitionCheckBox.Pressed;
        world.AutoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationAmount =
            (int)speciesSplitByMutationThresholdPopulationAmountSpinBox.Value;
        world.AutoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationFraction =
            (float)speciesSplitByMutationThresholdPopulationFractionSpinBox.Value;
        world.AutoEvoConfiguration.UseBiodiversityForceSplit = useBiodiversityForceSplitCheckBox.Pressed;
    }

    private void OnFinishXGenerationsSpinBoxValueChanged(float value)
    {
        finishXGenerationsButton.Text =
            TranslationServer.Translate("FINISH_X_GENERATIONS").FormatSafe(Math.Round(value));
    }

    /// <summary>
    ///   Sequentially finish X generations
    /// </summary>
    private void OnFinishXGenerationsButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        generationsPendingToRun = (int)Math.Round(finishXGenerationsSpinBox.Value) - 1;

        FinishOneGeneration();
    }

    /// <summary>
    ///   Run a new generation or finish the current generation async
    /// </summary>
    private void OnFinishOneGenerationButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        FinishOneGeneration();
    }

    private void FinishOneGeneration()
    {
        if (autoEvoRun?.Aborted != false || autoEvoRun.Finished)
        {
            // If the previous one has finished / failed
            autoEvoRun = new AutoEvoRun(world.GameProperties.GameWorld) { FullSpeed = true };
            autoEvoRun.Start();
        }
        else
        {
            // If the previous one is paused
            autoEvoRun.FullSpeed = true;
            autoEvoRun.Continue();
        }

        SetControlButtonsState(RunControlState.Running);
    }

    /// <summary>
    ///   Apply auto-evo effects and set up related controls
    /// </summary>
    private void ApplyAutoEvoRun()
    {
        var results = autoEvoRun!.Results!;
        var gameWorld = world.GameProperties.GameWorld;

        // Make summary, this must be called before results are applied so that summary is correct
        world.RunResultsList.Add(results.MakeSummary(gameWorld.Map, true));

        // Apply the results
        gameWorld.OnTimePassed(1);
        autoEvoRun.ApplyAllResultsAndEffects(true);
        ++gameWorld.PlayerSpecies.Generation;
        ++world.CurrentGeneration;

        // Add run results, this must be called after results are applied to generate unique species ID
        gameWorld.GenerationHistory.Add(gameWorld.PlayerSpecies.Generation - 1,
            new GenerationRecord(gameWorld.TotalPassedTime, results.GetSpeciesRecords()));
        gameWorld.BuildEvolutionaryTree(evolutionaryTree);
        world.SpeciesHistoryList.Add(gameWorld.Species.ToDictionary(pair => pair.Key,
            pair => (Species)pair.Value.Clone()));
        world.PatchHistoryList.Add(gameWorld.Map.Patches.ToDictionary(pair => pair.Key,
            pair => (PatchSnapshot)pair.Value.CurrentSnapshot.Clone()));

        // Add checkbox to history container
        historyListMenu.AddItem(world.CurrentGeneration.ToString(), false, Colors.White);
        historyListMenu.CreateElements();

        // Select the current generation
        HistoryListMenuIndexChanged(world.CurrentGeneration);

        currentGenerationLabel.Text = world.CurrentGeneration.ToString();
    }

    /// <summary>
    ///   Run one step synchronously
    /// </summary>
    private void OnRunOneStepButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (autoEvoRun?.Aborted != false || autoEvoRun.Finished)
        {
            autoEvoRun = new AutoEvoRun(world.GameProperties.GameWorld);
        }

        // To avoid concurrent steps
        autoEvoRun.FullSpeed = false;
        autoEvoRun.OneStep();
        SetControlButtonsState(RunControlState.Paused);
    }

    /// <summary>
    ///   Abort the current run
    /// </summary>
    private void OnAbortButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (autoEvoRun?.WasSuccessful == false)
            autoEvoRun.Abort();

        generationsPendingToRun = 0;

        SetControlButtonsState(RunControlState.Ready);
    }

    private void WorldsListMenuIndexChanged(int index)
    {
        if (world == worldsList[index])
            return;

        autoEvoRun?.Abort();
        autoEvoRun = null;
        generationsPendingToRun = 0;
        SetControlButtonsState(RunControlState.Ready);
        runStatusLabel.Text = TranslationServer.Translate("READY");

        worldsListMenu.Text = index.ToString();

        world = worldsList[index];

        generationDisplayed = world.CurrentGeneration;
        currentGenerationLabel.Text = world.CurrentGeneration.ToString();

        // Rebuild history list
        historyListMenu.ClearAllItems();
        for (int i = 0; i <= world.CurrentGeneration; ++i)
        {
            historyListMenu.AddItem(i.ToString(), false, Colors.White);
        }

        historyListMenu.CreateElements();
        HistoryListMenuIndexChanged(world.CurrentGeneration);

        UpdateSpeciesList();
        SpeciesListMenuIndexChanged(0);

        patchMapDrawer.Map = world.GameProperties.GameWorld.Map;
        patchDetailsPanel.SelectedPatch = patchMapDrawer.PlayerPatch;

        world.GameProperties.GameWorld.BuildEvolutionaryTree(evolutionaryTree);

        ready = false;
        InitAutoEvoConfigControls();
        ready = true;
    }

    private void HistoryListMenuIndexChanged(int index)
    {
        if (generationDisplayed == index && speciesListMenu.Popup.GetItemCount() > 0)
            return;

        historyListMenu.Text = index.ToString();

        generationDisplayed = index;
        UpdateAutoEvoReport();
        UpdateSpeciesList();
        UpdatePatchDetailPanel(patchMapDrawer);
    }

    private void UpdateAutoEvoReport()
    {
        if (generationDisplayed < world.RunResultsList.Count && generationDisplayed > -1)
        {
            autoEvoResultsLabel.ExtendedBbcode = world.RunResultsList[generationDisplayed].ToString();
        }
    }

    private void UpdateSpeciesList()
    {
        speciesListMenu.ClearAllItems();

        foreach (var pair in world.SpeciesHistoryList[generationDisplayed].OrderBy(p => p.Value.FormattedName))
        {
            speciesListMenu.AddItem(pair.Value.FormattedName, false, Colors.White);
        }

        speciesListMenu.CreateElements();
    }

    private void SpeciesListMenuIndexChanged(int index)
    {
        var speciesName = speciesListMenu.Popup.GetItemText(index);
        var species = world.SpeciesHistoryList[generationDisplayed].Values.First(p => p.FormattedName == speciesName);

        if (species == speciesPreview.PreviewSpecies)
            return;

        UpdateSpeciesPreview(species);
    }

    private void UpdateSpeciesPreview(Species species)
    {
        speciesListMenu.Text = species.FormattedName;
        speciesPreview.PreviewSpecies = species;

        if (species is MicrobeSpecies microbeSpecies)
        {
            hexesPreview.PreviewSpecies = microbeSpecies;
        }
        else
        {
            GD.PrintErr("Unknown species type to preview: ", species);
        }

        UpdateSpeciesDetail(species);
    }

    private void EvolutionaryTreeNodeSelected(int generation, uint id)
    {
        HistoryListMenuIndexChanged(generation);
        UpdateSpeciesPreview(world.SpeciesHistoryList[generation][id]);
    }

    private void UpdateSpeciesDetail(Species species)
    {
        speciesDetailsLabel.ExtendedBbcode = species.GetDetailString();
    }

    private void UpdatePatchDetailPanel(PatchMapDrawer drawer)
    {
        var selectedPatch = drawer.SelectedPatch;

        if (selectedPatch == null)
            return;

        // Get current snapshot
        var patch = new Patch(selectedPatch.Name, 0, selectedPatch.BiomeTemplate, selectedPatch.BiomeType,
            world.PatchHistoryList[generationDisplayed][selectedPatch.ID])
        {
            TimePeriod = selectedPatch.TimePeriod,
            Depth = { [0] = selectedPatch.Depth[0], [1] = selectedPatch.Depth[1] },
        };

        patchDetailsPanel.SelectedPatch = patch;
    }

    private void PlayWithCurrentSettingPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Disable the button to prevent it being executed again.
        playWithCurrentSettingButton.Disabled = true;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, () =>
        {
            // Instantiate a new editor scene
            var editor = (MicrobeEditor)SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instance();

            world.GameProperties.EnterFreeBuild();
            world.GameProperties.TutorialState.Enabled = false;

            // Copy our currently setup game to the editor
            editor.CurrentGame = world.GameProperties;

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
    }

    /// <summary>
    ///   Stores all data auto-evo exploring tool needs to present a world.
    /// </summary>
    private class AutoEvoExploringToolWorld
    {
        /// <summary>
        ///   Local copy of auto-evo configuration. Used to avoid modifying the global one
        /// </summary>
        public readonly AutoEvoConfiguration AutoEvoConfiguration;

        /// <summary>
        ///   The game itself
        /// </summary>
        public readonly GameProperties GameProperties;

        /// <summary>
        ///   This list stores copy of species in every generation.
        /// </summary>
        public readonly List<Dictionary<uint, Species>> SpeciesHistoryList;

        public readonly List<Dictionary<int, PatchSnapshot>> PatchHistoryList;

        /// <summary>
        ///   This list stores all auto-evo results.
        /// </summary>
        public readonly List<LocalizedStringBuilder> RunResultsList;

        /// <summary>
        ///   The current generation auto-evo has evolved
        /// </summary>
        public int CurrentGeneration;

        public AutoEvoExploringToolWorld(AutoEvoConfiguration? configuration = null)
        {
            AutoEvoConfiguration = configuration ?? SimulationParameters.Instance.AutoEvoConfiguration.Clone();
            GameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings
                { AutoEvoConfiguration = AutoEvoConfiguration });
            SpeciesHistoryList = new List<Dictionary<uint, Species>>();
            PatchHistoryList = new List<Dictionary<int, PatchSnapshot>>();
            RunResultsList = new List<LocalizedStringBuilder>();
            CurrentGeneration = 0;

            RunResultsList.Add(new LocalizedStringBuilder());
            SpeciesHistoryList.Add(new Dictionary<uint, Species>
            {
                {
                    GameProperties.GameWorld.PlayerSpecies.ID,
                    (Species)GameProperties.GameWorld.PlayerSpecies.Clone()
                },
            });

            PatchHistoryList.Add(GameProperties.GameWorld.Map.Patches.ToDictionary(pair => pair.Key,
                pair => (PatchSnapshot)pair.Value.CurrentSnapshot.Clone()));
        }
    }
}
