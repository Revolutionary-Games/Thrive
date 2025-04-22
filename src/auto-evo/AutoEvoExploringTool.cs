using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
public partial class AutoEvoExploringTool : NodeWithInput, ISpeciesDataProvider
{
    private readonly List<AutoEvoExploringToolWorld> worldsList = new();

#pragma warning disable CA2213

    // Tabs
    [Export]
    private Control worldTab = null!;
    [Export]
    private Control configTab = null!;
    [Export]
    private Control historyReportSplit = null!;
    [Export]
    private Control speciesSelectPanel = null!;
    [Export]
    private Control mapTab = null!;
    [Export]
    private Control reportTab = null!;
    [Export]
    private Control viewerTab = null!;
    [Export]
    private Control micheTab = null!;

    // World controls
    [Export]
    private CustomRichTextLabel allWorldsStatisticsLabel = null!;
    [Export]
    private CustomDropDown worldsListMenu = null!;
    [Export]
    private TextureButton newWorldButton = null!;
    [Export]
    private CustomRichTextLabel currentWorldStatisticsLabel = null!;
    [Export]
    private Button worldExportButton = null!;

    // Auto-evo parameters controls.
    [Export]
    private SpinBox moveAttemptsPerSpeciesSpinBox = null!;
    [Export]
    private SpinBox mutationsPerSpeciesSpinBox = null!;
    [Export]
    private CheckBox strictNicheCompetitionCheckBox = null!;

    // Status controls
    [Export]
    private Label runStatusLabel = null!;
    [Export]
    private SpinBox finishXGenerationsSpinBox = null!;
    [Export]
    private Button finishXGenerationsButton = null!;
    [Export]
    private SpinBox runXWorldsSpinBox = null!;
    [Export]
    private Button runXWorldsButton = null!;
    [Export]
    private Button finishOneGenerationButton = null!;
    [Export]
    private Button runOneStepButton = null!;
    [Export]
    private Button abortButton = null!;
    [Export]
    private Button playWithCurrentSettingButton = null!;

    // Report controls
    [Export]
    private CustomRichTextLabel autoEvoResultsLabel = null!;
    [Export]
    private CustomDropDown historyListMenu = null!;

    // Map controls
    [Export]
    private PatchMapDrawer patchMapDrawer = null!;
    [Export]
    private PatchDetailsPanel patchDetailsPanel = null!;

    // Viewer controls
    [Export]
    private CustomDropDown speciesListMenu = null!;
    [Export]
    private EvolutionaryTree evolutionaryTree = null!;
    [Export]
    private SpeciesDetailsPanelWithFossilisation speciesDetailsPanelWithFossilisation = null!;

    // Miche controls
    [Export]
    private CustomDropDown patchListMenu = null!;
    [Export]
    private MicheTree micheTree = null!;
    [Export]
    private MicheDetailsPanel micheDetailsPanel = null!;
    [Export]
    private SpeciesDetailsPanel micheSpeciesDetailsPanel = null!;

    [Export]
    private CustomConfirmationDialog exitConfirmationDialog = null!;
    [Export]
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

    /// <summary>
    ///   The patch that the miche tab is displaying,
    ///   which equals to the selected popup item index of <see cref="patchListMenu"/>
    /// </summary>
    private Patch patchDisplayed = null!;

    private bool ready;

    private enum TabIndex
    {
        World,
        Config,
        Map,
        Report,
        Viewer,
        Miche,
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

        patchMapDrawer.OnSelectedPatchChanged += UpdatePatchDetailPanel;

        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().ToList();

        ThriveopediaManager.ReportNonThriveopediaSpeciesDataProvider(this);

        // Init button translation
        OnFinishXGenerationsSpinBoxValueChanged((float)finishXGenerationsSpinBox.Value);
        OnRunXWorldsSpinBoxValueChanged((float)runXWorldsSpinBox.Value);

