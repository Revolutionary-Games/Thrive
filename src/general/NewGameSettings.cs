using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Godot;
using Xoshiro.PRNG64;
using Container = Godot.Container;

/// <summary>
///   New game settings screen
/// </summary>
public partial class NewGameSettings : ControlWithInput
{
    /// <summary>
    ///   When true, this menu works differently to facilitate beginning the microbe stage in a descended game
    /// </summary>
    [Export]
    public bool Descending;

#pragma warning disable CA2213

    // Main controls
    [Export]
    private PanelContainer basicOptions = null!;

    [Export]
    private PanelContainer advancedOptions = null!;

    [Export]
    private TabButtons tabButtons = null!;

    [Export]
    private Control difficultyTab = null!;

    [Export]
    private Control planetTab = null!;

    [Export]
    private Control miscTab = null!;

    [Export]
    private Button difficultyTabButton = null!;

    [Export]
    private Button planetTabButton = null!;

    [Export]
    private Button miscTabButton = null!;

    [Export]
    private Button basicButton = null!;

    [Export]
    private Button advancedButton = null!;

    [Export]
    private Button backButton = null!;

    [Export]
    private Button startButton = null!;

    // Difficulty controls
    [Export]
    private OptionButton difficultyPresetButton = null!;

    [Export]
    private OptionButton difficultyPresetAdvancedButton = null!;

    [Export]
    private HSlider mpMultiplier = null!;

    [Export]
    private LineEdit mpMultiplierReadout = null!;

    [Export]
    private HSlider aiMutationRate = null!;

    [Export]
    private LineEdit aiMutationRateReadout = null!;

    [Export]
    private HSlider compoundDensity = null!;

    [Export]
    private LineEdit compoundDensityReadout = null!;

    [Export]
    private HSlider playerDeathPopulationPenalty = null!;

    [Export]
    private LineEdit playerDeathPopulationPenaltyReadout = null!;

    [Export]
    private HSlider playerSpeciesAIPopulationStrength = null!;

    [Export]
    private LineEdit playerSpeciesAIPopulationStrengthReadout = null!;

    [Export]
    private HSlider glucoseDecayRate = null!;

    [Export]
    private LineEdit glucoseDecayRateReadout = null!;

    [Export]
    private HSlider osmoregulationMultiplier = null!;

    [Export]
    private LineEdit osmoregulationMultiplierReadout = null!;

    [Export]
    private HSlider autoEvoStrengthMultiplier = null!;

    [Export]
    private LineEdit autoEvoStrengthReadout = null!;

    [Export]
    private OptionButton fogOfWarModeDropdown = null!;

    [Export]
    private Label fogOfWarModeDescription = null!;

    [Export]
    private OptionButton reproductionCompoundsDropdown = null!;

    [Export]
    private Button freeGlucoseCloudButton = null!;

    [Export]
    private Button switchSpeciesOnExtinctionButton = null!;

    [Export]
    private Button limitGrowthRateButton = null!;

    [Export]
    private Button organelleUnlocksEnabled = null!;

    // Planet controls
    [Export]
    private OptionButton lifeOriginButton = null!;

    [Export]
    private OptionButton lifeOriginButtonAdvanced = null!;

    [Export]
    private Button lawkButton = null!;

    [Export]
    private Button lawkAdvancedButton = null!;

    [Export]
    private Button dayNightCycleButton = null!;

    [Export]
    private HSlider dayLength = null!;

    [Export]
    private LineEdit dayLengthReadout = null!;

    [Export]
    private VBoxContainer dayLengthContainer = null!;

    [Export]
    private LineEdit gameSeed = null!;

    [Export]
    private LineEdit gameSeedAdvanced = null!;

    [Export]
    private OptionButton worldSizeButton = null!;

    // Misc controls
    [Export]
    private Button includeMulticellularButton = null!;

    [Export]
    private Button easterEggsButton = null!;

    // Other
    [Export]
    private Container checkOptionsMenuAdviceContainer = null!;

    [Export]
    private CheckBox experimentalFeatures = null!;

    [Export]
    private Label experimentalExplanation = null!;

    [Export]
    private Label experimentalWarning = null!;
#pragma warning restore CA2213

    private SelectedOptionsTab selectedOptionsTab;

