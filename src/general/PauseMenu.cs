﻿using System;
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

    private Control primaryMenu;
    private HelpScreen helpScreen;
    private Control loadMenu;
    private OptionsMenu optionsMenu;
    private NewSaveMenu saveMenu;

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

    private enum ActiveMenuType
    {
        Primary,
        Help,
        Load,
        Options,y
        Save,
        None,
    }

    /// <summary>
    ///   The GameProperties object holding settings and state for the current game session.
    /// </summary>
    public GameProperties GameProperties { get; set; }

    private ActiveMenuType ActiveActiveMenu
    {
        get
        {
            foreach (ActiveMenuType menuEnumValue in Enum.GetValues(typeof(ActiveMenuType)))
            {
                if (GetControlFromMenuEnum(menuEnumValue)?.Visible == true)
                    return menuEnumValue;
            }

            return ActiveMenuType.None;
        }
        set
        {
            if (value == ActiveActiveMenu)
                return;

            GetControlFromMenuEnum(ActiveActiveMenu)?.Hide();

            switch (value)
            {
                case ActiveMenuType.Options:
                    optionsMenu.OpenFromInGame(GameProperties);
                    break;
                case ActiveMenuType.None:
                    // just close the current menu
                    break;
                default:
                    GetControlFromMenuEnum(value).Show();
                    break;
            }
        }
    }

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
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.PAUSE_MENU_CANCEL_PRIORITY)]
    public bool EscapeKeyPressed()
    {
        if (Visible)
        {
            ActiveActiveMenu = ActiveMenuType.Primary;

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
        if (ActiveActiveMenu == ActiveMenuType.Help)
            return;

        ActiveActiveMenu = ActiveMenuType.Help;
        helpScreen.RandomizeEasterEgg();
    }

    private Control GetControlFromMenuEnum(ActiveMenuType value)
    {
        return value switch
        {
            ActiveMenuType.Primary => primaryMenu,
            ActiveMenuType.Help => helpScreen,
            ActiveMenuType.Load => loadMenu,
            ActiveMenuType.Options => optionsMenu,
            ActiveMenuType.Save => saveMenu,
            ActiveMenuType.None => null,
            _ => throw new NotSupportedException($"{value} is not supported"),
        };
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

        // Unpause the game
        GetTree().Paused = false;

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.1f, false);
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

        ActiveActiveMenu = ActiveMenuType.Help;
        helpScreen.RandomizeEasterEgg();
    }

    private void CloseHelpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveActiveMenu = ActiveMenuType.Load;
    }

    private void CloseLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenOptionsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveActiveMenu = ActiveMenuType.Options;
    }

    private void OnOptionsClosed()
    {
        ActiveActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveActiveMenu = ActiveMenuType.Save;
    }

    private void CloseSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveActiveMenu = ActiveMenuType.Primary;
    }

    private void ForwardSaveAction(string name)
    {
        ActiveActiveMenu = ActiveMenuType.Primary;

        // Close this first to get the menus out of the way to capture the save screenshot
        EmitSignal(nameof(OnClosed));
        EmitSignal(nameof(MakeSave), name);
    }

    /// <summary>
    ///   Finishes the transition back to the main menu
    /// </summary>
    private void OnSwitchToMenu()
    {
        SceneManager.Instance.ReturnToMenu();
    }
}
