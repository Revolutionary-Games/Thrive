﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoEvo;
using Godot;

/// <summary>
///   Auto-evo exploring tool is a scene that contains numerous tools to debug, test, and explore auto-evo.
///   You can change the parameters to see how auto-evo responds, get a clearer view of how species evolves,
///   and start a game based on the simulation results.
/// </summary>
/// <remarks>
///   <para>
///     Partial class: GUI, auto-evo, world control
///   </para>
/// </remarks>
public partial class AutoEvoExploringTool : NodeWithInput
{
    // Tab paths

    [Export]
    public NodePath? WorldEditorPath;

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
    public NodePath WorldsListMenuPath = null!;

    [Export]
    public NodePath NewWorldButtonPath = null!;

    [Export]
    public NodePath CurrentWorldStatisticsLabelPath = null!;

    [Export]
    public NodePath WorldExportButtonPath = null!;

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
    public NodePath RunStatusLabelPath = null!;

    [Export]
    public NodePath FinishXGenerationsSpinBoxPath = null!;

    [Export]
    public NodePath FinishXGenerationsButtonPath = null!;

    [Export]
    public NodePath RunXWorldsSpinBoxPath = null!;

    [Export]
    public NodePath RunXWorldsButtonPath = null!;

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
    public NodePath SpeciesListMenuPath = null!;

    [Export]
    public NodePath EvolutionaryTreePath = null!;

    [Export]
    public NodePath SpeciesDetailsPanelPath = null!;

    // Dialog paths

    [Export]
    public NodePath ExitConfirmationDialogPath = null!;

    [Export]
    public NodePath ExportSuccessNotificationDialogPath = null!;

    private readonly List<AutoEvoExploringToolWorld> worldsList = new();

#pragma warning disable CA2213

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
    private CustomDropDown worldsListMenu = null!;
    private TextureButton newWorldButton = null!;
    private CustomRichTextLabel currentWorldStatisticsLabel = null!;
    private Button worldExportButton = null!;

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
    private Label runStatusLabel = null!;
    private SpinBox finishXGenerationsSpinBox = null!;
    private Button finishXGenerationsButton = null!;
    private SpinBox runXWorldsSpinBox = null!;
    private Button runXWorldsButton = null!;
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
    private CustomDropDown speciesListMenu = null!;
    private EvolutionaryTree evolutionaryTree = null!;
    private SpeciesDetailsPanel speciesDetailsPanel = null!;

    private CustomConfirmationDialog exitConfirmationDialog = null!;
    private CustomConfirmationDialog exportSuccessNotificationDialog = null!;
#pragma warning restore CA2213

    private AutoEvoExploringToolWorld world = null!;

    private AutoEvoRun? autoEvoRun;

    private List<OrganelleDefinition> allOrganelles = null!;

    /// <summary>
    ///   The generation that report and viewer tab is displaying,
    ///   which equals to the selected popup item index of <see cref="historyListMenu"/>
    /// </summary>
    private int generationDisplayed;

    private int generationsPendingToRun;

    private int worldsPendingToRun;

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
        worldsListMenu = GetNode<CustomDropDown>(WorldsListMenuPath);
        newWorldButton = GetNode<TextureButton>(NewWorldButtonPath);
        currentWorldStatisticsLabel = GetNode<CustomRichTextLabel>(CurrentWorldStatisticsLabelPath);
        worldExportButton = GetNode<Button>(WorldExportButtonPath);

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

        runStatusLabel = GetNode<Label>(RunStatusLabelPath);
        finishXGenerationsSpinBox = GetNode<SpinBox>(FinishXGenerationsSpinBoxPath);
        finishXGenerationsButton = GetNode<Button>(FinishXGenerationsButtonPath);
        runXWorldsSpinBox = GetNode<SpinBox>(RunXWorldsSpinBoxPath);
        runXWorldsButton = GetNode<Button>(RunXWorldsButtonPath);
        finishOneGenerationButton = GetNode<Button>(RunGenerationButtonPath);
        runOneStepButton = GetNode<Button>(RunStepButtonPath);
        abortButton = GetNode<Button>(AbortButtonPath);
        playWithCurrentSettingButton = GetNode<Button>(PlayWithCurrentSettingPath);

