using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

public class NewGameSettings : ControlWithInput
{
    [Export]
    public NodePath BasicOptionsPath = null!;

    [Export]
    public NodePath AdvancedOptionsPath = null!;

    [Export]
    public NodePath BasicButtonPath = null!;

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
    public NodePath MapTypeButtonPath = null!;

    [Export]
    public NodePath LifeOriginButtonPath = null!;

    [Export]
    public NodePath LifeOriginButtonAdvancedPath = null!;

    [Export]
    public NodePath LawkButtonPath = null!;

    [Export]
    public NodePath LawkAdvancedButtonPath = null!;

    [Export]
    public NodePath GameSeedPath = null!;

    [Export]
    public NodePath GameSeedAdvancedPath = null!;

    [Export]
    public NodePath IncludeMulticellularButtonPath = null!;

    [Export]
    public NodePath EasterEggsButtonPath = null!;

    [Export]
    public NodePath ConfirmButtonPath = null!;

    /*
    Static values for min/max in difficulty options
    */

    private const float MIN_MP_MULTIPLIER = 0.2f;
    private const float MAX_MP_MULTIPLIER = 2;
    private const float MIN_AI_MUTATION_RATE = 0.5f;
    private const float MAX_AI_MUTATION_RATE = 5;
    private const float MIN_COMPOUND_DENSITY = 0.2f;
    private const float MAX_COMPOUND_DENSITY = 2;
    private const float MIN_PLAYER_DEATH_POPULATION_PENALTY = 1;
    private const float MAX_PLAYER_DEATH_POPULATION_PENALTY = 5;
    private const float MIN_GLUCOSE_DECAY = 0.3f;
    private const float MAX_GLUCOSE_DECAY = 0.95f;
    private const float MIN_OSMOREGULATION_MULTIPLIER = 0.2f;
    private const float MAX_OSMOREGULATION_MULTIPLIER = 2;

    private PanelContainer basicOptions = null!;
    private PanelContainer advancedOptions = null!;
    private HBoxContainer tabButtons = null!;
    private Control difficultyTab = null!;
    private Control planetTab = null!;
    private Control miscTab = null!;
    private Button difficultyTabButton = null!;
    private Button planetTabButton = null!;
    private Button miscTabButton = null!;
    private Button basicButton = null!;
    private Button advancedButton = null!;
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
    private OptionButton mapTypeButton = null!;
    private OptionButton lifeOriginButton = null!;
    private OptionButton lifeOriginButtonAdvanced = null!;
    private Button lawkButton = null!;
    private Button lawkAdvancedButton = null!;
    private LineEdit gameSeed = null!;
    private LineEdit gameSeedAdvanced = null!;
    private Button includeMulticellularButton = null!;
    private Button easterEggsButton = null!;
    private Button confirmButton = null!;

    private SelectedOptionsTab selectedOptionsTab;

    private WorldGenerationSettings settings = new();

    private bool isGameSeedValid;

    [Signal]
    public delegate void OnNewGameSettingsClosed();

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
        tabButtons = GetNode<HBoxContainer>(TabButtonsPath);
        difficultyTab = GetNode<Control>(DifficultyTabPath);
        planetTab = GetNode<Control>(PlanetTabPath);
        miscTab = GetNode<Control>(MiscTabPath);
        difficultyTabButton = GetNode<Button>(DifficultyTabButtonPath);
        planetTabButton = GetNode<Button>(PlanetTabButtonPath);
        miscTabButton = GetNode<Button>(MiscTabButtonPath);

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
        mapTypeButton = GetNode<OptionButton>(MapTypeButtonPath);
        lifeOriginButton = GetNode<OptionButton>(LifeOriginButtonPath);
        lifeOriginButtonAdvanced = GetNode<OptionButton>(LifeOriginButtonAdvancedPath);
        lawkButton = GetNode<Button>(LawkButtonPath);
        lawkAdvancedButton = GetNode<Button>(LawkAdvancedButtonPath);
        gameSeed = GetNode<LineEdit>(GameSeedPath);
        gameSeedAdvanced = GetNode<LineEdit>(GameSeedAdvancedPath);
        includeMulticellularButton = GetNode<Button>(IncludeMulticellularButtonPath);
        easterEggsButton = GetNode<Button>(EasterEggsButtonPath);
        confirmButton = GetNode<Button>(ConfirmButtonPath);