        // Connect custom dropdown handler
        historyListMenu.Popup.Connect(PopupMenu.SignalName.IndexPressed,
            new Callable(this, nameof(HistoryListMenuIndexChanged)));
        speciesListMenu.Popup.Connect(PopupMenu.SignalName.IndexPressed,
            new Callable(this, nameof(SpeciesListMenuIndexChanged)));
        worldsListMenu.Popup.Connect(PopupMenu.SignalName.IndexPressed,
            new Callable(this, nameof(WorldsListMenuIndexChanged)));
        patchListMenu.Popup.Connect(PopupMenu.SignalName.IndexPressed,
            new Callable(this, nameof(PatchListMenuIndexChanged)));

        InitNewWorld();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Abort the current run to avoid problems
        autoEvoRun?.Abort();

        ThriveopediaManager.RemoveNonThriveopediaSpeciesDataProvider(this);
    }

    public override void _Process(double delta)
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

    [RunOnKeyDown("ui_cancel")]
    public void AskExit()
    {
        exitConfirmationDialog.PopupCenteredShrink();
    }

    public Species? GetActiveSpeciesData(uint speciesId)
    {
        return GetSpeciesDataFromGeneration(generationDisplayed, speciesId);
    }

    /// <summary>
    ///   Returns a record of the species from the given generation or earlier
    /// </summary>
    public Species? GetSpeciesDataFromGeneration(int generation, uint speciesId)
    {
        var gameWorld = world.GameProperties.GameWorld;

        for (int i = generation; i >= 0; --i)
        {
            gameWorld.GenerationHistory[i].AllSpeciesData
                .TryGetValue(speciesId, out var speciesRecord);

            var species = speciesRecord?.Species;

            if (species != null)
                return species;

            // If species of speciesRecord is null, then the species should have data in an earlier generation
        }

        return null;
    }

    private void InitNewWorld()
    {
        InitNewWorld(SimulationParameters.Instance.AutoEvoConfiguration);
    }

    private void InitNewWorld(IAutoEvoConfiguration configuration)
    {
        var newWorld = new AutoEvoExploringToolWorld(configuration);
        newWorld.GameProperties.GameWorld.Map.RevealAllPatches();
        worldsList.Add(newWorld);
        WorldsListMenuIndexChanged(worldsList.Count - 1);

        worldsListMenu.AddItem((worldsList.Count - 1).ToString(CultureInfo.CurrentCulture), false, Colors.White);
        worldsListMenu.CreateElements();

        UpdateAllWorldsStatistics();
    }

    private void InitAutoEvoConfigControls()
    {
        moveAttemptsPerSpeciesSpinBox.Value = world.AutoEvoConfiguration.MoveAttemptsPerSpecies;
        mutationsPerSpeciesSpinBox.Value = world.AutoEvoConfiguration.MutationsPerSpecies;
        strictNicheCompetitionCheckBox.ButtonPressed = world.AutoEvoConfiguration.StrictNicheCompetition;
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
                micheTab.Visible = false;
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
                micheTab.Visible = false;
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
                micheTab.Visible = false;
                break;
            }

            case TabIndex.Miche:
            {
                worldTab.Visible = false;
                reportTab.Visible = false;
                mapTab.Visible = false;
                configTab.Visible = false;
                speciesSelectPanel.Visible = true;
                historyReportSplit.Visible = true;
                viewerTab.Visible = false;
                micheTab.Visible = true;
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

        world.AutoEvoConfiguration.MoveAttemptsPerSpecies = (int)moveAttemptsPerSpeciesSpinBox.Value;
        world.AutoEvoConfiguration.MutationsPerSpecies = (int)mutationsPerSpeciesSpinBox.Value;
        world.AutoEvoConfiguration.StrictNicheCompetition = strictNicheCompetitionCheckBox.ButtonPressed;
    }

