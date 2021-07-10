using System;
using Godot;

/// <summary>
///   Handles logic in the pause menu
/// </summary>
public class PauseMenu : ControlWithInput
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
    public NodePath OptionsMenuPath;

    [Export]
    public NodePath SaveMenuPath;

    [Export]
    public NodePath LoadSaveListPath;

	[Export]
	public NodePath ExitConfirmationDialogPath;

    private Control primaryMenu;
    private HelpScreen helpScreen;
    private Control loadMenu;
    private OptionsMenu optionsMenu;
    private NewSaveMenu saveMenu;
	private ConfirmationDialog exitConfirmationDialog;

	private ExitType exitType;

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

	/// <summary>
	///	  Types of exit the player requests. Mainly used for game exit confirmation.
	/// </summary>
	public enum ExitType
	{
		ReturnToMenu,
		QuitGame,
	}

    /// <summary>
    ///   The GameProperties object holding settings and state for the current game session.
    /// </summary>
    public GameProperties GameProperties { get; set; }

    public override void _EnterTree()
    {
        // This needs to be done early here to make sure the help screen loads the right text
        helpScreen = GetNode<HelpScreen>(HelpScreenPath);
        helpScreen.Category = HelpCategory;
        base._EnterTree();
    }

    public override void _Ready()
    {
        primaryMenu = GetNode<Control>(PrimaryMenuPath);
        loadMenu = GetNode<Control>(LoadMenuPath);
        optionsMenu = GetNode<OptionsMenu>(OptionsMenuPath);
        saveMenu = GetNode<NewSaveMenu>(SaveMenuPath);
		exitConfirmationDialog = GetNode<ConfirmationDialog>(ExitConfirmationDialogPath);

		exitConfirmationDialog.GetOk().Text = TranslationServer.Translate("YES");
		exitConfirmationDialog.GetCancel().Text = TranslationServer.Translate("NO");
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.PAUSE_MENU_CANCEL_PRIORITY)]
    public bool EscapeKeyPressed()
    {
        if (Visible)
        {
            SetActiveMenu("primary");

            EmitSignal(nameof(OnClosed));

            return true;
        }

        if (NoExclusiveTutorialActive())
        {
            EmitSignal(nameof(OnOpenWithKeyPress));
            return true;
        }

        // Not handled, pass through.
        return false;
    }

    [RunOnKeyDown("help")]
    public void ShowHelpPressed()
    {
        if (NoExclusiveTutorialActive())
        {
            EmitSignal(nameof(OnOpenWithKeyPress));
            ShowHelpScreen();
        }
    }

    public void ShowHelpScreen()
    {
        SetActiveMenu("help");
        helpScreen.RandomizeEasterEgg();
    }

    private bool NoExclusiveTutorialActive()
    {
        return GameProperties.TutorialState?.ExclusiveTutorialActive() != true;
    }

    private void ClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnClosed));
    }

    private void ReturnToMenuPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

		exitType = ExitType.ReturnToMenu;

		if (SaveHelper.SaveIsRecentlyPerformed)
		{
			ConfirmExit();
		}
		else
		{
			ShowExitConfirmationDialog(TranslationServer.Translate("RETURN_TO_MENU_WARNING"));
		}
    }

    private void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        
		exitType = ExitType.QuitGame;

		if (SaveHelper.SaveIsRecentlyPerformed)
		{
			ConfirmExit();
		}
		else
		{
			ShowExitConfirmationDialog(TranslationServer.Translate("QUIT_GAME_WARNING"));
		}
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
    }

    private void CloseLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("primary");
    }

    private void OpenOptionsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("options");
    }

    private void OnOptionsClosed()
    {
        SetActiveMenu("primary");
    }

    private void OpenSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("save");
    }

    private void CloseSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetActiveMenu("primary");
    }

	private void ConfirmExit()
	{
		switch (exitType)
		{
			case ExitType.ReturnToMenu:
			{
				// Unpause the game
				GetTree().Paused = false;

				TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.1f, false);
				TransitionManager.Instance.StartTransitions(this, nameof(OnSwitchToMenu));
				break;
			}

			case ExitType.QuitGame:
			{
				GetTree().Quit();
				break;
			}
		}
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
        optionsMenu.Hide();
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
            case "options":
                optionsMenu.OpenFromInGame(GameProperties);
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

	private void ShowExitConfirmationDialog(string message)
	{
		exitConfirmationDialog.GetNode<Label>("DialogText").Text = message;
		exitConfirmationDialog.PopupCenteredShrink();
		exitConfirmationDialog.GetOk().ReleaseFocus();
	}
}
