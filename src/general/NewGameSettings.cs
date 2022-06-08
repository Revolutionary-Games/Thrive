using System;
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
    public NodePath CompoundDensityPath = null!;

    [Export]
    public NodePath CompoundDensityReadoutPath = null!;

    [Export]
    public NodePath LifeOriginButtonPath = null!;

    [Export]
    public NodePath LAWKButtonPath = null!;

    [Export]
    public NodePath GameSeedPath = null!;

    [Export]
    public NodePath ConfirmButtonPath = null!;

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
    private HSlider compoundDensity = null!;
    private LineEdit compoundDensityReadout = null!;
    private OptionButton lifeOriginButton = null!;
    private Button lawkButton = null!;
    private LineEdit gameSeed = null!;
    private Button confirmButton = null!;

    private SelectedOptionsTab selectedOptionsTab;

    [Signal]
    public delegate void OnNewGameSettingsClosed();

    private WorldGenerationSettings settings = new WorldGenerationSettings();

    private bool isGameSeedValid;

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
        compoundDensity = GetNode<HSlider>(CompoundDensityPath);
        compoundDensityReadout = GetNode<LineEdit>(CompoundDensityReadoutPath);
        lifeOriginButton = GetNode<OptionButton>(LifeOriginButtonPath);
        lawkButton = GetNode<Button>(LAWKButtonPath);
        gameSeed = GetNode<LineEdit>(GameSeedPath);
        confirmButton = GetNode<Button>(ConfirmButtonPath);

        mpMultiplier.MinValue = WorldGenerationSettings.MIN_MP_MULTIPLIER;
        mpMultiplier.MaxValue = WorldGenerationSettings.MAX_MP_MULTIPLIER;
        compoundDensity.MinValue = WorldGenerationSettings.MIN_COMPOUND_DENSITY;
        compoundDensity.MaxValue = WorldGenerationSettings.MAX_COMPOUND_DENSITY;

        gameSeed.Text = GenerateNewRandomSeed();
        SetSeed(gameSeed.Text);
    }

    private string GenerateNewRandomSeed()
    {
        var random = new Random();
        return random.Next().ToString();
    }

    private void SetSeed(string text)
    {
        int seed;
        isGameSeedValid = int.TryParse(text, out seed) && seed > 0;
        ReportValidityOfGameSeed(isGameSeedValid);
        if (isGameSeedValid)
            settings.Seed = seed;
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.SUBMENU_CANCEL_PRIORITY)]
    public bool OnEscapePressed()
    {
        // Only handle keypress when visible
        if (!Visible)
            return false;

        // TODO: Don't use bool as we don't work like settings?
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
            confirmButton.Disabled = false;
        }
        else
        {
            GUICommon.MarkInputAsInvalid(gameSeed);
            confirmButton.Disabled = true;
        }
    }

    private enum SelectedOptionsTab
    {
        Difficulty,
        Planet,
        Miscellaneous,
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
        ProcessAdvancedSelection(true);
    }

    private void ProcessAdvancedSelection(bool playSound)
    {
        if (playSound)
            GUICommon.Instance.PlayButtonPressSound();

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
        if (!isGameSeedValid)
            return;

        // Disable the button to prevent it being executed again.
        confirmButton.Disabled = true;

        settings.Difficulty = DifficultyPresetIndexToValue(difficultyPresetButton.Selected);
        settings.Origin = LifeOriginIndexToValue(lifeOriginButton.Selected);
        settings.LAWK = lawkButton.Pressed;
        SetSeed(gameSeed.Text);

        settings.MPMultiplier = mpMultiplier.Value;
        settings.CompoundDensity = compoundDensity.Value;

        var scene = GD.Load<PackedScene>("res://src/general/MainMenu.tscn");
        var mainMenu = (MainMenu)scene.Instance();
        mainMenu.NewGameSetupDone(settings);
    }

    private void OnDifficultyPresetSelected(int index)
    {
        // Set both buttons here as we only received a signal from one of them
        difficultyPresetButton.Selected = index;
        difficultyPresetAdvancedButton.Selected = index;

        DifficultyPreset preset = DifficultyPresetIndexToValue(index);
        settings.Difficulty = preset;

        // If custom was selected, open the advanced view to the difficulty tab
        if (preset == DifficultyPreset.Custom)
        {
            ChangeSettingsTab("Difficulty");
            ProcessAdvancedSelection(false);
            return;
        }

        mpMultiplier.Value = WorldGenerationSettings.GetMPMultiplier(preset);
        compoundDensity.Value = WorldGenerationSettings.GetCompoundDensity(preset);
    }

    private DifficultyPreset DifficultyPresetIndexToValue(int index)
    {
        switch (index)
        {
            case 0:
                return DifficultyPreset.Easy;
            case 2:
                return DifficultyPreset.Hard;
            case 3:
                return DifficultyPreset.Custom;
            default:
                return DifficultyPreset.Normal;
        }
    }

    private int DifficultyPresetValueToIndex(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 0;
            case DifficultyPreset.Hard:
                return 2;
            case DifficultyPreset.Custom:
                return 3;
            default:
                return 1;
        }
    }

    private void UpdateDifficultyPreset()
    {
        var custom = DifficultyPreset.Custom;

        foreach (DifficultyPreset preset in Enum.GetValues(typeof(DifficultyPreset)))
        {
            // Ignore custom until the end
            if (preset == custom)
                continue;

            if (Math.Abs(mpMultiplier.Value - WorldGenerationSettings.GetMPMultiplier(preset)) > MathUtils.EPSILON)
                continue;
            
            if (Math.Abs(compoundDensity.Value - WorldGenerationSettings.GetCompoundDensity(preset)) > MathUtils.EPSILON)
                continue;

            // If all values are equal to the values for a preset, use that preset
            difficultyPresetButton.Selected = DifficultyPresetValueToIndex(preset);
            difficultyPresetAdvancedButton.Selected = DifficultyPresetValueToIndex(preset);
            return;
        }

        // If there is no preset with all values equal to the values set, use custom
        difficultyPresetButton.Selected = DifficultyPresetValueToIndex(custom);
        difficultyPresetAdvancedButton.Selected = DifficultyPresetValueToIndex(custom);
    }

    private void OnMPMultiplierValueChanged(double amount)
    {
        mpMultiplierReadout.Text = amount.ToString();
        settings.MPMultiplier = amount;

        UpdateDifficultyPreset();
    }

    private void OnCompoundDensityValueChanged(double amount)
    {
        compoundDensityReadout.Text = amount.ToString();
        settings.CompoundDensity = amount;

        UpdateDifficultyPreset();
    }

    private void OnLifeOriginSelected(int index)
    {
        settings.Origin = LifeOriginIndexToValue(index);
    }

    private WorldGenerationSettings.LifeOrigin LifeOriginIndexToValue(int index)
    {
        switch (index)
        {
            case 1:
                return WorldGenerationSettings.LifeOrigin.Pond;
            case 2:
                return WorldGenerationSettings.LifeOrigin.Panspermia;
            default:
                return WorldGenerationSettings.LifeOrigin.Vent;
        }
    }

    private void OnLAWKToggled(int index)
    {
        settings.LAWK = lawkButton.Pressed;
    }

    private void OnGameSeedChanged(string text)
    {
        SetSeed(text);
    }

    private void OnRandomisedGameSeedPressed()
    {
        gameSeed.Text = GenerateNewRandomSeed();
    }
}