        patchMapDrawer = GetNode<PatchMapDrawer>(PatchMapDrawerPath);
        patchDetailsPanel = GetNode<PatchDetailsPanel>(PatchDetailsPanelPath);

        autoEvoResultsLabel = GetNode<CustomRichTextLabel>(ResultsLabelPath);
        historyListMenu = GetNode<CustomDropDown>(HistoryListMenuPath);

        speciesListMenu = GetNode<CustomDropDown>(SpeciesListMenuPath);
        evolutionaryTree = GetNode<EvolutionaryTree>(EvolutionaryTreePath);
        speciesDetailsPanel = GetNode<SpeciesDetailsPanel>(SpeciesDetailsPanelPath);

        exitConfirmationDialog = GetNode<CustomConfirmationDialog>(ExitConfirmationDialogPath);
        exportSuccessNotificationDialog = GetNode<CustomConfirmationDialog>(ExportSuccessNotificationDialogPath);

        patchMapDrawer.OnSelectedPatchChanged += UpdatePatchDetailPanel;

        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().ToList();

        // Init button translation
        OnFinishXGenerationsSpinBoxValueChanged((float)finishXGenerationsSpinBox.Value);

        // Connect custom dropdown handler
        historyListMenu.Popup.Connect("index_pressed", this, nameof(HistoryListMenuIndexChanged));
        speciesListMenu.Popup.Connect("index_pressed", this, nameof(SpeciesListMenuIndexChanged));
        worldsListMenu.Popup.Connect("index_pressed", this, nameof(WorldsListMenuIndexChanged));

        InitNewWorld();
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

