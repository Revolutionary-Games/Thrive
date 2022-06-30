using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;
using Godot.Collections;

public class AutoEvoExploringTool : NodeWithInput
{
    // Tab paths

    [Export]
    public NodePath ConfigEditorPath = null!;

    [Export]
    public NodePath HistoryReportSplitPath = null!;

    [Export]
    public NodePath ReportPath = null!;

    [Export]
    public NodePath ViewerPath = null!;

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

    // Status & control paths

    [Export]
    public NodePath CurrentGenerationLabelPath = null!;

    [Export]
    public NodePath RunStatusLabelPath = null!;

    [Export]
    public NodePath RunGenerationButtonPath = null!;

    [Export]
    public NodePath RunStepButtonPath = null!;

    [Export]
    public NodePath AbortButtonPath = null!;

    [Export]
    public NodePath PlayWithCurrentSettingPath = null!;

    // Report paths

    [Export]
    public NodePath HistoryContainerPath = null!;

    [Export]
    public NodePath ResultsLabelPath = null!;

    // Viewer paths

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexPreviewPath = null!;

    [Export]
    public NodePath SpeciesListPath = null!;

    /// <summary>
    ///   This list stores copy of species in every generation.
    /// </summary>
    private readonly List<System.Collections.Generic.Dictionary<uint, Species>> speciesHistoryList = new();

    /// <summary>
    ///   This list stores all auto-evo results.
    /// </summary>
    private readonly List<LocalizedStringBuilder> runResultsList = new();

    // ButtonGroups to allow checkboxes automatically uncheck
    private readonly ButtonGroup historyCheckBoxesGroup = new();
    private readonly ButtonGroup speciesListCheckBoxesGroup = new();

    // Tabs
    private Control configTab = null!;
    private Control historyReportSplit = null!;
    private Control reportTab = null!;
    private Control viewerTab = null!;

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
    private Button finishOneGenerationButton = null!;
    private Button runOneStepButton = null!;
    private Button abortButton = null!;
    private Button playWithCurrentSettingButton = null!;

    // Report controls
    private VBoxContainer generationHistoryList = null!;
    private CustomRichTextLabel autoEvoResultsLabel = null!;

    // Viewer controls
    private SpeciesPreview speciesPreview = null!;
    private CellHexPreview hexPreview = null!;
    private VBoxContainer speciesList = null!;

    private PackedScene customCheckBoxScene = null!;

    /// <summary>
    ///   The game itself
    /// </summary>
    private GameProperties gameProperties = null!;

    /// <summary>
    ///   Local copy of auto-evo configuration. Used to avoid modifying the global one
    /// </summary>
    private AutoEvoConfiguration autoEvoConfiguration = null!;

    private AutoEvoRun? autoEvoRun;

    /// <summary>
    ///   The current generation auto-evo has evolved
    /// </summary>
    private int currentGeneration;

    /// <summary>
    ///   The generation report & viewer tab is displaying
    /// </summary>
    private int generationDisplayed = -1;

    private bool ready;

    private enum TabIndex
    {
        Config,
        Report,
        Viewer,
    }

    public override void _Ready()
    {
        base._Ready();

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.1f, null, false);

        // Retrieve all node paths

        configTab = GetNode<Control>(ConfigEditorPath);
        historyReportSplit = GetNode<Control>(HistoryReportSplitPath);
        reportTab = GetNode<Control>(ReportPath);
        viewerTab = GetNode<Control>(ViewerPath);

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
        finishOneGenerationButton = GetNode<Button>(RunGenerationButtonPath);
        runOneStepButton = GetNode<Button>(RunStepButtonPath);
        abortButton = GetNode<Button>(AbortButtonPath);
        playWithCurrentSettingButton = GetNode<Button>(PlayWithCurrentSettingPath);

        autoEvoResultsLabel = GetNode<CustomRichTextLabel>(ResultsLabelPath);
        generationHistoryList = GetNode<VBoxContainer>(HistoryContainerPath);

        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexPreview = GetNode<CellHexPreview>(HexPreviewPath);
        speciesList = GetNode<VBoxContainer>(SpeciesListPath);

        customCheckBoxScene = GD.Load<PackedScene>("res://src/gui_common/CustomCheckBox.tscn");

        // Init game
        gameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
        autoEvoConfiguration = (AutoEvoConfiguration)SimulationParameters.Instance.AutoEvoConfiguration.Clone();