    /// <summary>
    ///   If not null, this is used as the base to start a new descended game
    /// </summary>
    private GameProperties? descendedGame;

    private long latestValidSeed;

    private IEnumerable<DifficultyPreset> difficultyPresets = null!;
    private DifficultyPreset normal = null!;
    private DifficultyPreset custom = null!;

    [Signal]
    public delegate void OnNewGameSettingsClosedEventHandler();

    [Signal]
    public delegate void OnWantToSwitchToOptionsMenuEventHandler();

    [Signal]
    public delegate void OnNewGameVideoStartedEventHandler();

    private enum SelectedOptionsTab
    {
        Difficulty,
        Planet,
        Miscellaneous,
    }

    private ReproductionCompoundHandling SelectedReproductionCompounds =>
        (ReproductionCompoundHandling)reproductionCompoundsDropdown.GetItemId(reproductionCompoundsDropdown.Selected);

    public override void _Ready()
    {
        // Difficulty presets need to be set here as the value sets below will trigger difficulty change callbacks
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
        autoEvoStrengthMultiplier.MinValue = Constants.MIN_AUTO_EVO_STRENGTH_MULTIPLIER;
        autoEvoStrengthMultiplier.MaxValue = Constants.MAX_AUTO_EVO_STRENGTH_MULTIPLIER;

        // Add items to the fog of war dropdown
        foreach (var mode in new[] { FogOfWarMode.Ignored, FogOfWarMode.Regular, FogOfWarMode.Intense })
        {
            fogOfWarModeDropdown.AddItem(Localization.Translate(mode.GetAttribute<DescriptionAttribute>().Description),
                (int)mode);
        }

        // Reproduction compounds mode is done in the Godot Editor. Not sure why the FogOfWar ended up being done here
        // in the code -hhyyrylainen

        // Do this in case default values in NewGameSettings.tscn don't match the normal preset
        InitialiseToPreset(normal);

        var seed = GenerateNewRandomSeed();
        gameSeed.Text = seed;
        gameSeedAdvanced.Text = seed;
        SetSeed(seed);

        // Make sure non-lawk options are disabled if lawk is set to true on start-up
        UpdateLifeOriginOptions(lawkButton.ButtonPressed);

        UpdatePlayerPopulationStrengthReadout((float)playerSpeciesAIPopulationStrength.Value);

        OnExperimentalFeaturesChanged(experimentalFeatures.ButtonPressed);

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

            // return true;
        }

