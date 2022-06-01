using System;
using Godot;

public class NewGameSettings : ControlWithInput
{
    [Export]
    public NodePath LAWKButtonPath = null!;

    [Export]
    public NodePath GameSeedPath = null!;

    [Export]
    public NodePath ConfirmButtonPath = null!;

    private Button lawkButton = null!;
    private LineEdit gameSeed = null!;
    private Button confirmButton = null!;

    [Signal]
    public delegate void OnNewGameSettingsClosed();

    private WorldGenerationSettings settings = new WorldGenerationSettings();

    private bool isGameSeedValid;

    public override void _Ready()
    {
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
        isGameSeedValid = int.TryParse(text, out seed);
        ReportValidityOfGameSeed(isGameSeedValid);
        if (isGameSeedValid)
            settings.seed = seed;
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

        // TBA
    }

    private void OnConfirmPressed()
    {
        if (!isGameSeedValid)
            return;

        // Disable the button to prevent it being executed again.
        confirmButton.Disabled = true;

        var scene = GD.Load<PackedScene>("res://src/general/MainMenu.tscn");
        var mainMenu = (MainMenu)scene.Instance();
        mainMenu.NewGameSetupDone(settings);
    }

    private void OnDifficultyPresetSelected(int index)
    {
        switch (index)
        {
            case 0:
                settings.difficultyPreset = WorldGenerationSettings.DifficultyPreset.Easy;
                break;
            case 2:
                settings.difficultyPreset = WorldGenerationSettings.DifficultyPreset.Hard;
                break;
            default:
                settings.difficultyPreset = WorldGenerationSettings.DifficultyPreset.Normal;
                break;
        }
    }

    private void OnLifeOriginSelected(int index)
    {
        switch (index)
        {
            case 1:
                settings.lifeOrigin = WorldGenerationSettings.LifeOrigin.Pond;
                break;
            case 2:
                settings.lifeOrigin = WorldGenerationSettings.LifeOrigin.Panspermia;
                break;
            default:
                settings.lifeOrigin = WorldGenerationSettings.LifeOrigin.Vent;
                break;
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