    private void OnFinishXGenerationsSpinBoxValueChanged(float value)
    {
        finishXGenerationsButton.Text =
            Localization.Translate("FINISH_X_GENERATIONS").FormatSafe(Math.Round(value));
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
        runXWorldsButton.Text = Localization.Translate("RUN_X_WORLDS").FormatSafe(Math.Round(value));
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
            autoEvoRun = new AutoEvoRun(world.GameProperties.GameWorld,
                AutoEvoRun.GetGlobalCache(autoEvoRun, world.WorldSettings)) { FullSpeed = true };
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

        // This needs to be called before making any auto-evo summaries
        autoEvoRun.CalculateAndApplyFinalExternalEffectSizes();

        // Make summary, this must be called before results are applied so that summary is correct
        results.RegisterNewSpeciesForSummary(gameWorld);
        results.StorePreviousPopulations(gameWorld.Map);
        world.RunResultsList.Add(results.MakeSummary(true));

        // Apply the results (and pass time for world effects)
        gameWorld.OnTimePassed(1);
        autoEvoRun.ApplyAllResults(true);
        ++gameWorld.PlayerSpecies.Generation;
        ++world.CurrentGeneration;

        // Add run results, this must be called after results are applied to generate unique species ID
        gameWorld.GenerationHistory.Add(gameWorld.PlayerSpecies.Generation - 1,
            new GenerationRecord(gameWorld.TotalPassedTime, results.GetSpeciesRecords()));
        gameWorld.BuildEvolutionaryTree(evolutionaryTree);
        world.SpeciesHistoryList.Add(gameWorld.Species.ToDictionary(s => s.Key,
            s => (Species)s.Value.Clone()));
        world.PatchHistoryList.Add(gameWorld.Map.Patches.ToDictionary(s => s.Key,
            s => (PatchSnapshot)s.Value.CurrentSnapshot.Clone()));
        world.MicheHistoryList.Add(results.InspectPatchMicheData().ToDictionary(s => s.Key,
            s => s.Value.DeepCopy()));

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
            autoEvoRun = new AutoEvoRun(world.GameProperties.GameWorld,
                AutoEvoRun.GetGlobalCache(autoEvoRun, world.WorldSettings));
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
        runStatusLabel.Text = Localization.Translate("READY");

        world = worldsList[index];
        worldsListMenu.Text = index.ToString(CultureInfo.CurrentCulture);

        generationDisplayed = world.CurrentGeneration;

        micheDetailsPanel.WorldSettings = world.WorldSettings;

        patchMapDrawer.Map = world.GameProperties.GameWorld.Map;
        patchMapDrawer.SelectedPatch = patchMapDrawer.PlayerPatch;
        patchDetailsPanel.SelectedPatch = patchMapDrawer.PlayerPatch;

        // Rebuild history list
        historyListMenu.ClearAllItems();
        for (int i = 0; i <= world.CurrentGeneration; ++i)
        {
            historyListMenu.AddItem(i.ToString(CultureInfo.CurrentCulture), false, Colors.White);
        }

        historyListMenu.CreateElements();
        HistoryListMenuIndexChanged(world.CurrentGeneration);

        // Rebuild patch list
        patchListMenu.ClearAllItems();
        foreach (var pair in world.GameProperties.GameWorld.Map.Patches.OrderBy(p => p.Value.Name.ToString()))
        {
            patchListMenu.AddItem(pair.Value.Name.ToString(), false, Colors.White);
        }

        patchListMenu.CreateElements();

        UpdateSpeciesList();
        SpeciesListMenuIndexChanged(0);
        UpdateCurrentWorldStatistics();
        patchMapDrawer.UpdatePatchEvents();

        if (patchMapDrawer.PlayerPatch != null)
            PatchListMenuUpdate(patchMapDrawer.PlayerPatch);

        world.GameProperties.GameWorld.BuildEvolutionaryTree(evolutionaryTree);

        ready = false;
        InitAutoEvoConfigControls();
        ready = true;

        GD.Print("Selected world ", index);
    }

