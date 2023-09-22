using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

public class NewGameSettings : ControlWithInput
{
    /// <summary>
    ///   When true this menu works differently to facilitate beginning the microbe stage in a descended game
    /// </summary>
    [Export]
    public bool Descending;

    [Export]
    public NodePath? BasicOptionsPath;

    [Export]
    public NodePath AdvancedOptionsPath = null!;

    [Export]
    public NodePath BasicButtonPath = null!;

    [Export]
    public NodePath BackButtonPath = null!;

    [Export]
    public NodePath AdvancedButtonPath = null!;

    [Export]
    public NodePath TabButtonsPath = null!;

    [Export]
    public NodePath DifficultyTabPath = null!;

    [Export]
    public NodePath PlanetTabPath = null!;

    [Export]
    public NodePath MiscTabPath = null!;

    [Export]
    public NodePath DifficultyTabButtonPath = null!;

    [Export]
    public NodePath PlanetTabButtonPath = null!;

    [Export]
    public NodePath MiscTabButtonPath = null!;

    [Export]
    public NodePath DifficultyPresetButtonPath = null!;

    [Export]
    public NodePath DifficultyPresetAdvancedButtonPath = null!;

    [Export]
    public NodePath MPMultiplierPath = null!;

    [Export]
    public NodePath MPMultiplierReadoutPath = null!;

    [Export]
    public NodePath MutationRatePath = null!;

    [Export]
    public NodePath MutationRateReadoutPath = null!;

    [Export]
    public NodePath CompoundDensityPath = null!;

    [Export]
    public NodePath CompoundDensityReadoutPath = null!;

    [Export]
    public NodePath PlayerDeathPopulationPenaltyPath = null!;

    [Export]
    public NodePath PlayerDeathPopulationPenaltyReadoutPath = null!;

    [Export]
    public NodePath GlucoseDecayRatePath = null!;

    [Export]
    public NodePath GlucoseDecayRateReadoutPath = null!;

    [Export]
    public NodePath OsmoregulationMultiplierPath = null!;

    [Export]
    public NodePath OsmoregulationMultiplierReadoutPath = null!;

    [Export]
    public NodePath FreeGlucoseCloudButtonPath = null!;

    [Export]
    public NodePath PassiveReproductionButtonPath = null!;

    [Export]
    public NodePath PatchMapExplorationModePath = null!;

    [Export]
    public NodePath PatchMapExplorationModeDescriptionPath = null!;

    [Export]
    public NodePath LimitGrowthRateButtonPath = null!;

    [Export]
    public NodePath LifeOriginButtonPath = null!;

    [Export]
    public NodePath LifeOriginButtonAdvancedPath = null!;

    [Export]
    public NodePath LAWKButtonPath = null!;

    [Export]
    public NodePath LAWKAdvancedButtonPath = null!;

    [Export]
    public NodePath DayNightCycleButtonPath = null!;

    [Export]
    public NodePath DayLengthContainerPath = null!;

    [Export]
    public NodePath DayLengthPath = null!;

    [Export]
    public NodePath DayLengthReadoutPath = null!;

    [Export]
    public NodePath GameSeedPath = null!;

    [Export]
    public NodePath GameSeedAdvancedPath = null!;

    [Export]
    public NodePath IncludeMulticellularButtonPath = null!;

    [Export]
    public NodePath EasterEggsButtonPath = null!;

    [Export]
    public NodePath StartButtonPath = null!;

    [Export]
    public NodePath CheckOptionsMenuAdviceContainerPath = null!;

#pragma warning disable CA2213

    // Main controls
    private PanelContainer basicOptions = null!;
    private PanelContainer advancedOptions = null!;
    private TabButtons tabButtons = null!;
    private Control difficultyTab = null!;
    private Control planetTab = null!;
    private Control miscTab = null!;
    private Button difficultyTabButton = null!;
    private Button planetTabButton = null!;
    private Button miscTabButton = null!;
    private Button basicButton = null!;
    private Button advancedButton = null!;
    private Button backButton = null!;
    private Button startButton = null!;

