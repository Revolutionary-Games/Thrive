using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;
using Godot.Collections;

public class AutoEvoExploringTool : ControlWithInput
{
    // All auto-evo config paths

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

    // Status paths

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

    // Report paths

    [Export]
    public NodePath HistoryContainerPath = null!;

    [Export]
    public NodePath ResultsLabelPath = null!;

    // Tab paths

    [Export]
    public NodePath ConfigEditorPath = null!;

    [Export]
    public NodePath ReportPath = null!;

    [Export]
    public NodePath ViewerPath = null!;

    // All auto-evo config related controls.
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

    // Auto-evo status related controls
    private Label currentGenerationLabel = null!;
    private Label runStatusLabel = null!;
    private Button runGenerationButton = null!;
    private Button runStepButton = null!;
    private Button abortButton = null!;

    // Auto-evo report related controls
    private VBoxContainer historyContainer = null!;
    private CustomRichTextLabel resultsLabel = null!;

    // Tabs
    private Control configEditorTab = null!;
    private Control reportTab = null!;
    private Control viewerTab = null!;
    private List<Control> tabsList = null!;

    private GameProperties gameProperties = null!;
    private AutoEvoConfiguration autoEvoConfiguration = null!;
    private AutoEvoRun? autoEvoRun;
    private bool initialized;
    private int currentGeneration = 0;
    private List<LocalizedStringBuilder> runResultsList = new List<LocalizedStringBuilder>();
    private List<Button> historyButtons = new List<Button>();
    private int currentDisplayed;

    [Signal]
    public delegate void OnAutoEvoExploringToolClosed();

    public override void _Ready()
    {
        base._Ready();

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
        runGenerationButton = GetNode<Button>(RunGenerationButtonPath);
        runStepButton = GetNode<Button>(RunStepButtonPath);
        abortButton = GetNode<Button>(AbortButtonPath);

        resultsLabel = GetNode<CustomRichTextLabel>(ResultsLabelPath);
        historyContainer = GetNode<VBoxContainer>(HistoryContainerPath);

        configEditorTab = GetNode<Control>(ConfigEditorPath);
        reportTab = GetNode<Control>(ReportPath);
        viewerTab = GetNode<Control>(ViewerPath);
        tabsList = new List<Control> { configEditorTab, reportTab, viewerTab };

        Init();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!initialized)
            return;

        if (autoEvoRun != null)
        {
            runStatusLabel.Text = autoEvoRun.Status;

            if (autoEvoRun.WasSuccessful)
            {
                // Add run results
                RunResults results = autoEvoRun.Results!;
                runResultsList.Add(results.MakeSummary(gameProperties.GameWorld.Map, true));

                // Add button to history container
                var button = new Button { Text = currentGeneration.ToString(), ToggleMode = true };
                button.Connect("toggled", this, nameof(HistoryButtonToggled),
                    new Godot.Collections.Array { currentGeneration });

                historyButtons.Add(button);
                historyContainer.AddChild(button);

                // Display the most recent result
                ChangeReportDisplayed(currentGeneration);

                // Apply the results
                autoEvoRun.ApplyAllEffects(true);
                currentGenerationLabel.Text = (++currentGeneration).ToString();

                // Clear autoEvoRun and enable buttons to allow the next run to start.
                autoEvoRun = null;
                runGenerationButton.Disabled = false;
                runStepButton.Disabled = false;
            }
            else if (autoEvoRun.Aborted)
            {
                // Clear autoEvoRun and enable buttons to allow the next run to start.
                autoEvoRun = null;
                runGenerationButton.Disabled = false;
                runStepButton.Disabled = false;
            }
        }
    }

    public void OpenFromMainMenu()
    {
        if (Visible)
            return;

        Init();

        Show();
    }

    [RunOnKeyDown("ui_cancel")]
    public bool OnBackButtonPressed()
    {
        if (!Visible)
            return false;

        initialized = false;
        autoEvoConfiguration = null!;
        gameProperties = null!;
        EmitSignal(nameof(OnAutoEvoExploringToolClosed));
        return true;
    }

    private void Init()
    {
        currentGeneration = 0;
        currentGenerationLabel.Text = currentGeneration.ToString();
        runStatusLabel.Text = TranslationServer.Translate("READY");
        gameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
        autoEvoConfiguration = (AutoEvoConfiguration)SimulationParameters.Instance.AutoEvoConfiguration.Clone();

        // Init all config controls
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

        initialized = true;
    }

    /// <summary>
    ///   This function updates all configurations in a row to avoid adding numerous separate callback functions.
    /// </summary>
    /// <param name="value">
    ///   Godot Signal parameter,
    ///   'state' from Button::toggled or 'value' from SpinBox::value_changed
    /// </param>
    private void UpdateAutoEvoConfiguration(object? value = null)
    {
        _ = value;

        if (!initialized)
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
    ///   Run a new generation or finish the current generation
    /// </summary>
    private void OnRunGenerationButtonPressed()
    {
        // If the previous one has finished / failed
        if (autoEvoRun?.Aborted != false || autoEvoRun.Finished)
        {
            autoEvoRun = new AutoEvoRun(gameProperties.GameWorld, autoEvoConfiguration) { FullSpeed = true };
            autoEvoRun.Start();
        }
        else
        {
            autoEvoRun.FullSpeed = true;
            autoEvoRun.Continue();
        }

        // Disable these buttons
        runGenerationButton.Disabled = true;
        runStepButton.Disabled = true;
    }

    private void OnRunStepButtonPressed()
    {
        if (autoEvoRun?.Aborted != false || autoEvoRun.Finished)
        {
            autoEvoRun = new AutoEvoRun(gameProperties.GameWorld, autoEvoConfiguration);
        }

        autoEvoRun.FullSpeed = false;
        autoEvoRun.OneStep();
    }

    private void OnAbortButtonPressed()
    {
        if (autoEvoRun?.WasSuccessful == false)
            autoEvoRun.Abort();

        runGenerationButton.Disabled = false;
        runStepButton.Disabled = false;
    }

    private void UpdateResults()
    {
        if (currentDisplayed < runResultsList.Count)
            resultsLabel.ExtendedBbcode = runResultsList[currentDisplayed].ToString();
    }

    private void ChangeTab(int index)
    {
        foreach (Control tab in tabsList)
            tab.Visible = false;

        tabsList[index].Visible = true;
    }

    private void HistoryButtonToggled(bool state, int index)
    {
        if (state == false)
        {
            // If only one button is checked
            if (currentDisplayed == index)
                historyButtons[index].Pressed = true;

            return;
        }

        ChangeReportDisplayed(index);
    }

    private void ChangeReportDisplayed(int index)
    {
        if (currentDisplayed == index)
            return;

        currentDisplayed = index;

        foreach (var button in historyButtons)
            button.Pressed = false;

        historyButtons[index].Pressed = true;

        UpdateResults();
    }
}