    private void HistoryListMenuIndexChanged(int index)
    {
        if (generationDisplayed == index && speciesListMenu.Popup.ItemCount > 0)
            return;

        historyListMenu.Text = index.ToString(CultureInfo.CurrentCulture);

        generationDisplayed = index;
        UpdateAutoEvoReport();
        UpdateSpeciesList();
        UpdatePatchDetailPanel(patchMapDrawer);
        patchMapDrawer.UpdatePatchEvents();
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

        if (species == speciesDetailsPanelWithFossilisation.PreviewSpecies)
            return;

        UpdateSpeciesPreview(species);
    }

    private void UpdateSpeciesPreview(Species? species)
    {
        if (species == null)
        {
            ResetSpeciesPreview();
            return;
        }

        speciesListMenu.Text = species.FormattedName;
        speciesDetailsPanelWithFossilisation.PreviewSpecies = species;
    }

    private void ResetSpeciesPreview()
    {
        speciesListMenu.Text = string.Empty;
        speciesDetailsPanelWithFossilisation.PreviewSpecies = null;
    }

    private void EvolutionaryTreeNodeSelected(int generation, uint id)
    {
        HistoryListMenuIndexChanged(generation);

        var species = GetSpeciesDataFromGeneration(generation, id);

        UpdateSpeciesPreview(species);
    }

    private void PatchListMenuIndexChanged(int index)
    {
        var patchName = patchListMenu.Popup.GetItemText(index);
        var selectedPatch = world.GameProperties.GameWorld.Map.Patches.Values
            .First(p => p.Name.ToString() == patchName);

        // Get current snapshot
        var patch = new Patch(selectedPatch.Name, 0, selectedPatch.BiomeTemplate, selectedPatch.BiomeType,
            world.PatchHistoryList[generationDisplayed][selectedPatch.ID])
        {
            TimePeriod = selectedPatch.TimePeriod,
            Depth = { [0] = selectedPatch.Depth[0], [1] = selectedPatch.Depth[1] },
            Visibility = selectedPatch.Visibility,
        };

        PatchListMenuUpdate(patch);
    }

    private void PatchListMenuUpdate(Patch patch)
    {
        patchDisplayed = patch;
        patchListMenu.Text = patch.Name.ToString();

        if (generationDisplayed != world.CurrentGeneration &&
            world.MicheHistoryList[generationDisplayed].TryGetValue(patch, out Miche? miche))
        {
            micheTree.SetMiche(miche);
        }
        else
        {
            var cache = new SimulationCache(world.WorldSettings);
            var globalCache = new AutoEvoGlobalCache(world.WorldSettings);

            var generateMiche = new GenerateMiche(patch, cache, globalCache);
            var newMiche = generateMiche.GenerateMicheTree(globalCache);

            generateMiche.PopulateMiche(newMiche);

            micheTree.SetMiche(newMiche);
        }
    }

    private void MicheTreeNodeSelected(int micheHash)
    {
        if (!micheTree.MicheByHash.TryGetValue(micheHash, out var micheData))
        {
            throw new ArgumentException("Invalid hash passed into MicheTreeNodeSelected");
        }

        // NoOps are being used to hold species nodes
        if (micheData.Pressure.GetType() == typeof(NoOpPressure))
        {
            if (micheData.Occupant == null)
            {
                // No species selected so reset the display panel to not make the GUI as confusing
                micheSpeciesDetailsPanel.Visible = false;
                micheDetailsPanel.Visible = false;
                return;
            }

            micheSpeciesDetailsPanel.Visible = true;
            micheDetailsPanel.Visible = false;

            Species species;
            if (generationDisplayed == world.CurrentGeneration)
            {
                species = patchDisplayed.SpeciesInPatch.Keys.First(p => p.ID == micheData.Occupant.ID);
            }
            else
            {
                species = world.SpeciesHistoryList[generationDisplayed].Values
                    .First(p => p.ID == micheData.Occupant.ID);
            }

            micheSpeciesDetailsPanel.PreviewSpecies = species;
        }
        else
        {
            micheSpeciesDetailsPanel.Visible = false;
            micheDetailsPanel.Visible = true;

            micheDetailsPanel.SetPreview(micheData, patchDisplayed);
        }
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
            Visibility = selectedPatch.Visibility,
        };

