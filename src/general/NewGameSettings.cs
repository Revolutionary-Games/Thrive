using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public NodePath LAWKButtonPath = null!;

    [Export]
    public NodePath LAWKAdvancedButtonPath = null!;

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

    // Main controls
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
    private Button confirmButton = null!;

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

    // Planet controls
    private OptionButton mapTypeButton = null!;
    private OptionButton lifeOriginButton = null!;
    private OptionButton lifeOriginButtonAdvanced = null!;
    private Button lawkButton = null!;
    private Button lawkAdvancedButton = null!;
    private LineEdit gameSeed = null!;
    private LineEdit gameSeedAdvanced = null!;

    // Misc controls
    private Button includeMulticellularButton = null!;
    private Button easterEggsButton = null!;

    private SelectedOptionsTab selectedOptionsTab;

    private WorldGenerationSettings settings = null!;

    private IEnumerable<DifficultyPreset> difficultyPresets = null!;
    private DifficultyPreset normal = null!;
    private DifficultyPreset custom = null!;

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
        lawkButton = GetNode<Button>(LAWKButtonPath);
        lawkAdvancedButton = GetNode<Button>(LAWKAdvancedButtonPath);
        gameSeed = GetNode<LineEdit>(GameSeedPath);
        gameSeedAdvanced = GetNode<LineEdit>(GameSeedAdvancedPath);
        includeMulticellularButton = GetNode<Button>(IncludeMulticellularButtonPath);
        easterEggsButton = GetNode<Button>(EasterEggsButtonPath);
        confirmButton = GetNode<Button>(ConfirmButtonPath);

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

        settings = new WorldGenerationSettings();
        difficultyPresets = SimulationParameters.Instance.GetAllDifficultyPresets();
        normal = SimulationParameters.Instance.GetDifficultyPreset("normal");
        custom = SimulationParameters.Instance.GetDifficultyPreset("custom");

        UpdateDifficultyPresetControl();

        // Do this in case default values in NewGameSettings.tscn don't match the normal preset
        InitialiseToPreset(normal);

        var seed = GenerateNewRandomSeed();
        gameSeed.Text = seed;
        gameSeedAdvanced.Text = seed;
        SetSeed(seed);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            UpdateDifficultyPresetControl();
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

    private void InitialiseToPreset(DifficultyPreset preset)
    {
        difficultyPresetButton.Selected = preset.Index;
        difficultyPresetAdvancedButton.Selected = preset.Index;

        OnMPMultiplierValueChanged(preset.MPMultiplier);
        OnAIMutationRateValueChanged(preset.AIMutationMultiplier);
        OnCompoundDensityValueChanged(preset.CompoundDensity);
        OnPlayerDeathPopulationPenaltyValueChanged(preset.PlayerDeathPopulationPenalty);
        OnGlucoseDecayRateValueChanged(preset.GlucoseDecay * 100);
        OnOsmoregulationMultiplierValueChanged(preset.OsmoregulationMultiplier);
        OnFreeGlucoseCloudToggled(preset.FreeGlucoseCloud);
    }

    private void UpdateDifficultyPresetControl()
    {
        difficultyPresetButton.Clear();
        difficultyPresetAdvancedButton.Clear();

        foreach (DifficultyPreset preset in difficultyPresets.OrderBy(p => p.Index))
        {
            difficultyPresetButton.AddItem(preset.Name);
            difficultyPresetAdvancedButton.AddItem(preset.Name);
        }
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

        settings.Difficulty = SimulationParameters.Instance.GetDifficultyPresetByIndex(difficultyPresetButton.Selected);
        settings.Origin = (WorldGenerationSettings.LifeOrigin)lifeOriginButton.Selected;
        settings.LAWK = lawkButton.Pressed;
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
            microbeStage.CurrentGame = GameProperties.StartNewMicrobeGame(settings);
            SceneManager.Instance.SwitchToScene(microbeStage);
        });

        // Disable the button to prevent it being executed again.
        confirmButton.Disabled = true;
    }

    private void OnDifficultyPresetSelected(int index)
    {
        // Set both buttons here as we only received a signal from one of them
        difficultyPresetButton.Selected = index;
        difficultyPresetAdvancedButton.Selected = index;

        DifficultyPreset preset = SimulationParameters.Instance.GetDifficultyPresetByIndex(index);
        settings.Difficulty = preset;

        // If custom was selected, open the advanced view to the difficulty tab
        if (preset.InternalName == custom.InternalName)
        {
            ChangeSettingsTab("Difficulty");
            ProcessAdvancedSelection();
            return;
        }

        mpMultiplier.Value = preset.MPMultiplier;
        aiMutationRate.Value = preset.AIMutationMultiplier;
        compoundDensity.Value = preset.CompoundDensity;
        playerDeathPopulationPenalty.Value = preset.PlayerDeathPopulationPenalty;
        glucoseDecayRate.Value = preset.GlucoseDecay * 100;
        osmoregulationMultiplier.Value = preset.OsmoregulationMultiplier;
        freeGlucoseCloudButton.Pressed = preset.FreeGlucoseCloud;

        UpdateSelectedDifficultyPresetControl();
    }

    private void UpdateSelectedDifficultyPresetControl()
    {
        foreach (DifficultyPreset preset in difficultyPresets)
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
        settings.MPMultiplier = (float)amount;

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnAIMutationRateValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        aiMutationRateReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.AIMutationMultiplier = (float)amount;

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnCompoundDensityValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        compoundDensityReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.CompoundDensity = (float)amount;

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnPlayerDeathPopulationPenaltyValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        playerDeathPopulationPenaltyReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.PlayerDeathPopulationPenalty = (float)amount;

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnGlucoseDecayRateValueChanged(double percentage)
    {
        percentage = Math.Round(percentage, 2);
        glucoseDecayRateReadout.Text = TranslationServer.Translate("PERCENTAGE_VALUE").FormatSafe(percentage);
        settings.GlucoseDecay = (float)percentage * 0.01f;

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnOsmoregulationMultiplierValueChanged(double amount)
    {
        amount = Math.Round(amount, 1);
        osmoregulationMultiplierReadout.Text = amount.ToString(CultureInfo.CurrentCulture);
        settings.OsmoregulationMultiplier = (float)amount;

        UpdateSelectedDifficultyPresetControl();
    }

    private void OnFreeGlucoseCloudToggled(bool pressed)
    {
        settings.FreeGlucoseCloud = pressed;

        UpdateSelectedDifficultyPresetControl();
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
            case 0:
                return WorldGenerationSettings.PatchMapType.Procedural;
            case 1:
                return WorldGenerationSettings.PatchMapType.Classic;
            default:
                GD.PrintErr($"Index {index} does not correspond to known map type");
                return WorldGenerationSettings.PatchMapType.Procedural;
        }
    }

    private void OnLAWKToggled(bool pressed)
    {
        // Set both buttons here as we only received a signal from one of them
        lawkButton.Pressed = pressed;
        lawkAdvancedButton.Pressed = pressed;

        settings.LAWK = lawkButton.Pressed;

        UpdateLifeOriginOptions(pressed);
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
        settings.IncludeMulticellular = pressed;
    }

    private void OnEasterEggsToggled(bool pressed)
    {
        settings.EasterEggs = pressed;
    }
}