        mpMultiplier.MinValue = MIN_MP_MULTIPLIER;
        mpMultiplier.MaxValue = MAX_MP_MULTIPLIER;
        aiMutationRate.MinValue = MIN_AI_MUTATION_RATE;
        aiMutationRate.MaxValue = MAX_AI_MUTATION_RATE;
        compoundDensity.MinValue = MIN_COMPOUND_DENSITY;
        compoundDensity.MaxValue = MAX_COMPOUND_DENSITY;
        playerDeathPopulationPenalty.MinValue = MIN_PLAYER_DEATH_POPULATION_PENALTY;
        playerDeathPopulationPenalty.MaxValue = MAX_PLAYER_DEATH_POPULATION_PENALTY;
        glucoseDecayRate.MinValue = MIN_GLUCOSE_DECAY * 100;
        glucoseDecayRate.MaxValue = MAX_GLUCOSE_DECAY * 100;
        osmoregulationMultiplier.MinValue = MIN_OSMOREGULATION_MULTIPLIER;
        osmoregulationMultiplier.MaxValue = MAX_OSMOREGULATION_MULTIPLIER;

        var seed = GenerateNewRandomSeed();
        gameSeed.Text = seed;
        gameSeedAdvanced.Text = seed;
        SetSeed(seed);
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

    public void ReportValidityOfGameSeed(bool valid)
    {
        if (valid)
        {
            GUICommon.MarkInputAsValid(gameSeed);
            GUICommon.MarkInputAsValid(gameSeedAdvanced);
            confirmButton.Disabled = false;
            confirmButton.HintTooltip = TranslationServer.Translate("CONFIRM_NEW_GAME_BUTTON_TOOLTIP");
        }
        else
        {
            GUICommon.MarkInputAsInvalid(gameSeed);
            GUICommon.MarkInputAsInvalid(gameSeedAdvanced);
            confirmButton.Disabled = true;
            confirmButton.HintTooltip = TranslationServer.Translate("CONFIRM_NEW_GAME_BUTTON_TOOLTIP_DISABLED");
        }
    }

    private string GenerateNewRandomSeed()
    {
        var random = new Random();
        return random.Next().ToString();
    }

    private void SetSeed(string text)
    {
        isGameSeedValid = int.TryParse(text, out int seed) && seed > 0;
        ReportValidityOfGameSeed(isGameSeedValid);
        if (isGameSeedValid)
            settings.Seed = seed;
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

    /*
      GUI Control Callbacks
    */

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
        ProcessAdvancedSelection();
    }

    private void ProcessAdvancedSelection()
    {
        basicOptions.Visible = false;
        advancedButton.Visible = false;

        advancedOptions.Visible = true;
        basicButton.Visible = true;
        tabButtons.Visible = true;
    }

    private void OnBasicPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        advancedOptions.Visible = false;
        basicButton.Visible = false;
        tabButtons.Visible = false;