        patchDetailsPanel.SelectedPatch = patch;
        PatchListMenuUpdate(patch);
    }

    private void PlayWithCurrentSettingPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Disable the button to prevent it being executed again.
        playWithCurrentSettingButton.Disabled = true;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, () =>
        {
            MainMenu.OnEnteringGame();

            // Instantiate a new editor scene
            var editor = (MicrobeEditor)SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instantiate();

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

        var bbcode = Localization.Translate("CURRENT_WORLD_STATISTICS").FormatSafe(world.CurrentGeneration,
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
            bbcode += "\n" + Localization.Translate("MICROBE_ORGANELLE_STATISTICS").FormatSafe(
                stat.Key.NameWithoutSpecialCharacters,
                stat.Value.Percentage.ToString("P", CultureInfo.CurrentCulture),
                stat.Value.Average.ToString("F2", CultureInfo.CurrentCulture));
        }

        bbcode += "\n\n" + Localization.Translate("MICROBE_ORGANELLE_UPGRADES_STATISTICS");

        foreach (var stat in world.MicrobeSpeciesUpgradesStatistics.OrderByDescending(s => s.Value.Percentage))
        {
            bbcode += "\n" + Localization.Translate("MICROBE_ORGANELLE_STATISTICS").FormatSafe(stat.Value.Name,
                stat.Value.Percentage.ToString("P", CultureInfo.CurrentCulture),
                stat.Value.Average.ToString("F2", CultureInfo.CurrentCulture));
        }

        bbcode += "\n\n" + Localization.Translate("MICROBE_MEMBRANE_STATISTICS");

        foreach (var stat in world.MicrobeSpeciesMembranesStatistics.OrderByDescending(s => s.Value))
        {
            bbcode += "\n" + Localization.Translate("MICROBE_MEMBRANE_PERCENTAGE_STATISTICS").FormatSafe(stat.Key,
                stat.Value.ToString("P", CultureInfo.CurrentCulture));
        }

        bbcode += "\n\n" + Localization.Translate("MICROBE_ENZYME_STATISTICS");

        foreach (var stat in world.MicrobeSpeciesEnzymesStatistics.OrderByDescending(s => s.Value.Percentage))
        {
            bbcode += "\n" + Localization.Translate("MICROBE_ORGANELLE_STATISTICS").FormatSafe(stat.Key.Name,
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

        var stringBuilder = new StringBuilder(1000);
        stringBuilder.Append(Localization.Translate("ALL_WORLDS_STATISTICS").FormatSafe(worldGenerations,
            totalSpeciesAverage.ToString("F2", CultureInfo.CurrentCulture),
            totalSpeciesStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            speciesStillAliveAverage.ToString("F2", CultureInfo.CurrentCulture),
            speciesStillAliveStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            speciesCountPerPatchAverage.ToString("F2", CultureInfo.CurrentCulture),
            speciesCountPerPatchStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            populationPerPatchAverage.ToString("F2", CultureInfo.CurrentCulture),
            populationPerPatchStandardDeviation.ToString("F2", CultureInfo.CurrentCulture),
            microbeSpeciesHexSizeAverage.ToString("F2", CultureInfo.CurrentCulture),
            microbeSpeciesHexSizeStandardDeviation.ToString("F2", CultureInfo.CurrentCulture)));

        foreach (var organelle in SimulationParameters.Instance.GetAllOrganelles())
        {
            var percentage = worldsList.Average(w => w.MicrobeSpeciesOrganelleStatistics[organelle].Percentage);
            var average = worldsList.Average(w => w.MicrobeSpeciesOrganelleStatistics[organelle].Average);
            stringBuilder.Append("\n");
            stringBuilder.Append(Localization.Translate("MICROBE_ORGANELLE_STATISTICS").FormatSafe(
                organelle.NameWithoutSpecialCharacters,
                percentage.ToString("P", CultureInfo.CurrentCulture),
                average.ToString("F2", CultureInfo.CurrentCulture)));
        }

        stringBuilder.Append("\n\n");
        stringBuilder.Append(Localization.Translate("MICROBE_ORGANELLE_UPGRADES_STATISTICS"));

        foreach (var upgradeName in world.MicrobeSpeciesUpgradesStatistics.Keys)
        {
            var percentage = worldsList.Average(w => w.MicrobeSpeciesUpgradesStatistics[upgradeName].Percentage);
            var average = worldsList.Average(w => w.MicrobeSpeciesUpgradesStatistics[upgradeName].Average);
            stringBuilder.Append("\n");
            stringBuilder.Append(Localization.Translate("MICROBE_ORGANELLE_STATISTICS").FormatSafe(
                worldsList[0].MicrobeSpeciesUpgradesStatistics[upgradeName].Name,
                percentage.ToString("P", CultureInfo.CurrentCulture),
                average.ToString("F2", CultureInfo.CurrentCulture)));
        }

        stringBuilder.Append("\n\n");
        stringBuilder.Append(Localization.Translate("MICROBE_MEMBRANE_STATISTICS"));

        foreach (var upgradeName in world.MicrobeSpeciesMembranesStatistics.Keys)
        {
            var percentage = worldsList.Average(w => w.MicrobeSpeciesMembranesStatistics[upgradeName]);
            stringBuilder.Append("\n");
            stringBuilder.Append("  ");
            stringBuilder.Append(Localization.Translate("MICROBE_MEMBRANE_PERCENTAGE_STATISTICS").FormatSafe(
                upgradeName, percentage.ToString("P", CultureInfo.CurrentCulture)));
        }

        stringBuilder.Append("\n\n");
        stringBuilder.Append(Localization.Translate("MICROBE_ENZYME_STATISTICS"));

        foreach (var upgradeName in world.MicrobeSpeciesEnzymesStatistics.Keys)
        {
            var percentage = worldsList.Average(w => w.MicrobeSpeciesEnzymesStatistics[upgradeName].Percentage);
            var average = worldsList.Average(w => w.MicrobeSpeciesEnzymesStatistics[upgradeName].Average);
            stringBuilder.Append("\n");
            stringBuilder.Append(Localization.Translate("MICROBE_ORGANELLE_STATISTICS").FormatSafe(upgradeName.Name,
                percentage.ToString("P", CultureInfo.CurrentCulture),
                average.ToString("F2", CultureInfo.CurrentCulture)));
        }

        allWorldsStatisticsLabel.ExtendedBbcode = stringBuilder.ToString();
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

        public readonly WorldGenerationSettings WorldSettings;

        /// <summary>
        ///   The game itself
        /// </summary>
        public readonly GameProperties GameProperties;

        /// <summary>
        ///   This list stores copy of species in every generation.
        /// </summary>
        public readonly List<Dictionary<uint, Species>> SpeciesHistoryList = new();

        public readonly List<Dictionary<int, PatchSnapshot>> PatchHistoryList = new();
        public readonly List<Dictionary<Patch, Miche>> MicheHistoryList = new();

        /// <summary>
        ///   This list stores all auto-evo results.
        /// </summary>
        public readonly List<LocalizedStringBuilder> RunResultsList = new();

        /// <summary>
        ///   Used to generate organelle statistics
        /// </summary>
        public readonly Dictionary<OrganelleDefinition, (double Percentage, double Average)>
            MicrobeSpeciesOrganelleStatistics = new();

        /// <summary>
        ///   Used to generate organelle upgrade statistics
        /// </summary>
        public readonly Dictionary<string, (string Name, double Percentage, double Average)>
            MicrobeSpeciesUpgradesStatistics = new();

        /// <summary>
        ///   Used to generate membrane statistics
        /// </summary>
        public readonly Dictionary<string, double>
            MicrobeSpeciesMembranesStatistics = new();

        /// <summary>
        ///   Used to generate enzymes statistics
        /// </summary>
        public readonly Dictionary<Enzyme, (double Percentage, double Average)>
            MicrobeSpeciesEnzymesStatistics = new();

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
            WorldSettings = new WorldGenerationSettings { AutoEvoConfiguration = AutoEvoConfiguration };
            GameProperties = GameProperties.StartNewMicrobeGame(WorldSettings);

            RunResultsList.Add(new LocalizedStringBuilder());
            SpeciesHistoryList.Add(new Dictionary<uint, Species>
            {
                {
                    GameProperties.GameWorld.PlayerSpecies.ID,
                    (Species)GameProperties.GameWorld.PlayerSpecies.Clone()
                },
            });

            PatchHistoryList.Add(GameProperties.GameWorld.Map.Patches.ToDictionary(p => p.Key,
                p => (PatchSnapshot)p.Value.CurrentSnapshot.Clone()));

            PatchesCount = GameProperties.GameWorld.Map.Patches.Count;

            foreach (var organelle in SimulationParameters.Instance.GetAllOrganelles())
            {
                MicrobeSpeciesOrganelleStatistics.Add(organelle, (0, 0));
                foreach (var upgrade in organelle.AvailableUpgrades.Keys)
                {
                    organelle.AvailableUpgrades.TryGetValue(upgrade, out var upgradeName);
                    if (upgradeName != null)
                    {
                        MicrobeSpeciesUpgradesStatistics.TryAdd(upgrade, (upgradeName.Name, 0, 0));
                    }
                }
            }

            MicrobeSpeciesUpgradesStatistics.Remove("none");

            foreach (var membrane in SimulationParameters.Instance.GetAllMembranes())
            {
                MicrobeSpeciesMembranesStatistics.TryAdd(membrane.Name, 0);
            }

            foreach (var enzyme in SimulationParameters.Instance.GetAllEnzymes())
            {
                MicrobeSpeciesEnzymesStatistics.TryAdd(enzyme, (0, 0));
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

            foreach (var upgradeName in MicrobeSpeciesUpgradesStatistics.Keys)
            {
                MicrobeSpeciesUpgradesStatistics[upgradeName] = (
                    MicrobeSpeciesUpgradesStatistics[upgradeName].Name,
                    microbeSpecies.Average(s =>
                        s.Organelles.Any(o => o.Upgrades?.UnlockedFeatures.Contains(upgradeName) ?? false) ? 1 : 0),
                    microbeSpecies.Average(s =>
                        s.Organelles.Count(o => o.Upgrades?.UnlockedFeatures.Contains(upgradeName) ?? false)));
            }

            foreach (var membrane in SimulationParameters.Instance.GetAllMembranes())
            {
                MicrobeSpeciesMembranesStatistics[membrane.Name] =
                    microbeSpecies.Average(s => s.MembraneType == membrane ? 1 : 0);
            }

            foreach (var enzyme in MicrobeSpeciesEnzymesStatistics.Keys)
            {
                MicrobeSpeciesEnzymesStatistics[enzyme] = (
                    microbeSpecies.Average(s => s.Organelles.Any(o =>
                        o.Definition.Enzymes.TryGetValue(enzyme, out var value) && value > 0) ?
                        1 :
                        0),
                    microbeSpecies.Average(s => s.Organelles.Count(o =>
                        o.Definition.Enzymes.TryGetValue(enzyme, out var value) && value > 0)));
            }
        }
    }
}