        // Init config control values
        allowSpeciesToNotMutateCheckBox.Pressed = autoEvoConfiguration.AllowSpeciesToNotMutate;
        allowSpeciesToNotMigrateCheckBox.Pressed = autoEvoConfiguration.AllowSpeciesToNotMigrate;
        biodiversityAttemptFillChanceSpinBox.Value = autoEvoConfiguration.BiodiversityAttemptFillChance;
        biodiversityFromNeighbourPatchChanceSpinBox.Value = autoEvoConfiguration.BiodiversityFromNeighbourPatchChance;
        biodiversitySplitIsMutatedCheckBox.Pressed = autoEvoConfiguration.BiodiversityNearbyPatchIsFreePopulation;
        biodiversityNearbyPatchIsFreePopulationCheckBox.Pressed = autoEvoConfiguration.BiodiversitySplitIsMutated;
        lowBiodiversityLimitSpinBox.Value = autoEvoConfiguration.LowBiodiversityLimit;
        maximumSpeciesInPatchSpinBox.Value = autoEvoConfiguration.MaximumSpeciesInPatch;
        moveAttemptsPerSpeciesSpinBox.Value = autoEvoConfiguration.MoveAttemptsPerSpecies;
        mutationsPerSpeciesSpinBox.Value = autoEvoConfiguration.MutationsPerSpecies;
        newBiodiversityIncreasingSpeciesPopulationSpinBox.Value =
            autoEvoConfiguration.NewBiodiversityIncreasingSpeciesPopulation;
        protectMigrationsFromSpeciesCapCheckBox.Pressed = autoEvoConfiguration.ProtectMigrationsFromSpeciesCap;
        protectNewCellsFromSpeciesCapCheckBox.Pressed = autoEvoConfiguration.ProtectNewCellsFromSpeciesCap;
        refundMigrationsInExtinctionsCheckBox.Pressed = autoEvoConfiguration.RefundMigrationsInExtinctions;
        strictNicheCompetitionCheckBox.Pressed = autoEvoConfiguration.StrictNicheCompetition;
        speciesSplitByMutationThresholdPopulationAmountSpinBox.Value =
            autoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationAmount;
        speciesSplitByMutationThresholdPopulationFractionSpinBox.Value =
            autoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationFraction;
        useBiodiversityForceSplitCheckBox.Pressed = autoEvoConfiguration.UseBiodiversityForceSplit;