        advancedButton.Visible = true;
        basicOptions.Visible = true;
    }

    private void OnConfirmPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (!isGameSeedValid)
            return;

        // Disable the button to prevent it being executed again.
        confirmButton.Disabled = true;

        settings.Difficulty = (DifficultyPreset)difficultyPresetButton.Selected;
        settings.Origin = (WorldGenerationSettings.LifeOrigin)lifeOriginButton.Selected;
        settings.Lawk = lawkButton.Pressed;
        SetSeed(gameSeed.Text);

        settings.MPMultiplier = (float)mpMultiplier.Value;
        settings.AIMutationMultiplier = (float)aiMutationRate.Value;
        settings.CompoundDensity = (float)compoundDensity.Value;
        settings.PlayerDeathPopulationPenalty = (float)playerDeathPopulationPenalty.Value;
        settings.GlucoseDecay = (float)glucoseDecayRate.Value * 0.01f;
        settings.OsmoregulationMultiplier = (float)osmoregulationMultiplier.Value;
        settings.FreeGlucoseCloud = freeGlucoseCloudButton.Pressed;

        settings.MapType = MapTypeIndexToValue(mapTypeButton.Selected);

        settings.IncludeMulticellular = includeMulticellularButton.Pressed;
        settings.EasterEggs = easterEggsButton.Pressed;

        // Stop music for the video (stop is used instead of pause to stop the menu music playing a bit after the video
        // before the stage music starts)
        Jukebox.Instance.Stop(true);

        var transitions = new List<ITransition>();

        if (Settings.Instance.PlayMicrobeIntroVideo && LaunchOptions.VideosEnabled)
        {
            transitions.Add(TransitionManager.Instance.CreateScreenFade(ScreenFade.FadeType.FadeOut, 1.5f));
            transitions.Add(TransitionManager.Instance.CreateCutscene(
                "res://assets/videos/microbe_intro2.ogv", 0.65f));
        }
        else
        {
            // People who disable the cutscene are impatient anyway so use a reduced fade time
            transitions.Add(TransitionManager.Instance.CreateScreenFade(ScreenFade.FadeType.FadeOut, 0.2f));
        }

        TransitionManager.Instance.AddSequence(transitions, () =>
        {
            MainMenu.OnEnteringGame();

            // TODO: Add loading screen while changing between scenes
            var microbeStage = (MicrobeStage)SceneManager.Instance.LoadScene(MainGameState.MicrobeStage).Instance();
            microbeStage.WorldSettings = settings;
            SceneManager.Instance.SwitchToScene(microbeStage);
        });
    }

    private void OnEnteringGame()
    {
        CheatManager.OnCheatsDisabled();
        SaveHelper.ClearLastSaveTime();
    }

    private void OnDifficultyPresetSelected(int index)
    {
        // Set both buttons here as we only received a signal from one of them
        difficultyPresetButton.Selected = index;
        difficultyPresetAdvancedButton.Selected = index;

        DifficultyPreset preset = (DifficultyPreset)index;
        settings.Difficulty = preset;

        // If custom was selected, open the advanced view to the difficulty tab
        if (preset == DifficultyPreset.Custom)
        {
            ChangeSettingsTab("Difficulty");
            ProcessAdvancedSelection();
            return;
        }

        mpMultiplier.Value = WorldGenerationSettings.GetMPMultiplier(preset);
        aiMutationRate.Value = WorldGenerationSettings.GetAIMutationMultiplier(preset);
        compoundDensity.Value = WorldGenerationSettings.GetCompoundDensity(preset);
        playerDeathPopulationPenalty.Value = WorldGenerationSettings.GetPlayerDeathPopulationPenalty(preset);
        glucoseDecayRate.Value = WorldGenerationSettings.GetGlucoseDecay(preset) * 100;
        osmoregulationMultiplier.Value = WorldGenerationSettings.GetOsmoregulationMultiplier(preset);
        freeGlucoseCloudButton.Pressed = WorldGenerationSettings.GetFreeGlucoseCloud(preset);

        UpdateDifficultyPreset();
    }

    private void UpdateDifficultyPreset()
    {
        var custom = DifficultyPreset.Custom;

        foreach (DifficultyPreset preset in Enum.GetValues(typeof(DifficultyPreset)))
        {
            // Ignore custom until the end
            if (preset == custom)
                continue;

            if (Math.Abs((float)mpMultiplier.Value - WorldGenerationSettings.GetMPMultiplier(preset))
                > MathUtils.EPSILON)
                continue;

            if (Math.Abs((float)aiMutationRate.Value - WorldGenerationSettings.GetAIMutationMultiplier(preset))
                > MathUtils.EPSILON)
                continue;

            if (Math.Abs((float)compoundDensity.Value - WorldGenerationSettings.GetCompoundDensity(preset)) >
                MathUtils.EPSILON)
                continue;

            if (Math.Abs((float)playerDeathPopulationPenalty.Value -
                    WorldGenerationSettings.GetPlayerDeathPopulationPenalty(preset)) > MathUtils.EPSILON)
                continue;

            if ((int)glucoseDecayRate.Value != WorldGenerationSettings.GetGlucoseDecay(preset) * 100)
                continue;

            if (Math.Abs((float)osmoregulationMultiplier.Value -
                    WorldGenerationSettings.GetOsmoregulationMultiplier(preset)) > MathUtils.EPSILON)
                continue;

            if (freeGlucoseCloudButton.Pressed != WorldGenerationSettings.GetFreeGlucoseCloud(preset))
                continue;

            // If all values are equal to the values for a preset, use that preset
            difficultyPresetButton.Selected = (int)preset;
            difficultyPresetAdvancedButton.Selected = (int)preset;
            return;
        }

        // If there is no preset with all values equal to the values set, use custom
        difficultyPresetButton.Selected = (int)custom;
        difficultyPresetAdvancedButton.Selected = (int)custom;
    }

    private void OnMPMultiplierValueChanged(double amount)
    {
        mpMultiplierReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.MPMultiplier = (float)amount;

        UpdateDifficultyPreset();
    }

    private void OnAIMutationRateValueChanged(double amount)
    {
        aiMutationRateReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.AIMutationMultiplier = (float)amount;

        UpdateDifficultyPreset();
    }

    private void OnCompoundDensityValueChanged(double amount)
    {
        compoundDensityReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.CompoundDensity = (float)amount;

        UpdateDifficultyPreset();
    }

    private void OnPlayerDeathPopulationPenaltyValueChanged(double amount)
    {
        playerDeathPopulationPenaltyReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.PlayerDeathPopulationPenalty = (float)amount;

        UpdateDifficultyPreset();
    }

    private void OnGlucoseDecayRateValueChanged(double percentage)
    {
        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");
        glucoseDecayRateReadout.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            percentage);
        settings.GlucoseDecay = (float)percentage * 0.01f;

        UpdateDifficultyPreset();
    }

    private void OnOsmoregulationMultiplierValueChanged(double amount)
    {
        osmoregulationMultiplierReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.OsmoregulationMultiplier = (float)amount;

        UpdateDifficultyPreset();
    }

    private void OnFreeGlucoseCloudToggled(bool pressed)
    {
        settings.FreeGlucoseCloud = pressed;

        UpdateDifficultyPreset();
    }

    private void OnLifeOriginSelected(int index)
    {
        // Set both buttons here as we only received a signal from one of them
        lifeOriginButton.Selected = index;
        lifeOriginButtonAdvanced.Selected = index;

        settings.Origin = (WorldGenerationSettings.LifeOrigin)index;
    }

    private void OnMapTypeSelected(int index)
    {
        settings.MapType = MapTypeIndexToValue(index);
    }

    private WorldGenerationSettings.PatchMapType MapTypeIndexToValue(int index)
    {
        switch (index)
        {
            case 1:
                return WorldGenerationSettings.PatchMapType.Classic;
            default:
                return WorldGenerationSettings.PatchMapType.Procedural;
        }
    }

    private void OnLAWKToggled(bool pressed)
    {
        // Set both buttons here as we only received a signal from one of them
        lawkButton.Pressed = pressed;
        lawkAdvancedButton.Pressed = pressed;

        settings.Lawk = lawkButton.Pressed;
    }

    private void OnGameSeedChangedFromBasic(string text)
    {
        gameSeedAdvanced.Text = text;
        SetSeed(text);
    }

    private void OnGameSeedChangedFromAdvanced(string text)
    {
        gameSeed.Text = text;
        SetSeed(text);
    }

    private void OnRandomisedGameSeedPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var seed = GenerateNewRandomSeed();
        SetSeed(seed);
        gameSeed.Text = seed;
        gameSeedAdvanced.Text = seed;
    }

    private void OnIncludeMulticellularToggled(bool pressed)
    {
        settings.IncludeMulticellular = pressed;
    }

    private void OnEasterEggsToggled(bool pressed)
    {
        settings.EasterEggs = pressed;
    }
}
