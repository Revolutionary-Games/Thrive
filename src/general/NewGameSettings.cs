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
        lifeOriginButton = GetNode<OptionButton>(LifeOriginButtonPath);
        lawkButton = GetNode<Button>(LAWKButtonPath);
        gameSeed = GetNode<LineEdit>(GameSeedPath);
        confirmButton = GetNode<Button>(ConfirmButtonPath);

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

        var scene = GD.Load<PackedScene>("res://src/general/MainMenu.tscn");
        var mainMenu = (MainMenu)scene.Instance();
        mainMenu.NewGameSetupDone(settings);
    }

    private void OnDifficultyPresetSelected(int index)
    {
        // Set both buttons here as we only received a signal from one of them
        difficultyPresetButton.Selected = index;
        difficultyPresetAdvancedButton.Selected = index;

        WorldGenerationSettings.DifficultyPreset preset = DifficultyPresetIndexToValue(index);
        settings.Difficulty = preset;
        GD.Print(settings.Difficulty);

        // If custom was selected, open the advanced view to the difficulty tab
        if (preset == WorldGenerationSettings.DifficultyPreset.Custom)
        {
            ChangeSettingsTab("Difficulty");
            ProcessAdvancedSelection(false);
            return;
        }

        mpMultiplier.Value = WorldGenerationSettings.GetMPMultiplier(preset);
    }

    private WorldGenerationSettings.DifficultyPreset DifficultyPresetIndexToValue(int index)
    {
        switch (index)
        {
            case 0:
                return WorldGenerationSettings.DifficultyPreset.Easy;
            case 2:
                return WorldGenerationSettings.DifficultyPreset.Hard;
            case 3:
                return WorldGenerationSettings.DifficultyPreset.Custom;
            default:
                return WorldGenerationSettings.DifficultyPreset.Normal;
        }
    }

    private int DifficultyPresetValueToIndex(WorldGenerationSettings.DifficultyPreset preset)
    {
        switch (preset)
        {
            case WorldGenerationSettings.DifficultyPreset.Easy:
                return 0;
            case WorldGenerationSettings.DifficultyPreset.Hard:
                return 2;
            case WorldGenerationSettings.DifficultyPreset.Custom:
                return 3;
            default:
                return 1;
        }
    }

    private void UpdateDifficultyPreset()
    {
        var custom = WorldGenerationSettings.DifficultyPreset.Custom;

        foreach (WorldGenerationSettings.DifficultyPreset preset in Enum.GetValues(typeof(WorldGenerationSettings.DifficultyPreset)))
        {
            if (preset == custom)
                continue;

            if (mpMultiplier.Value != WorldGenerationSettings.GetMPMultiplier(preset))
                continue;

            difficultyPresetButton.Selected = DifficultyPresetValueToIndex(preset);
            difficultyPresetAdvancedButton.Selected = DifficultyPresetValueToIndex(preset);
            return;
        }

        difficultyPresetButton.Selected = DifficultyPresetValueToIndex(custom);
        difficultyPresetAdvancedButton.Selected = DifficultyPresetValueToIndex(custom);
    }

    private void OnMPMultiplierValueChanged(double amount)
    {
        mpMultiplierReadout.Text = amount.ToString();
        settings.MPMultiplier = amount;

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