    // Difficulty controls
    private OptionButton difficultyPresetButton = null!;
    private OptionButton difficultyPresetAdvancedButton = null!;
    private HSlider mpMultiplier = null!;
    private LineEdit mpMultiplierReadout = null!;
    private HSlider aiMutationRate = null!;
    private LineEdit aiMutationRateReadout = null!;
    private HSlider compoundDensity = null!;
    private LineEdit compoundDensityReadout = null!;
    private HSlider playerDeathPopulationPenalty = null!;
    private LineEdit playerDeathPopulationPenaltyReadout = null!;
    private HSlider glucoseDecayRate = null!;
    private LineEdit glucoseDecayRateReadout = null!;
    private HSlider osmoregulationMultiplier = null!;
    private LineEdit osmoregulationMultiplierReadout = null!;
    private Button freeGlucoseCloudButton = null!;
    private Button passiveReproductionButton = null!;
    private Button limitGrowthRateButton = null!;
    private OptionButton patchMapExplorationMode = null!;
    private Label patchMapExplorationModeDescription = null!;

    // Planet controls
    private OptionButton lifeOriginButton = null!;
    private OptionButton lifeOriginButtonAdvanced = null!;
    private Button lawkButton = null!;
    private Button lawkAdvancedButton = null!;
    private Button dayNightCycleButton = null!;
    private HSlider dayLength = null!;
    private LineEdit dayLengthReadout = null!;
    private VBoxContainer dayLengthContainer = null!;
    private LineEdit gameSeed = null!;
    private LineEdit gameSeedAdvanced = null!;

    // Misc controls
    private Button includeMulticellularButton = null!;
    private Button easterEggsButton = null!;

    // Other
    private Container checkOptionsMenuAdviceContainer = null!;
#pragma warning restore CA2213

    private SelectedOptionsTab selectedOptionsTab;

    /// <summary>
    ///   If not null this is used as the base to start a new descended game
    /// </summary>
    private GameProperties? descendedGame;

    private int latestValidSeed;

    private IEnumerable<DifficultyPreset> difficultyPresets = null!;
    private DifficultyPreset normal = null!;
    private DifficultyPreset custom = null!;

    [Signal]
    public delegate void OnNewGameSettingsClosed();

    [Signal]
    public delegate void OnWantToSwitchToOptionsMenu();

    [Signal]
    public delegate void OnNewGameVideoStarted();

    private enum SelectedOptionsTab
    {
        Difficulty,
        Planet,
        Miscellaneous,
    }

