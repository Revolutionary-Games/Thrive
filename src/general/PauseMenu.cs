using System;
using Godot;

/// <summary>
///   Handles logic in the pause menu
/// </summary>
public class PauseMenu : Control
{
    [Export]
    public string HelpCategory;

    [Export]
    public NodePath PrimaryMenuPath;

    [Export]
    public NodePath HelpScreenPath;

    [Export]
    public NodePath LoadMenuPath;

    [Export]
    public NodePath SaveMenuPath;

    [Export]
    public NodePath LoadSaveListPath;

    private Control primaryMenu;
    private HelpScreen helpScreen;
    private Control loadMenu;
    private NewSaveMenu saveMenu;
    private SaveList loadSaveList;

    [Signal]
    public delegate void OnClosed();

    /// <summary>
    ///   Triggered when the user hits ESC to open the pause menu
    /// </summary>
    [Signal]
    public delegate void OnOpenWithKeyPress();

    /// <summary>
    ///   Called when a save needs to be made
    /// </summary>
    /// <param name="name">Name of the save to make or empty string</param>
    [Signal]
    public delegate void MakeSave(string name);

    public override void _EnterTree()
    {
        // This needs to be done early here to make sure the help screen loads the right text
        helpScreen = GetNode<HelpScreen>(HelpScreenPath);
        helpScreen.Category = HelpCategory;
    }

    public override void _Ready()
    {
        primaryMenu = GetNode<Control>(PrimaryMenuPath);
        loadMenu = GetNode<Control>(LoadMenuPath);
        saveMenu = GetNode<NewSaveMenu>(SaveMenuPath);
        loadSaveList = GetNode<SaveList>(LoadSaveListPath);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            if (Visible)
            {
                SetActiveMenu("primary");

                EmitSignal(nameof(OnClosed));
            }
            else
            {
                EmitSignal(nameof(OnOpenWithKeyPress));
            }
        }
    }

    public void ShowHelpScreen()
    {
        SetActiveMenu("help");
        helpScreen.RandomizeEasterEgg();
    }

    private void ClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnClosed));
    }

    private void ReturnToMenuPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Unpause the game as well as close the pause menu
        GetTree().Paused = false;

        EmitSignal(nameof(OnClosed));

        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.3f, false);
        TransitionManager.Instance.StartTransitions(this, nameof(OnSwitchToMenu));
    }

    private void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Quit();
    }

    private void OpenHelpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("help");
        helpScreen.RandomizeEasterEgg();
    }

    private void CloseHelpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("primary");
    }

    private void OpenLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("load");
        loadSaveList.Refresh();
    }

    private void CloseLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("primary");
    }

    private void OpenSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("save");
        saveMenu.RefreshExisting();
    }

    private void CloseSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("primary");
    }

    private void ForwardSaveAction(string name)
    {
        SetActiveMenu("primary");

        // Close this first to get the menus out of the way to capture the save screenshot
        EmitSignal(nameof(OnClosed));
        EmitSignal(nameof(MakeSave), name);
    }

    private void SetActiveMenu(string menu)
    {
        helpScreen.Hide();
        primaryMenu.Hide();
        loadMenu.Hide();
        saveMenu.Hide();

        switch (menu)
        {
            case "primary":
                primaryMenu.Show();
                break;
            case "help":
                helpScreen.Show();
                break;
            case "load":
                loadMenu.Show();
                break;
            case "save":
                saveMenu.Show();
                break;
            default:
                throw new ArgumentException("unknown menu", nameof(menu));
        }
    }

    /// <summary>
    ///   Finishes the transition back to the main menu
    /// </summary>
    private void OnSwitchToMenu()
    {
        SceneManager.Instance.ReturnToMenu();
    }
}