        return true;
    }

    public void OpenFromMainMenu()
    {
        // Shouldn't do anything if options are already open.
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
        playerSpeciesAIPopulationStrength.Value = difficulty.PlayerSpeciesAIPopulationStrength;
        UpdatePlayerPopulationStrengthReadout(difficulty.PlayerSpeciesAIPopulationStrength);
        glucoseDecayRate.Value = difficulty.GlucoseDecay * 100;
        osmoregulationMultiplier.Value = difficulty.OsmoregulationMultiplier;
        autoEvoStrengthMultiplier.Value = difficulty.PlayerAutoEvoStrength;
        fogOfWarModeDropdown.Selected = (int)difficulty.FogOfWarMode;

        var reproductionIndex = reproductionCompoundsDropdown.GetItemIndex((int)difficulty.ReproductionCompounds);

        // If unknown fallback to 0 for safety
        if (reproductionIndex < 0)
            reproductionIndex = 0;

        reproductionCompoundsDropdown.Selected = reproductionIndex;
        freeGlucoseCloudButton.ButtonPressed = difficulty.FreeGlucoseCloud;
        switchSpeciesOnExtinctionButton.ButtonPressed = difficulty.SwitchSpeciesOnExtinction;
        limitGrowthRateButton.ButtonPressed = difficulty.LimitGrowthRate;
        organelleUnlocksEnabled.ButtonPressed = difficulty.OrganelleUnlocksEnabled;

        UpdateFogOfWarModeDescription(difficulty.FogOfWarMode);
        UpdateSelectedDifficultyPresetControl();

        lifeOriginButton.Selected = (int)settings.Origin;

        lawkButton.ButtonPressed = settings.LAWK;
        experimentalFeatures.ButtonPressed = settings.ExperimentalFeatures;
        OnExperimentalFeaturesChanged(settings.ExperimentalFeatures);
        dayNightCycleButton.ButtonPressed = settings.DayNightCycleEnabled;
        dayLength.Value = settings.DayLength;

        // Copy the seed from the settings, as there isn't one method to set this, this is done a bit clumsily like
        // this
        var seedText = settings.Seed.ToString();
        gameSeed.Text = seedText;
        gameSeedAdvanced.Text = seedText;
        SetSeed(seedText);

        worldSizeButton.Selected = (int)settings.WorldSize;

        // Always set prototypes to true as the player must have been there to descend
        includeMulticellularButton.ButtonPressed = true;

        // And also turn LAWK off because if the player initially played with it on, they'll probably want to experience
        // what they missed now.
        // If they still want to play with LAWK on, they can just put the checkbox back
        lawkButton.ButtonPressed = false;

        easterEggsButton.ButtonPressed = settings.EasterEggs;
    }

    public void ReportValidityOfGameSeed(bool valid)
    {
        if (valid)
        {
            GUICommon.MarkInputAsValid(gameSeed);
            GUICommon.MarkInputAsValid(gameSeedAdvanced);
            startButton.Disabled = false;
            startButton.TooltipText = Localization.Translate("CONFIRM_NEW_GAME_BUTTON_TOOLTIP");
        }
        else
        {
            GUICommon.MarkInputAsInvalid(gameSeed);
            GUICommon.MarkInputAsInvalid(gameSeedAdvanced);
            startButton.Disabled = true;
            startButton.TooltipText = Localization.Translate("CONFIRM_NEW_GAME_BUTTON_TOOLTIP_DISABLED");
        }
    }

    private void InitialiseToPreset(DifficultyPreset preset)
    {
        OnDifficultyPresetSelected(preset.Index);
    }

    private string GenerateNewRandomSeed()
    {
        var random = new XoShiRo256starstar();

        string result;

        // Generate seeds until valid (0 is not considered valid)
        do
        {
            result = random.Next64().ToString();
        }
        while (result == "0");

        return result;
    }

    private void SetSeed(string text)
    {
        bool valid = long.TryParse(text, out var seed) && seed > 0;
        ReportValidityOfGameSeed(valid);
        if (valid)
            latestValidSeed = seed;
    }

    /// <summary>
    ///   Changes the active settings tab that is displayed or returns if the tab is already active.
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
                difficultyTabButton.ButtonPressed = true;
                break;
            case SelectedOptionsTab.Planet:
                planetTab.Show();
                planetTabButton.ButtonPressed = true;
                break;
            case SelectedOptionsTab.Miscellaneous:
                miscTab.Show();
                miscTabButton.ButtonPressed = true;
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
                PlayerSpeciesAIPopulationStrength = (float)playerSpeciesAIPopulationStrength.Value,
                GlucoseDecay = (float)glucoseDecayRate.Value * 0.01f,
                OsmoregulationMultiplier = (float)osmoregulationMultiplier.Value,
                PlayerAutoEvoStrength = (float)autoEvoStrengthMultiplier.Value,
                ReproductionCompounds = SelectedReproductionCompounds,
                FogOfWarMode = (FogOfWarMode)fogOfWarModeDropdown.Selected,
                FreeGlucoseCloud = freeGlucoseCloudButton.ButtonPressed,
                SwitchSpeciesOnExtinction = switchSpeciesOnExtinctionButton.ButtonPressed,
                LimitGrowthRate = limitGrowthRateButton.ButtonPressed,
                OrganelleUnlocksEnabled = organelleUnlocksEnabled.ButtonPressed,
            };

            settings.Difficulty = customDifficulty;
        }
        else
        {
            settings.Difficulty = difficulty;
        }

        settings.Origin = (WorldGenerationSettings.LifeOrigin)lifeOriginButton.Selected;
        settings.LAWK = lawkButton.ButtonPressed;
        settings.ExperimentalFeatures = experimentalFeatures.ButtonPressed;
        OnExperimentalFeaturesChanged(settings.ExperimentalFeatures);
        settings.DayNightCycleEnabled = dayNightCycleButton.ButtonPressed;
        settings.DayLength = (int)dayLength.Value;
        settings.Seed = latestValidSeed;
        settings.WorldSize = (WorldGenerationSettings.WorldSizeEnum)worldSizeButton.Selected;

        settings.IncludeMulticellular = includeMulticellularButton.ButtonPressed;
        settings.EasterEggs = easterEggsButton.ButtonPressed;

        // Stop music for the video (stop is used instead of pause to stop the menu music playing a bit after the video
        // before the stage music starts)
        Jukebox.Instance.Stop(true);

        void OnStartGame()
        {
            MainMenu.OnEnteringGame(false);

            var microbeStage = (MicrobeStage)SceneManager.Instance.LoadScene(MainGameState.MicrobeStage).Instantiate();
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
            TransitionManager.Instance.AddSequence(
                TransitionManager.Instance.CreateScreenFade(ScreenFade.FadeType.FadeOut, 1.5f), () =>
                {
                    // Notify that the video now starts to allow the main menu to hide its expensive 3D rendering
                    EmitSignal(SignalName.OnNewGameVideoStarted);
                });

            TransitionManager.Instance.AddSequence(
                TransitionManager.Instance.CreateCutscene("res://assets/videos/microbe_intro2.ogv", 0.65f), OnStartGame,
                true, false);
        }
        else
        {
            // People who disable the cutscene are impatient anyway, so use a reduced fade time
            TransitionManager.Instance.AddSequence(
                TransitionManager.Instance.CreateScreenFade(ScreenFade.FadeType.FadeOut, 0.2f), OnStartGame);
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
        EmitSignal(SignalName.OnNewGameSettingsClosed);
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
        playerSpeciesAIPopulationStrength.Value = preset.PlayerSpeciesAIPopulationStrength;
        glucoseDecayRate.Value = preset.GlucoseDecay * 100;
        osmoregulationMultiplier.Value = preset.OsmoregulationMultiplier;
        autoEvoStrengthMultiplier.Value = preset.PlayerAutoEvoStrength;
        fogOfWarModeDropdown.Selected = (int)preset.FogOfWarMode;
        reproductionCompoundsDropdown.Selected =
            reproductionCompoundsDropdown.GetItemIndex((int)preset.ReproductionCompounds);
        freeGlucoseCloudButton.ButtonPressed = preset.FreeGlucoseCloud;
        switchSpeciesOnExtinctionButton.ButtonPressed = preset.SwitchSpeciesOnExtinction;
        limitGrowthRateButton.ButtonPressed = preset.LimitGrowthRate;
        organelleUnlocksEnabled.ButtonPressed = preset.OrganelleUnlocksEnabled;

        UpdateFogOfWarModeDescription(preset.FogOfWarMode);

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
            {
                continue;
            }

            if (Math.Abs((float)playerSpeciesAIPopulationStrength.Value - preset.PlayerSpeciesAIPopulationStrength) >
                MathUtils.EPSILON)
            {
                continue;
            }

            if ((int)glucoseDecayRate.Value != (int)(preset.GlucoseDecay * 100))
                continue;

            if (Math.Abs((float)osmoregulationMultiplier.Value - preset.OsmoregulationMultiplier) > MathUtils.EPSILON)
                continue;

            if (Math.Abs((float)autoEvoStrengthMultiplier.Value - preset.PlayerAutoEvoStrength) > MathUtils.EPSILON)
                continue;

            if (fogOfWarModeDropdown.Selected != (int)preset.FogOfWarMode)
                continue;

            if (SelectedReproductionCompounds != preset.ReproductionCompounds)
                continue;

            if (freeGlucoseCloudButton.ButtonPressed != preset.FreeGlucoseCloud)
                continue;

            if (switchSpeciesOnExtinctionButton.ButtonPressed != preset.SwitchSpeciesOnExtinction)
                continue;

            if (limitGrowthRateButton.ButtonPressed != preset.LimitGrowthRate)
                continue;

            if (organelleUnlocksEnabled.ButtonPressed != preset.OrganelleUnlocksEnabled)
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

    private void OnPlayerSpeciesAIPopulationStrengthValueChanged(double amount)
    {
        UpdatePlayerPopulationStrengthReadout((float)amount);

        UpdateSelectedDifficultyPresetControl();
    }

    private void UpdatePlayerPopulationStrengthReadout(float amount)
    {
        amount = MathF.Round(amount * 100);
        playerSpeciesAIPopulationStrengthReadout.Text = Localization.Translate("PERCENTAGE_VALUE")
            .FormatSafe(amount.ToString(CultureInfo.CurrentCulture));
    }

    private void OnGlucoseDecayRateValueChanged(double percentage)
    {
        percentage = Math.Round(percentage, 2);
        glucoseDecayRateReadout.Text = Localization.Translate("PERCENTAGE_VALUE").FormatSafe(percentage);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnOsmoregulationMultiplierValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        osmoregulationMultiplierReadout.Text = amount.ToString(CultureInfo.CurrentCulture);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnAutoEvoStrengthValueChanged(double amount)
    {
        amount = Math.Round(amount * 100);
        autoEvoStrengthReadout.Text = Localization.Translate("PERCENTAGE_VALUE").FormatSafe(amount);

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnFogOfWarModeChanged(int index)
    {
        var mode = (FogOfWarMode)index;
        UpdateFogOfWarModeDescription(mode);
        UpdateSelectedDifficultyPresetControl();
    }

    private void OnReproductionCompoundModeChanged(int index)
    {
        _ = index;
        UpdateSelectedDifficultyPresetControl();
    }

    private void UpdateFogOfWarModeDescription(FogOfWarMode mode)
    {
        var description = string.Empty;

        switch (mode)
        {
            case FogOfWarMode.Ignored:
                description = Localization.Translate("FOG_OF_WAR_DISABLED_DESCRIPTION");
                break;
            case FogOfWarMode.Regular:
                description = Localization.Translate("FOG_OF_WAR_REGULAR_DESCRIPTION");
                break;
            case FogOfWarMode.Intense:
                description = Localization.Translate("FOG_OF_WAR_INTENSE_DESCRIPTION");
                break;
        }

        fogOfWarModeDescription.Text = description;
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

    private void OnSwapOnExtinctionToggled(bool pressed)
    {
        _ = pressed;
        UpdateSelectedDifficultyPresetControl();
    }

    private void OnGrowthRateToggled(bool pressed)
    {
        _ = pressed;
        UpdateSelectedDifficultyPresetControl();
    }

    private void OnOrganelleUnlocksToggled(bool pressed)
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

    // This and a few other callbacks are not currently needed to detect anything, but I left them in, in case we
    // need them in the future / this is refactored to build the custom difficulty object in steps - hhyyrylainen
    private void OnMapTypeSelected(int index)
    {
        _ = index;
    }

    private void OnLAWKToggled(bool pressed)
    {
        // Set both buttons here as we only received a signal from one of them
        lawkButton.ButtonPressed = pressed;
        lawkAdvancedButton.ButtonPressed = pressed;

        UpdateLifeOriginOptions(pressed);
    }

    private void OnDayNightCycleToggled(bool pressed)
    {
        dayLengthContainer.Modulate = pressed ? Colors.White : new Color(1.0f, 1.0f, 1.0f, 0.5f);
        dayLength.Editable = pressed;
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
        // Need different methods to handle each view; otherwise we overwrite caret position
        gameSeedAdvanced.Text = text;
        SetSeed(text);
    }

    private void OnGameSeedChangedFromAdvanced(string text)
    {
        // Need different methods to handle each view; otherwise we overwrite caret position
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

    private void OnWorldSizeSelected(int index)
    {
        _ = index;
    }

    private void OnIncludeMulticellularToggled(bool pressed)
    {
        _ = pressed;
    }

    private void OnEasterEggsToggled(bool pressed)
    {
        _ = pressed;
    }

    private void PerformanceNoteLinkClicked(Variant meta)
    {
        if (meta.VariantType != Variant.Type.String)
        {
            GD.PrintErr("Unexpected new game info text meta clicked");
            return;
        }

        // TODO: check that the meta has the correct content?

        EmitSignal(SignalName.OnWantToSwitchToOptionsMenu);
    }

    private void OnExperimentalFeaturesChanged(bool enabled)
    {
        experimentalWarning.Visible = enabled;
        experimentalExplanation.Visible = !enabled;
    }
}