        if (autoEvoRun == null && worldsPendingToRun > 0)
        {
            InitNewWorld(world.AutoEvoConfiguration);
            FinishOneGeneration();
            generationsPendingToRun = (int)Math.Round(finishXGenerationsSpinBox.Value) - 1;
            --worldsPendingToRun;
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (WorldEditorPath != null)
            {
                WorldEditorPath.Dispose();
                ConfigEditorPath.Dispose();
                HistoryReportSplitPath.Dispose();
                SpeciesSelectPanelPath.Dispose();
                MapPath.Dispose();
                ReportPath.Dispose();
                ViewerPath.Dispose();
                AllWorldsStatisticsLabelPath.Dispose();
                WorldsListMenuPath.Dispose();
                NewWorldButtonPath.Dispose();
                CurrentWorldStatisticsLabelPath.Dispose();
                WorldExportButtonPath.Dispose();
                AllowSpeciesToNotMutatePath.Dispose();
                AllowSpeciesToNotMigratePath.Dispose();
                BiodiversityAttemptFillChancePath.Dispose();
                BiodiversityFromNeighbourPatchChancePath.Dispose();
                BiodiversityNearbyPatchIsFreePopulationPath.Dispose();
                BiodiversitySplitIsMutatedPath.Dispose();
                LowBiodiversityLimitPath.Dispose();
                MaximumSpeciesInPatchPath.Dispose();
                MoveAttemptsPerSpeciesPath.Dispose();
                MutationsPerSpeciesPath.Dispose();
                NewBiodiversityIncreasingSpeciesPopulationPath.Dispose();
                ProtectMigrationsFromSpeciesCapPath.Dispose();
                ProtectNewCellsFromSpeciesCapPath.Dispose();
                RefundMigrationsInExtinctionsPath.Dispose();
                StrictNicheCompetitionPath.Dispose();
                SpeciesSplitByMutationThresholdPopulationAmountPath.Dispose();
                SpeciesSplitByMutationThresholdPopulationFractionPath.Dispose();
                UseBiodiversityForceSplitPath.Dispose();
                RunStatusLabelPath.Dispose();
                FinishXGenerationsSpinBoxPath.Dispose();
                FinishXGenerationsButtonPath.Dispose();
                RunXWorldsSpinBoxPath.Dispose();
                RunXWorldsButtonPath.Dispose();
                RunGenerationButtonPath.Dispose();
                RunStepButtonPath.Dispose();
                AbortButtonPath.Dispose();
                PlayWithCurrentSettingPath.Dispose();
                PatchMapDrawerPath.Dispose();
                PatchDetailsPanelPath.Dispose();
                HistoryListMenuPath.Dispose();
                ResultsLabelPath.Dispose();
                SpeciesListMenuPath.Dispose();
                EvolutionaryTreePath.Dispose();
                SpeciesDetailsPanelPath.Dispose();
                ExitConfirmationDialogPath.Dispose();
                ExportSuccessNotificationDialogPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void InitNewWorld()
    {
        InitNewWorld(SimulationParameters.Instance.AutoEvoConfiguration);
    }

    private void InitNewWorld(IAutoEvoConfiguration configuration)
    {
        worldsList.Add(new AutoEvoExploringToolWorld(configuration));
        WorldsListMenuIndexChanged(worldsList.Count - 1);

        worldsListMenu.AddItem((worldsList.Count - 1).ToString(CultureInfo.CurrentCulture), false, Colors.White);
        worldsListMenu.CreateElements();

        UpdateAllWorldsStatistics();
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

    private void SetControlButtonsState(RunControlState runControlState)
    {
        switch (runControlState)
        {
            case RunControlState.Ready:
            {
                finishXGenerationsSpinBox.Editable = true;
                finishXGenerationsButton.Disabled = false;
                runXWorldsSpinBox.Editable = true;
                runXWorldsButton.Disabled = false;
                finishOneGenerationButton.Disabled = false;
                runOneStepButton.Disabled = false;
                playWithCurrentSettingButton.Disabled = false;
                abortButton.Disabled = true;
                worldsListMenu.Disabled = false;
                newWorldButton.Disabled = false;
                break;
            }

            case RunControlState.Running:
            {
                finishXGenerationsSpinBox.Editable = false;
                finishXGenerationsButton.Disabled = true;
                runXWorldsSpinBox.Editable = false;
                runXWorldsButton.Disabled = true;
                finishOneGenerationButton.Disabled = true;
                runOneStepButton.Disabled = true;
                playWithCurrentSettingButton.Disabled = true;
                abortButton.Disabled = false;
                worldsListMenu.Disabled = true;
                newWorldButton.Disabled = true;
                break;
            }

            case RunControlState.Paused:
            {
                finishXGenerationsSpinBox.Editable = true;
                finishXGenerationsButton.Disabled = false;
                runXWorldsSpinBox.Editable = false;
                runXWorldsButton.Disabled = true;
                finishOneGenerationButton.Disabled = false;
                runOneStepButton.Disabled = false;
                playWithCurrentSettingButton.Disabled = false;
                abortButton.Disabled = false;
                worldsListMenu.Disabled = false;
                newWorldButton.Disabled = false;
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

    private void OnRunXWorldsSpinBoxValueChanged(float value)
    {
        runXWorldsButton.Text = TranslationServer.Translate("RUN_X_WORLDS").FormatSafe(Math.Round(value));
    }

    /// <summary>
    ///   Sequentially run X worlds, Y generations each
    /// </summary>
    private void OnRunXWorldsButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        worldsPendingToRun = (int)Math.Round(runXWorldsSpinBox.Value) - 1;
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
        historyListMenu.AddItem(world.CurrentGeneration.ToString(CultureInfo.CurrentCulture), false, Colors.White);
        historyListMenu.CreateElements();

        // Select the current generation
        HistoryListMenuIndexChanged(world.CurrentGeneration);

        world.TotalTimeUsed += autoEvoRun.RunDuration;

        UpdateCurrentWorldStatistics();
        UpdateAllWorldsStatistics();
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
        worldsPendingToRun = 0;

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

        world = worldsList[index];
        worldsListMenu.Text = index.ToString(CultureInfo.CurrentCulture);

        generationDisplayed = world.CurrentGeneration;

        // Rebuild history list
        historyListMenu.ClearAllItems();
        for (int i = 0; i <= world.CurrentGeneration; ++i)
        {
            historyListMenu.AddItem(i.ToString(CultureInfo.CurrentCulture), false, Colors.White);
        }

        historyListMenu.CreateElements();
        HistoryListMenuIndexChanged(world.CurrentGeneration);

        UpdateSpeciesList();
        SpeciesListMenuIndexChanged(0);
        UpdateCurrentWorldStatistics();

        patchMapDrawer.Map = world.GameProperties.GameWorld.Map;
        patchDetailsPanel.SelectedPatch = patchMapDrawer.PlayerPatch;

        world.GameProperties.GameWorld.BuildEvolutionaryTree(evolutionaryTree);

        ready = false;
        InitAutoEvoConfigControls();
        ready = true;

        GD.Print("Selected world ", index);
    }

    private void HistoryListMenuIndexChanged(int index)
    {
        if (generationDisplayed == index && speciesListMenu.Popup.GetItemCount() > 0)
            return;

        historyListMenu.Text = index.ToString(CultureInfo.CurrentCulture);

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

        if (species == speciesDetailsPanel.PreviewSpecies)
            return;

        UpdateSpeciesPreview(species);
    }

    private void UpdateSpeciesPreview(Species species)
    {
        speciesListMenu.Text = species.FormattedName;
        speciesDetailsPanel.PreviewSpecies = species;
    }

    private void EvolutionaryTreeNodeSelected(int generation, uint id)
    {
        HistoryListMenuIndexChanged(generation);
        UpdateSpeciesPreview(world.SpeciesHistoryList[generation][id]);
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

    private void UpdateCurrentWorldStatistics()
    {
        world.UpdateWorldStatistics();

        var bbcode = TranslationServer.Translate("CURRENT_WORLD_STATISTICS").FormatSafe(
            world.CurrentGeneration,
            world.PatchesCount,
            world.TotalTimeUsed.ToString("g", CultureInfo.CurrentCulture),
            world.TotalSpeciesCount,
            world.CurrentSpeciesCount,
            world.PatchSpeciesCountAverage.ToString("F2", CultureInfo.CurrentCulture),
            world.PatchSpeciesCountStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            world.TotalPopulation,
            world.SpeciesHistoryList.Last().Values.OrderByDescending(s => s.Population).First().FormattedNameBbCode,
            world.MicrobeSpeciesAverageHexSize.ToString("F2", CultureInfo.CurrentCulture));

        foreach (var stat in world.MicrobeSpeciesOrganelleStatistics.OrderByDescending(s => s.Value.Percentage))
        {
            bbcode += "\n" + TranslationServer.Translate("MICROBE_ORGANELLE_STATISTICS").FormatSafe(
                stat.Key.NameWithoutSpecialCharacters,
                stat.Value.Percentage.ToString("P", CultureInfo.CurrentCulture),
                stat.Value.Average.ToString("F2", CultureInfo.CurrentCulture));
        }

        currentWorldStatisticsLabel.ExtendedBbcode = bbcode;
    }

    private void UpdateAllWorldsStatistics()
    {
        var worldGenerations = string.Join(", ", worldsList.Select(w => w.CurrentGeneration).OrderBy(g => g));

        var (totalSpeciesAverage, totalSpeciesStandardDeviation) =
            worldsList.Select(w => w.TotalSpeciesCount).CalculateAverageAndStandardDeviation();

        var (speciesStillAliveAverage, speciesStillAliveStandardDeviation) =
            worldsList.Select(w => w.CurrentSpeciesCount).CalculateAverageAndStandardDeviation();

        var (speciesCountPerPatchAverage, speciesCountPerPatchStandardDeviation) =
            worldsList.Select(w => w.PatchSpeciesCountAverage).CalculateAverageAndStandardDeviation();

        var (populationPerPatchAverage, populationPerPatchStandardDeviation) =
            worldsList.Select(w => (double)w.TotalPopulation / w.PatchesCount).CalculateAverageAndStandardDeviation();

        var (microbeSpeciesHexSizeAverage, microbeSpeciesHexSizeStandardDeviation) =
            worldsList.Select(w => w.MicrobeSpeciesAverageHexSize).CalculateAverageAndStandardDeviation();

        var bbcode = TranslationServer.Translate("ALL_WORLDS_STATISTICS").FormatSafe(worldGenerations,
            totalSpeciesAverage.ToString("F2", CultureInfo.CurrentCulture),
            totalSpeciesStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            speciesStillAliveAverage.ToString("F2", CultureInfo.CurrentCulture),
            speciesStillAliveStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            speciesCountPerPatchAverage.ToString("F2", CultureInfo.CurrentCulture),
            speciesCountPerPatchStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            populationPerPatchAverage.ToString("F2", CultureInfo.CurrentCulture),
            populationPerPatchStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            microbeSpeciesHexSizeAverage.ToString("F2", CultureInfo.CurrentCulture),
            microbeSpeciesHexSizeStandardDeviation.ToString("F2", CultureInfo.CurrentCulture));

        foreach (var organelle in SimulationParameters.Instance.GetAllOrganelles())
        {
            var percentage = worldsList.Average(w => w.MicrobeSpeciesOrganelleStatistics[organelle].Percentage);
            var average = worldsList.Average(w => w.MicrobeSpeciesOrganelleStatistics[organelle].Average);
            bbcode += "\n" + TranslationServer.Translate("MICROBE_ORGANELLE_STATISTICS").FormatSafe(
                organelle.NameWithoutSpecialCharacters,
                percentage.ToString("P", CultureInfo.CurrentCulture),
                average.ToString("F2", CultureInfo.CurrentCulture));
        }

        allWorldsStatisticsLabel.ExtendedBbcode = bbcode;
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
        public readonly List<Dictionary<uint, Species>> SpeciesHistoryList = new();

        public readonly List<Dictionary<int, PatchSnapshot>> PatchHistoryList = new();

        /// <summary>
        ///   This list stores all auto-evo results.
        /// </summary>
        public readonly List<LocalizedStringBuilder> RunResultsList = new();

        /// <summary>
        ///   Used to generate world statistics
        /// </summary>
        public readonly Dictionary<OrganelleDefinition, (double Percentage, double Average)>
            MicrobeSpeciesOrganelleStatistics = new();

        /// <summary>
        ///   The current generation auto-evo has evolved
        /// </summary>
        public int CurrentGeneration;

        /// <summary>
        ///   Total used auto-evo time
        /// </summary>
        public TimeSpan TotalTimeUsed = TimeSpan.Zero;

        public AutoEvoExploringToolWorld(IAutoEvoConfiguration configuration)
        {
            AutoEvoConfiguration = configuration.Clone();
            GameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings
                { AutoEvoConfiguration = AutoEvoConfiguration });

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

            PatchesCount = GameProperties.GameWorld.Map.Patches.Count;

            foreach (var organelle in SimulationParameters.Instance.GetAllOrganelles())
            {
                MicrobeSpeciesOrganelleStatistics.Add(organelle, (0, 0));
            }

            UpdateWorldStatistics();
        }

        public int TotalSpeciesCount { get; private set; }

        public int CurrentSpeciesCount { get; private set; }

        public int PatchesCount { get; }

        public double PatchSpeciesCountAverage { get; private set; }

        public double PatchSpeciesCountStandardDeviation { get; private set; }

        public long TotalPopulation { get; private set; }

        public double MicrobeSpeciesAverageHexSize { get; private set; }

        public void UpdateWorldStatistics()
        {
            TotalSpeciesCount = SpeciesHistoryList.SelectMany(s => s.Keys).Distinct().Count();
            CurrentSpeciesCount = SpeciesHistoryList.Last().Values.Count;
            (PatchSpeciesCountAverage, PatchSpeciesCountStandardDeviation) = GameProperties.GameWorld.Map.Patches.Values
                .Select(p => p.SpeciesInPatch.Count).CalculateAverageAndStandardDeviation();
            TotalPopulation = SpeciesHistoryList.Last().Values.Sum(s => s.Population);

            var microbeSpecies = SpeciesHistoryList.Last().Values.Select(s => s as MicrobeSpecies).WhereNotNull()
                .ToList();

            MicrobeSpeciesAverageHexSize = microbeSpecies.Average(s => s.BaseHexSize);

            foreach (var organelle in SimulationParameters.Instance.GetAllOrganelles())
            {
                MicrobeSpeciesOrganelleStatistics[organelle] = (
                    microbeSpecies.Average(s => s.Organelles.Any(o => o.Definition == organelle) ? 1 : 0),
                    microbeSpecies.Average(s => s.Organelles.Count(o => o.Definition == organelle)));
            }
        }
    }
}