    public override void _Ready()
    {
        basicOptions = GetNode<PanelContainer>(BasicOptionsPath);
        advancedOptions = GetNode<PanelContainer>(AdvancedOptionsPath);
        basicButton = GetNode<Button>(BasicButtonPath);
        advancedButton = GetNode<Button>(AdvancedButtonPath);
        tabButtons = GetNode<TabButtons>(TabButtonsPath);
        difficultyTab = GetNode<Control>(DifficultyTabPath);
        planetTab = GetNode<Control>(PlanetTabPath);
        miscTab = GetNode<Control>(MiscTabPath);
        difficultyTabButton =
            GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, DifficultyTabButtonPath));
        planetTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, PlanetTabButtonPath));
        miscTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, MiscTabButtonPath));

        difficultyPresetButton = GetNode<OptionButton>(DifficultyPresetButtonPath);
        difficultyPresetAdvancedButton = GetNode<OptionButton>(DifficultyPresetAdvancedButtonPath);
        mpMultiplier = GetNode<HSlider>(MPMultiplierPath);
        mpMultiplierReadout = GetNode<LineEdit>(MPMultiplierReadoutPath);
        aiMutationRate = GetNode<HSlider>(MutationRatePath);
        aiMutationRateReadout = GetNode<LineEdit>(MutationRateReadoutPath);
        compoundDensity = GetNode<HSlider>(CompoundDensityPath);
        compoundDensityReadout = GetNode<LineEdit>(CompoundDensityReadoutPath);
        playerDeathPopulationPenalty = GetNode<HSlider>(PlayerDeathPopulationPenaltyPath);
        playerDeathPopulationPenaltyReadout = GetNode<LineEdit>(PlayerDeathPopulationPenaltyReadoutPath);
        glucoseDecayRate = GetNode<HSlider>(GlucoseDecayRatePath);
        glucoseDecayRateReadout = GetNode<LineEdit>(GlucoseDecayRateReadoutPath);
        osmoregulationMultiplier = GetNode<HSlider>(OsmoregulationMultiplierPath);
        osmoregulationMultiplierReadout = GetNode<LineEdit>(OsmoregulationMultiplierReadoutPath);
        freeGlucoseCloudButton = GetNode<Button>(FreeGlucoseCloudButtonPath);
        passiveReproductionButton = GetNode<Button>(PassiveReproductionButtonPath);
        patchMapExplorationMode = GetNode<OptionButton>(PatchMapExplorationModePath);
        patchMapExplorationModeDescription = GetNode<Label>(PatchMapExplorationModeDescriptionPath);
        limitGrowthRateButton = GetNode<Button>(LimitGrowthRateButtonPath);
        lifeOriginButton = GetNode<OptionButton>(LifeOriginButtonPath);
        lifeOriginButtonAdvanced = GetNode<OptionButton>(LifeOriginButtonAdvancedPath);
        lawkButton = GetNode<Button>(LAWKButtonPath);
        lawkAdvancedButton = GetNode<Button>(LAWKAdvancedButtonPath);
        dayNightCycleButton = GetNode<Button>(DayNightCycleButtonPath);
        dayLengthContainer = GetNode<VBoxContainer>(DayLengthContainerPath);
        dayLength = GetNode<HSlider>(DayLengthPath);
        dayLengthReadout = GetNode<LineEdit>(DayLengthReadoutPath);
        gameSeed = GetNode<LineEdit>(GameSeedPath);
        gameSeedAdvanced = GetNode<LineEdit>(GameSeedAdvancedPath);
        includeMulticellularButton = GetNode<Button>(IncludeMulticellularButtonPath);
        easterEggsButton = GetNode<Button>(EasterEggsButtonPath);
        backButton = GetNode<Button>(BackButtonPath);
        startButton = GetNode<Button>(StartButtonPath);

        mpMultiplier.MinValue = Constants.MIN_MP_MULTIPLIER;
        mpMultiplier.MaxValue = Constants.MAX_MP_MULTIPLIER;
        aiMutationRate.MinValue = Constants.MIN_AI_MUTATION_RATE;
        aiMutationRate.MaxValue = Constants.MAX_AI_MUTATION_RATE;
        compoundDensity.MinValue = Constants.MIN_COMPOUND_DENSITY;
        compoundDensity.MaxValue = Constants.MAX_COMPOUND_DENSITY;
        playerDeathPopulationPenalty.MinValue = Constants.MIN_PLAYER_DEATH_POPULATION_PENALTY;
        playerDeathPopulationPenalty.MaxValue = Constants.MAX_PLAYER_DEATH_POPULATION_PENALTY;
        glucoseDecayRate.MinValue = Constants.MIN_GLUCOSE_DECAY * 100;
        glucoseDecayRate.MaxValue = Constants.MAX_GLUCOSE_DECAY * 100;
        osmoregulationMultiplier.MinValue = Constants.MIN_OSMOREGULATION_MULTIPLIER;
        osmoregulationMultiplier.MaxValue = Constants.MAX_OSMOREGULATION_MULTIPLIER;

        checkOptionsMenuAdviceContainer = GetNode<Container>(CheckOptionsMenuAdviceContainerPath);

        var simulationParameters = SimulationParameters.Instance;

        difficultyPresets = simulationParameters.GetAllDifficultyPresets();
        normal = simulationParameters.GetDifficultyPreset("normal");
        custom = simulationParameters.GetDifficultyPreset("custom");

        foreach (var preset in difficultyPresets.OrderBy(p => p.Index))
        {
            // The untranslated name will be translated automatically by Godot during runtime
            difficultyPresetButton.AddItem(preset.UntranslatedName);
            difficultyPresetAdvancedButton.AddItem(preset.UntranslatedName);
        }

        patchMapExplorationMode.AddItem(TranslationServer.Translate("FOG_OF_WAR_DISABLED"));
        patchMapExplorationMode.AddItem(TranslationServer.Translate("FOG_OF_WAR_LIGHT"));
        patchMapExplorationMode.AddItem(TranslationServer.Translate("FOG_OF_WAR_INTENSE"));

        // Do this in case default values in NewGameSettings.tscn don't match the normal preset
        InitialiseToPreset(normal);

        var seed = GenerateNewRandomSeed();
        gameSeed.Text = seed;
        gameSeedAdvanced.Text = seed;
        SetSeed(seed);

        // Make sure non-lawk options are disabled if lawk is set to true on start-up
        UpdateLifeOriginOptions(lawkButton.Pressed);

        if (Descending)
        {
            backButton.Visible = false;
            checkOptionsMenuAdviceContainer.Visible = false;
        }
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.SUBMENU_CANCEL_PRIORITY)]
    public bool OnEscapePressed()
    {
        // Only handle keypress when visible
        if (!Visible)
            return false;

        if (!Exit())
        {
            // We are prevented from exiting, consume this input
            return true;
        }

        return true;
    }

    public void OpenFromMainMenu()
    {
        // Shouldn't do anything if options is already open.
        if (Visible)
            return;

        Show();
    }

    public void OpenFromDescendScreen(GameProperties currentGame)
    {
        if (!Descending)
            GD.PrintErr("Incorrectly configured new game settings opened for descending");

        GD.Print("Opening new game for descending, overriding current settings");

        descendedGame = currentGame;
        Show();

        var settings = descendedGame.GameWorld.WorldSettings;
        var difficulty = settings.Difficulty;

        // Override the settings that were set by this opening as default to keep the settings consistent with the
        // previous game
        mpMultiplier.Value = difficulty.MPMultiplier;
        aiMutationRate.Value = difficulty.AIMutationMultiplier;
        compoundDensity.Value = difficulty.CompoundDensity;
        playerDeathPopulationPenalty.Value = difficulty.PlayerDeathPopulationPenalty;
        glucoseDecayRate.Value = difficulty.GlucoseDecay * 100;
        osmoregulationMultiplier.Value = difficulty.OsmoregulationMultiplier;
        freeGlucoseCloudButton.Pressed = difficulty.FreeGlucoseCloud;
        passiveReproductionButton.Pressed = difficulty.PassiveReproduction;
        limitGrowthRateButton.Pressed = difficulty.LimitGrowthRate;
        patchMapExplorationMode.Selected = (int)difficulty.PatchMapExplorationMode;

        UpdatePatchMapExplorationModeDescription(patchMapExplorationMode.Selected);

        UpdateSelectedDifficultyPresetControl();

        lifeOriginButton.Selected = (int)settings.Origin;

        lawkButton.Pressed = settings.LAWK;
        dayNightCycleButton.Pressed = settings.DayNightCycleEnabled;
        dayLength.Value = settings.DayLength;

        // Copy the seed from the settings, as there isn't one method to set this, this is done a bit clumsily like
        // this
        var seedText = settings.Seed.ToString();
        gameSeed.Text = seedText;
        gameSeedAdvanced.Text = seedText;
        SetSeed(seedText);

        // Always set prototypes to true as the player must have been there to descend
        includeMulticellularButton.Pressed = true;

        // And also turn LAWK off because if the player initially played with it on they'll probably want to experience
        // what they missed now. If they still wanted to play with LAWK on they can just put the checkbox back
        lawkButton.Pressed = false;

        easterEggsButton.Pressed = settings.EasterEggs;
    }

    public void ReportValidityOfGameSeed(bool valid)
    {
        if (valid)
        {
            GUICommon.MarkInputAsValid(gameSeed);
            GUICommon.MarkInputAsValid(gameSeedAdvanced);
            startButton.Disabled = false;
            startButton.HintTooltip = TranslationServer.Translate("CONFIRM_NEW_GAME_BUTTON_TOOLTIP");
        }
        else
        {
            GUICommon.MarkInputAsInvalid(gameSeed);
            GUICommon.MarkInputAsInvalid(gameSeedAdvanced);
            startButton.Disabled = true;
            startButton.HintTooltip = TranslationServer.Translate("CONFIRM_NEW_GAME_BUTTON_TOOLTIP_DISABLED");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (BasicOptionsPath != null)
            {
                BasicOptionsPath.Dispose();
                AdvancedOptionsPath.Dispose();
                BasicButtonPath.Dispose();
                AdvancedButtonPath.Dispose();
                TabButtonsPath.Dispose();
                DifficultyTabPath.Dispose();
                PlanetTabPath.Dispose();
                MiscTabPath.Dispose();
                DifficultyTabButtonPath.Dispose();
                PlanetTabButtonPath.Dispose();
                MiscTabButtonPath.Dispose();
                DifficultyPresetButtonPath.Dispose();
                DifficultyPresetAdvancedButtonPath.Dispose();
                MPMultiplierPath.Dispose();
                MPMultiplierReadoutPath.Dispose();
                MutationRatePath.Dispose();
                MutationRateReadoutPath.Dispose();
                CompoundDensityPath.Dispose();
                CompoundDensityReadoutPath.Dispose();
                PlayerDeathPopulationPenaltyPath.Dispose();
                PlayerDeathPopulationPenaltyReadoutPath.Dispose();
                GlucoseDecayRatePath.Dispose();
                GlucoseDecayRateReadoutPath.Dispose();
                OsmoregulationMultiplierPath.Dispose();
                OsmoregulationMultiplierReadoutPath.Dispose();
                FreeGlucoseCloudButtonPath.Dispose();
                PassiveReproductionButtonPath.Dispose();
                PatchMapExplorationModePath.Dispose();
                PatchMapExplorationModeDescriptionPath.Dispose();
                LimitGrowthRateButtonPath.Dispose();
                LifeOriginButtonPath.Dispose();
                LifeOriginButtonAdvancedPath.Dispose();
                LAWKButtonPath.Dispose();
                LAWKAdvancedButtonPath.Dispose();
                DayNightCycleButtonPath.Dispose();
                DayLengthContainerPath.Dispose();
                DayLengthPath.Dispose();
                DayLengthReadoutPath.Dispose();
                GameSeedPath.Dispose();
                GameSeedAdvancedPath.Dispose();
                IncludeMulticellularButtonPath.Dispose();
                EasterEggsButtonPath.Dispose();
                BackButtonPath.Dispose();
                StartButtonPath.Dispose();
                CheckOptionsMenuAdviceContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void InitialiseToPreset(DifficultyPreset preset)
    {
        OnDifficultyPresetSelected(preset.Index);
    }

    private string GenerateNewRandomSeed()
    {
        var random = new Random();
        return random.Next().ToString();
    }

    private void SetSeed(string text)
    {
        bool valid = int.TryParse(text, out int seed) && seed > 0;
        ReportValidityOfGameSeed(valid);
        if (valid)
            latestValidSeed = seed;
    }

    /// <summary>
    ///   Changes the active settings tab that is displayed, or returns if the tab is already active.
    /// </summary>
    private void ChangeSettingsTab(string newTabName)
    {
        // Convert from the string binding to an enum.
        SelectedOptionsTab selection = (SelectedOptionsTab)Enum.Parse(typeof(SelectedOptionsTab), newTabName);

        // Pressing the same button that's already active, so just return.
        if (selection == selectedOptionsTab)
            return;

        difficultyTab.Hide();
        planetTab.Hide();
        miscTab.Hide();

        switch (selection)
        {
            case SelectedOptionsTab.Difficulty:
                difficultyTab.Show();
                difficultyTabButton.Pressed = true;
                break;
            case SelectedOptionsTab.Planet:
                planetTab.Show();
                planetTabButton.Pressed = true;
                break;
            case SelectedOptionsTab.Miscellaneous:
                miscTab.Show();
                miscTabButton.Pressed = true;
                break;
            default:
                GD.PrintErr("Invalid tab");
                break;
        }

        GUICommon.Instance.PlayButtonPressSound();
        selectedOptionsTab = selection;
    }

    private void StartGame()
    {
        var settings = new WorldGenerationSettings();

        var difficulty = SimulationParameters.Instance.GetDifficultyPresetByIndex(difficultyPresetButton.Selected);

        if (difficulty.InternalName == custom.InternalName)
        {
            var customDifficulty = new CustomDifficulty
            {
                MPMultiplier = (float)mpMultiplier.Value,
                AIMutationMultiplier = (float)aiMutationRate.Value,
                CompoundDensity = (float)compoundDensity.Value,
                PlayerDeathPopulationPenalty = (float)playerDeathPopulationPenalty.Value,
                GlucoseDecay = (float)glucoseDecayRate.Value * 0.01f,
                OsmoregulationMultiplier = (float)osmoregulationMultiplier.Value,
                FreeGlucoseCloud = freeGlucoseCloudButton.Pressed,
                PassiveReproduction = passiveReproductionButton.Pressed,
                LimitGrowthRate = limitGrowthRateButton.Pressed,
                PatchMapExplorationMode = (FogOfWarMode)patchMapExplorationMode.Selected,
            };

            settings.Difficulty = customDifficulty;
        }
        else
        {
            settings.Difficulty = difficulty;
        }

        settings.Origin = (WorldGenerationSettings.LifeOrigin)lifeOriginButton.Selected;
        settings.LAWK = lawkButton.Pressed;
        settings.DayNightCycleEnabled = dayNightCycleButton.Pressed;
        settings.DayLength = (int)dayLength.Value;
        settings.Seed = latestValidSeed;

        settings.IncludeMulticellular = includeMulticellularButton.Pressed;
        settings.EasterEggs = easterEggsButton.Pressed;

        // Stop music for the video (stop is used instead of pause to stop the menu music playing a bit after the video
        // before the stage music starts)
        Jukebox.Instance.Stop(true);

        void OnStartGame()
        {
            MainMenu.OnEnteringGame();

            // TODO: Add loading screen while changing between scenes
            var microbeStage = (MicrobeStage)SceneManager.Instance.LoadScene(MainGameState.MicrobeStage).Instance();
            microbeStage.CurrentGame = GameProperties.StartNewMicrobeGame(settings);

            if (descendedGame != null)
            {
                GD.Print("Applying old game data to starting microbe stage");
                microbeStage.CurrentGame.BecomeDescendedVersionOf(descendedGame);
            }

            SceneManager.Instance.SwitchToScene(microbeStage);
        }

        if (Settings.Instance.PlayMicrobeIntroVideo && LaunchOptions.VideosEnabled &&
            SafeModeStartupHandler.AreVideosAllowed())
        {
            TransitionManager.Instance.AddSequence(TransitionManager.Instance.CreateScreenFade(
                ScreenFade.FadeType.FadeOut, 1.5f), () =>
            {
                // Notify that the video now starts to allow the main menu to hide its expensive 3D rendering
                EmitSignal(nameof(OnNewGameVideoStarted));
            });

            TransitionManager.Instance.AddSequence(TransitionManager.Instance.CreateCutscene(
                "res://assets/videos/microbe_intro2.ogv", 0.65f), OnStartGame, true, false);
        }
        else
        {
            // People who disable the cutscene are impatient anyway so use a reduced fade time
            TransitionManager.Instance.AddSequence(TransitionManager.Instance.CreateScreenFade(
                ScreenFade.FadeType.FadeOut, 0.2f), OnStartGame);
        }
    }

    // GUI Control Callbacks

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        Exit();
    }

    private bool Exit()
    {
        EmitSignal(nameof(OnNewGameSettingsClosed));
        return true;
    }

    private void OnAdvancedPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SetAdvancedView(true);
    }

    private void OnBasicPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SetAdvancedView(false);
    }

    private void SetAdvancedView(bool advanced)
    {
        advancedButton.Visible = !advanced;
        basicOptions.Visible = !advanced;
        backButton.Visible = !advanced && !Descending;
        basicButton.Visible = advanced;
        advancedOptions.Visible = advanced;
        tabButtons.Visible = advanced;
    }

    private void OnConfirmPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        StartGame();

        // Disable the button to prevent it being executed again.
        startButton.Disabled = true;
    }

    private void OnDifficultyPresetSelected(int index)
    {
        // Set both buttons here as we only received a signal from one of them
        difficultyPresetButton.Selected = index;
        difficultyPresetAdvancedButton.Selected = index;

        var preset = SimulationParameters.Instance.GetDifficultyPresetByIndex(index);

        // If custom was selected, open the advanced view to the difficulty tab
        if (preset.InternalName == custom.InternalName)
        {
            ChangeSettingsTab(SelectedOptionsTab.Difficulty.ToString());
            SetAdvancedView(true);
            return;
        }

        mpMultiplier.Value = preset.MPMultiplier;
        aiMutationRate.Value = preset.AIMutationMultiplier;
        compoundDensity.Value = preset.CompoundDensity;
        playerDeathPopulationPenalty.Value = preset.PlayerDeathPopulationPenalty;
        glucoseDecayRate.Value = preset.GlucoseDecay * 100;
        osmoregulationMultiplier.Value = preset.OsmoregulationMultiplier;
        freeGlucoseCloudButton.Pressed = preset.FreeGlucoseCloud;
        passiveReproductionButton.Pressed = preset.PassiveReproduction;
        limitGrowthRateButton.Pressed = preset.LimitGrowthRate;
        patchMapExplorationMode.Selected = (int)preset.PatchMapExplorationMode;

        UpdatePatchMapExplorationModeDescription(patchMapExplorationMode.Selected);

        UpdateSelectedDifficultyPresetControl();
    }

    private void UpdateSelectedDifficultyPresetControl()
    {
        foreach (var preset in difficultyPresets)
        {
            // Ignore custom until the end
            if (preset.InternalName == custom.InternalName)
                continue;

            if (Math.Abs((float)mpMultiplier.Value - preset.MPMultiplier) > MathUtils.EPSILON)
                continue;

            if (Math.Abs((float)aiMutationRate.Value - preset.AIMutationMultiplier) > MathUtils.EPSILON)
                continue;

            if (Math.Abs((float)compoundDensity.Value - preset.CompoundDensity) > MathUtils.EPSILON)
                continue;

            if (Math.Abs((float)playerDeathPopulationPenalty.Value - preset.PlayerDeathPopulationPenalty)
                > MathUtils.EPSILON)
                continue;

            if ((int)glucoseDecayRate.Value != (int)(preset.GlucoseDecay * 100))
                continue;

            if (Math.Abs((float)osmoregulationMultiplier.Value - preset.OsmoregulationMultiplier) > MathUtils.EPSILON)
                continue;

            if (freeGlucoseCloudButton.Pressed != preset.FreeGlucoseCloud)
                continue;

            if (passiveReproductionButton.Pressed != preset.PassiveReproduction)
                continue;

            if (limitGrowthRateButton.Pressed != preset.LimitGrowthRate)
                continue;

            if (patchMapExplorationMode.Selected != (int)preset.PatchMapExplorationMode)
                continue;

            // If all values are equal to the values for a preset, use that preset
            difficultyPresetButton.Selected = preset.Index;
            difficultyPresetAdvancedButton.Selected = preset.Index;
            return;
        }

        // If there is no preset with all values equal to the values set, use custom
        difficultyPresetButton.Selected = custom.Index;
        difficultyPresetAdvancedButton.Selected = custom.Index;
    }

    private void OnMPMultiplierValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        mpMultiplierReadout.Text = amount.ToString(CultureInfo.CurrentCulture);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnAIMutationRateValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        aiMutationRateReadout.Text = amount.ToString(CultureInfo.CurrentCulture);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnCompoundDensityValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        compoundDensityReadout.Text = amount.ToString(CultureInfo.CurrentCulture);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnPlayerDeathPopulationPenaltyValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        playerDeathPopulationPenaltyReadout.Text = amount.ToString(CultureInfo.CurrentCulture);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnGlucoseDecayRateValueChanged(double percentage)
    {
        percentage = Math.Round(percentage, 2);
        glucoseDecayRateReadout.Text = TranslationServer.Translate("PERCENTAGE_VALUE").FormatSafe(percentage);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnOsmoregulationMultiplierValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        osmoregulationMultiplierReadout.Text = amount.ToString(CultureInfo.CurrentCulture);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnFreeGlucoseCloudToggled(bool pressed)
    {
        _ = pressed;
        UpdateSelectedDifficultyPresetControl();
    }

    private void OnPassiveReproductionToggled(bool pressed)
    {
        _ = pressed;
        UpdateSelectedDifficultyPresetControl();
    }

    private void OnGrowthRateToggled(bool pressed)
    {
        _ = pressed;
        UpdateSelectedDifficultyPresetControl();
    }

    private void OnLifeOriginSelected(int index)
    {
        // Set both buttons here as we only received a signal from one of them
        lifeOriginButton.Selected = index;
        lifeOriginButtonAdvanced.Selected = index;
    }

    private void OnPatchMapExplorationModeSelected(int index)
    {
        patchMapExplorationMode.Selected = index;

        UpdatePatchMapExplorationModeDescription(index);

        UpdateSelectedDifficultyPresetControl();
    }

    private void UpdatePatchMapExplorationModeDescription(int index)
    {
        switch (index)
        {
            case 0:
                patchMapExplorationModeDescription.Text =
                    TranslationServer.Translate("FOG_OF_WAR_DISABLED_DESCRIPTION");
                break;
            case 1:
                patchMapExplorationModeDescription.Text =
                    TranslationServer.Translate("FOG_OF_WAR_LIGHT_DESCRIPTION");
                break;
            case 2:
                patchMapExplorationModeDescription.Text =
                    TranslationServer.Translate("FOG_OF_WAR_INTENSE_DESCRIPTION");
                break;
        }
    }

    // This and a few other callbacks are not currently needed to detect anything, but I left them in in case we
    // need them in the future / this is refactored to build the custom difficulty object in steps - hhyyrylainen
    private void OnMapTypeSelected(int index)
    {
        _ = index;
    }

    private void OnLAWKToggled(bool pressed)
    {
        // Set both buttons here as we only received a signal from one of them
        lawkButton.Pressed = pressed;
        lawkAdvancedButton.Pressed = pressed;

        UpdateLifeOriginOptions(pressed);
    }

    private void OnDayNightCycleToggled(bool pressed)
    {
        dayLengthContainer.Modulate = pressed ? Colors.White : new Color(1.0f, 1.0f, 1.0f, 0.5f);
        dayLength.Editable = pressed;
        dayLength.Scrollable = pressed;
    }

    private void OnDayLengthChanged(double length)
    {
        length = Math.Round(length, 1);
        dayLengthReadout.Text = length.ToString(CultureInfo.CurrentCulture);
    }

    private void UpdateLifeOriginOptions(bool lawk)
    {
        // If we've switched to LAWK only, disable panspermia
        var panspermiaIndex = (int)WorldGenerationSettings.LifeOrigin.Panspermia;
        lifeOriginButton.SetItemDisabled(panspermiaIndex, lawk);
        lifeOriginButtonAdvanced.SetItemDisabled(panspermiaIndex, lawk);

        // If we had selected panspermia, reset to vents
        if (lawk && lifeOriginButton.Selected == panspermiaIndex)
        {
            lifeOriginButton.Selected = (int)WorldGenerationSettings.LifeOrigin.Vent;
            lifeOriginButtonAdvanced.Selected = (int)WorldGenerationSettings.LifeOrigin.Vent;
        }
    }

    private void OnGameSeedChangedFromBasic(string text)
    {
        // Need different methods to handle each view, otherwise we overwrite caret position
        gameSeedAdvanced.Text = text;
        SetSeed(text);
    }

    private void OnGameSeedChangedFromAdvanced(string text)
    {
        // Need different methods to handle each view, otherwise we overwrite caret position
        gameSeed.Text = text;
        SetSeed(text);
    }

    private void OnRandomisedGameSeedPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var seed = GenerateNewRandomSeed();
        gameSeed.Text = seed;
        gameSeedAdvanced.Text = seed;
        SetSeed(seed);
    }

    private void OnIncludeMulticellularToggled(bool pressed)
    {
        _ = pressed;
    }

    private void OnEasterEggsToggled(bool pressed)
    {
        _ = pressed;
    }

    private void PerformanceNoteLinkClicked(object meta)
    {
        _ = meta;

        EmitSignal(nameof(OnWantToSwitchToOptionsMenu));
    }
}