        // Mark ready, later on autoEvoConfiguration values should only be changed from the GUI
        ready = true;
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
                finishOneGenerationButton.Disabled = false;
                runOneStepButton.Disabled = false;
                abortButton.Disabled = true;
            }
            else if (autoEvoRun.Aborted)
            {
                autoEvoRun = null;
                finishOneGenerationButton.Disabled = false;
                runOneStepButton.Disabled = false;
                abortButton.Disabled = true;
            }
        }
    }

    [RunOnKeyDown("ui_cancel")]
    public bool OnBackButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // TODO: Ask to return

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f,
            SceneManager.Instance.ReturnToMenu, false);

        return true;
    }

    private void ChangeTab(int index)
    {
        GUICommon.Instance.PlayButtonPressSound();

        switch ((TabIndex)index)
        {
            case TabIndex.Config:
            {
                historyReportSplit.Visible = false;
                configTab.Visible = true;
                break;
            }

            case TabIndex.Report:
            {
                configTab.Visible = false;
                viewerTab.Visible = false;
                historyReportSplit.Visible = true;
                reportTab.Visible = true;
                break;
            }

            case TabIndex.Viewer:
            {
                reportTab.Visible = false;
                configTab.Visible = false;
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

        autoEvoConfiguration.AllowSpeciesToNotMutate = allowSpeciesToNotMutateCheckBox.Pressed;
        autoEvoConfiguration.AllowSpeciesToNotMigrate = allowSpeciesToNotMigrateCheckBox.Pressed;
        autoEvoConfiguration.BiodiversityAttemptFillChance = (int)biodiversityAttemptFillChanceSpinBox.Value;
        autoEvoConfiguration.BiodiversityFromNeighbourPatchChance =
            (float)biodiversityFromNeighbourPatchChanceSpinBox.Value;
        autoEvoConfiguration.BiodiversityNearbyPatchIsFreePopulation = biodiversitySplitIsMutatedCheckBox.Pressed;
        autoEvoConfiguration.BiodiversitySplitIsMutated = biodiversityNearbyPatchIsFreePopulationCheckBox.Pressed;
        autoEvoConfiguration.LowBiodiversityLimit = (int)lowBiodiversityLimitSpinBox.Value;
        autoEvoConfiguration.MaximumSpeciesInPatch = (int)maximumSpeciesInPatchSpinBox.Value;
        autoEvoConfiguration.MoveAttemptsPerSpecies = (int)moveAttemptsPerSpeciesSpinBox.Value;
        autoEvoConfiguration.MutationsPerSpecies = (int)mutationsPerSpeciesSpinBox.Value;
        autoEvoConfiguration.NewBiodiversityIncreasingSpeciesPopulation =
            (int)newBiodiversityIncreasingSpeciesPopulationSpinBox.Value;
        autoEvoConfiguration.ProtectMigrationsFromSpeciesCap = protectMigrationsFromSpeciesCapCheckBox.Pressed;
        autoEvoConfiguration.ProtectNewCellsFromSpeciesCap = protectNewCellsFromSpeciesCapCheckBox.Pressed;
        autoEvoConfiguration.RefundMigrationsInExtinctions = refundMigrationsInExtinctionsCheckBox.Pressed;
        autoEvoConfiguration.StrictNicheCompetition = strictNicheCompetitionCheckBox.Pressed;
        autoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationAmount =
            (int)speciesSplitByMutationThresholdPopulationAmountSpinBox.Value;
        autoEvoConfiguration.SpeciesSplitByMutationThresholdPopulationFraction =
            (float)speciesSplitByMutationThresholdPopulationFractionSpinBox.Value;
        autoEvoConfiguration.UseBiodiversityForceSplit = useBiodiversityForceSplitCheckBox.Pressed;
    }

    /// <summary>
    ///   Run a new generation or finish the current generation async
    /// </summary>
    private void OnFinishOneGenerationButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (autoEvoRun?.Aborted != false || autoEvoRun.Finished)
        {
            // If the previous one has finished / failed
            autoEvoRun = new AutoEvoRun(gameProperties.GameWorld, autoEvoConfiguration) { FullSpeed = true };
            autoEvoRun.Start();
        }
        else
        {
            // If the previous one is paused
            autoEvoRun.FullSpeed = true;
            autoEvoRun.Continue();
        }

        // Disable these buttons
        finishOneGenerationButton.Disabled = true;
        runOneStepButton.Disabled = true;
        abortButton.Disabled = false;
    }

    /// <summary>
    ///   Apply auto-evo effects and set up related controls
    /// </summary>
    private void ApplyAutoEvoRun()
    {
        // Add run results
        RunResults results = autoEvoRun!.Results!;
        runResultsList.Add(results.MakeSummary(gameProperties.GameWorld.Map, true));
        speciesHistoryList.Add(
            gameProperties.GameWorld.Species.ToDictionary(pair => pair.Key, pair => (Species)pair.Value.Clone()));

        // Add check box to history container
        var checkBox = customCheckBoxScene.Instance<CustomCheckBox>();
        checkBox.Text = (currentGeneration + 1).ToString();
        checkBox.Group = historyCheckBoxesGroup;
        checkBox.Connect("toggled", this, nameof(GenerationCheckBoxToggled),
            new Array { currentGeneration });
        generationHistoryList.AddChild(checkBox);

        // History checkboxes are in one button group, so this automatically releases other buttons
        // History label is updated in button toggled signal callback
        checkBox.Pressed = true;

        // Apply the results
        autoEvoRun.ApplyAllEffects(true);
        currentGenerationLabel.Text = (++currentGeneration).ToString();
    }

    /// <summary>
    ///   Run one step sync
    /// </summary>
    private void OnRunOneStepButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (autoEvoRun?.Aborted != false || autoEvoRun.Finished)
        {
            autoEvoRun = new AutoEvoRun(gameProperties.GameWorld, autoEvoConfiguration);
        }

        // To avoid concurrent steps
        autoEvoRun.FullSpeed = false;
        autoEvoRun.OneStep();
        abortButton.Disabled = false;
    }

    /// <summary>
    ///   Abort the current run
    /// </summary>
    private void OnAbortButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (autoEvoRun?.WasSuccessful == false)
            autoEvoRun.Abort();

        finishOneGenerationButton.Disabled = false;
        runOneStepButton.Disabled = false;
    }

    private void GenerationCheckBoxToggled(bool state, int index)
    {
        if (state && generationDisplayed != index)
        {
            generationDisplayed = index;
            UpdateAutoEvoReport();
            UpdateSpeciesList();
        }
    }

    private void UpdateAutoEvoReport()
    {
        if (generationDisplayed < runResultsList.Count && generationDisplayed > -1)
        {
            autoEvoResultsLabel.ExtendedBbcode = runResultsList[generationDisplayed].ToString();
        }
    }

    private void UpdateSpeciesList()
    {
        // Clear the current ones
        speciesList.FreeChildren();

        foreach (var pair in speciesHistoryList[generationDisplayed])
        {
            var checkBox = customCheckBoxScene.Instance<CustomCheckBox>();
            checkBox.Text = $"{pair.Key.ToString()}: {pair.Value.FormattedName}";
            checkBox.Group = speciesListCheckBoxesGroup;
            checkBox.Connect("toggled", this, nameof(SpeciesListCheckBoxToggled), new Array { pair.Key });
            speciesList.AddChild(checkBox);
        }
    }

    private void SpeciesListCheckBoxToggled(bool state, uint speciesIndex)
    {
        var species = speciesHistoryList[generationDisplayed][speciesIndex];

        if (state)
        {
            speciesPreview.PreviewSpecies = species;
            hexPreview.PreviewSpecies = species as MicrobeSpecies;
        }
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

            // Start freebuild game
            editor.CurrentGame = gameProperties;

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
    }
}
