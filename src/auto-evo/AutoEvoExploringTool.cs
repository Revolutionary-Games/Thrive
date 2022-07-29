using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Auto-evo exploring tool is a scene that contains numerous tool to debug and exploring auto-evo.
///   You can change the parameters to see how auto-evo responds, you can get a clearer view of how species evolves,
///   and you can start a game based on the current progress.
/// </summary>
public class AutoEvoExploringTool : NodeWithInput
{
    // Tab paths

    [Export]
    public NodePath ConfigEditorPath = null!;

    [Export]
    public NodePath HistoryReportSplitPath = null!;

    [Export]
    public NodePath SpeciesSelectPanelPath = null!;

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

    // Status and control paths

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

    // Exit confirm path
    [Export]
    public NodePath ExitConfirmationDialogPath = null!;

    /// <summary>
    ///   This list stores copy of species in every generation.
    /// </summary>
    private readonly List<Dictionary<uint, Species>> speciesHistoryList = new();

    /// <summary>
    ///   This list stores all auto-evo results.
    /// </summary>
    private readonly List<LocalizedStringBuilder> runResultsList = new();

    // Tabs
    private Control configTab = null!;
    private Control historyReportSplit = null!;
    private Control speciesSelectPanel = null!;
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
    private CustomRichTextLabel autoEvoResultsLabel = null!;
    private CustomDropDown historyListMenu = null!;

    // Viewer controls
    private SpeciesPreview speciesPreview = null!;
    private CellHexPreview hexPreview = null!;
    private CustomDropDown speciesListMenu = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private EvolutionaryTree evolutionaryTree = null!;

    private CustomConfirmationDialog exitConfirmationDialog = null!;

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
    ///   The generation that report and viewer tab is displaying,
    ///   which equals to the selected popup item index of <see cref="historyListMenu"/>
    /// </summary>
    private int generationDisplayed;

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
        speciesSelectPanel = GetNode<Control>(SpeciesSelectPanelPath);
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
        historyListMenu = GetNode<CustomDropDown>(HistoryListMenuPath);

        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexPreview = GetNode<CellHexPreview>(HexPreviewPath);
        speciesListMenu = GetNode<CustomDropDown>(SpeciesListMenuPath);
        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
        evolutionaryTree = GetNode<EvolutionaryTree>(EvolutionaryTreePath);

        exitConfirmationDialog = GetNode<CustomConfirmationDialog>(ExitConfirmationDialogPath);

        // Init game
        gameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
        autoEvoConfiguration = (AutoEvoConfiguration)SimulationParameters.Instance.AutoEvoConfiguration.Clone();

        InitLucaGeneration();

        evolutionaryTree.Init(gameProperties.GameWorld.PlayerSpecies);

        InitConfigControl();

        // Connect custom dropdown handler
        historyListMenu.Popup.Connect("index_pressed", this, nameof(HistoryListMenuIndexChanged));
        speciesListMenu.Popup.Connect("index_pressed", this, nameof(SpeciesListMenuIndexChanged));

        // Mark ready, later on autoEvoConfiguration values should only be changed through GUI
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
                playWithCurrentSettingButton.Disabled = false;
                abortButton.Disabled = true;
            }
            else if (autoEvoRun.Aborted)
            {
                autoEvoRun = null;
                finishOneGenerationButton.Disabled = false;
                runOneStepButton.Disabled = false;
                playWithCurrentSettingButton.Disabled = false;
                abortButton.Disabled = true;
            }
        }
    }

    [RunOnKeyDown("ui_cancel")]
    public void AskExit()
    {
        exitConfirmationDialog.PopupCenteredShrink();
    }

    private void InitConfigControl()
    {
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
    }

    private void InitLucaGeneration()
    {
        runResultsList.Add(new LocalizedStringBuilder());
        speciesHistoryList.Add(new Dictionary<uint, Species>
        {
            { gameProperties.GameWorld.PlayerSpecies.ID, gameProperties.GameWorld.PlayerSpecies },
        });
        historyListMenu.AddItem("0", false, Colors.White);
        historyListMenu.CreateElements();
        HistoryListMenuIndexChanged(0);
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
                speciesSelectPanel.Visible = false;
                historyReportSplit.Visible = true;
                reportTab.Visible = true;
                break;
            }

            case TabIndex.Viewer:
            {
                reportTab.Visible = false;
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
        playWithCurrentSettingButton.Disabled = true;
        abortButton.Disabled = false;
    }

    /// <summary>
    ///   Apply auto-evo effects and set up related controls
    /// </summary>
    private void ApplyAutoEvoRun()
    {
        var results = autoEvoRun!.Results!;

        // Make summary, this must be called before results are applied so that summary is correct
        runResultsList.Add(results.MakeSummary(gameProperties.GameWorld.Map, true));

        // Apply the results
        autoEvoRun.ApplyAllResultsAndEffects(true);

        // Add run results, this must be called after results are applied to generate unique species ID
        evolutionaryTree.UpdateEvolutionaryTreeWithRunResults(results, ++currentGeneration);
        speciesHistoryList.Add(
            gameProperties.GameWorld.Species.ToDictionary(pair => pair.Key, pair => (Species)pair.Value.Clone()));

        // Add checkbox to history container
        historyListMenu.AddItem(currentGeneration.ToString(), false, Colors.White);
        historyListMenu.CreateElements();

        currentGenerationLabel.Text = currentGeneration.ToString();
    }

    /// <summary>
    ///   Run one step synchronously
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
        playWithCurrentSettingButton.Disabled = false;
    }

    private void HistoryListMenuIndexChanged(int index)
    {
        if (generationDisplayed == index && speciesListMenu.Popup.GetItemCount() > 0)
            return;

        historyListMenu.Text = index.ToString();

        generationDisplayed = index;
        UpdateAutoEvoReport();
        UpdateSpeciesList();
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
        speciesListMenu.ClearAllItems();

        foreach (var pair in speciesHistoryList[generationDisplayed].OrderBy(p => p.Value.FormattedName))
        {
            speciesListMenu.AddItem(pair.Value.FormattedName, false, Colors.White);
        }

        speciesListMenu.CreateElements();
    }

    private void SpeciesListMenuIndexChanged(int index)
    {
        var speciesName = speciesListMenu.Popup.GetItemText(index);
        var species = speciesHistoryList[generationDisplayed].Values.First(p => p.FormattedName == speciesName);

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
            hexPreview.PreviewSpecies = microbeSpecies;
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
        UpdateSpeciesPreview(speciesHistoryList[generation][id]);
    }

    private void UpdateSpeciesDetail(Species species)
    {
        /*
          [b]Species:[/b]
            {0}:{1}
          [b]Generation:[/b]
            {2}
          [b]Population:[/b]
            {3}
          [b]Colour:[/b]
            #{4}
          [b]Behaviour[/b]
            {5}
        */
        speciesDetailsLabel.ExtendedBbcode = TranslationServer.Translate("SPECIES_DETAIL_TEXT").FormatSafe(
            species.FormattedNameBbCode, species.ID, species.Generation, species.Population, species.Colour.ToHtml(),
            string.Join("\n  ", species.Behaviour.Select(b => b.Key + ": " + b.Value)));

        switch (species)
        {
            case MicrobeSpecies microbeSpecies:
            {
                /*
                  [b]Stage:[/b]
                    Microbe
                  [b]Membrane Type:[/b]
                    {0}
                  [b]Membrane Rigidity:[/b]
                    {1}
                  [b]Base Speed:[/b]
                    {2}
                  [b]Base Rotation Speed[/b]
                    {3}
                  [b]Base Hex Size[/b]
                    {4}
                */
                speciesDetailsLabel.ExtendedBbcode += TranslationServer.Translate("MICROBE_SPECIES_DETAIL_TEXT")
                    .FormatSafe(microbeSpecies.MembraneType.Name, microbeSpecies.MembraneRigidity,
                        microbeSpecies.BaseSpeed, microbeSpecies.BaseRotationSpeed, microbeSpecies.BaseHexSize);
                break;
            }
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

            gameProperties.EnterFreeBuild();

            // Copy our currently setup game to the editor
            editor.CurrentGame = gameProperties;

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
    }
}
