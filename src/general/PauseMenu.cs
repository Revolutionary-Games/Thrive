﻿using System;
using System.Diagnostics.CodeAnalysis;
using Godot;

/// <summary>
///   Handles logic in the pause menu
/// </summary>
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification =
    "We don't manually dispose Godot derived types")]
public class PauseMenu : CustomDialog
{
    [Export]
    public string HelpCategory = null!;

    [Export]
    public NodePath PrimaryMenuPath = null!;

    [Export]
    public NodePath HelpScreenPath = null!;

    [Export]
    public NodePath LoadMenuPath = null!;

    [Export]
    public NodePath OptionsMenuPath = null!;

    [Export]
    public NodePath SaveMenuPath = null!;

    [Export]
    public NodePath LoadSaveListPath = null!;

    [Export]
    public NodePath UnsavedProgressWarningPath = null!;

    private Control primaryMenu = null!;
    private HelpScreen helpScreen = null!;
    private Control loadMenu = null!;
    private OptionsMenu optionsMenu = null!;
    private NewSaveMenu saveMenu = null!;
    private CustomConfirmationDialog unsavedProgressWarning = null!;
    private AnimationPlayer animationPlayer = null!;

    /// <summary>
    ///   The assigned pending exit type, will be used to specify what kind of
    ///   game exit will be performed on exit confirmation.
    /// </summary>
    private ExitType exitType;

    [Signal]
    public delegate void OnResumed();

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
    ///   Types of exit the player can request. Used to store the action for when the warning popup
    ///   about this is closed.
    /// </summary>
    public enum ExitType
    {
        ReturnToMenu,
        QuitGame,
    }

    private enum ActiveMenuType
    {
        Primary,
        Help,
        Load,
        Options,
        Save,
        None,
    }

    /// <summary>
    ///   The GameProperties object holding settings and state for the current game session.
    /// </summary>
    public GameProperties? GameProperties { get; set; } = null!;

    public bool GameLoading { get; set; }

    /// <summary>
    ///   If true the user may not open the pause menu.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Does not automatically close the pause menu when set to true.
    ///   </para>
    /// </remarks>
    public bool IsPausingBlocked
    {
        get
        {
            if (GameLoading)
                return true;

            if (GUICommon.Instance.IsAnyExclusivePopupActive)
                return true;

            if (TransitionManager.Instance.HasQueuedTransitions)
                return true;

            return false;
        }
    }

    private ActiveMenuType ActiveMenu
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
            var currentActiveMenu = ActiveMenu;
            if (value == currentActiveMenu)
                return;

            GetControlFromMenuEnum(currentActiveMenu)?.Hide();

            switch (value)
            {
                case ActiveMenuType.Options:
                    optionsMenu.OpenFromInGame(GameProperties ??
                        throw new InvalidOperationException(
                            $"{nameof(GameProperties)} is required before opening options"));
                    break;
                case ActiveMenuType.None:
                    // just close the current menu
                    break;
                default:
                    var control = GetControlFromMenuEnum(value);
                    if (control == null)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value),
                            "Can't set active menu to one without an associated control");
                    }

                    control.Show();
                    break;
            }
        }
    }

    public override void _EnterTree()
    {
        // This needs to be done early here to make sure the help screen loads the right text
        helpScreen = GetNode<HelpScreen>(HelpScreenPath);
        helpScreen.Category = HelpCategory;
        InputManager.RegisterReceiver(this);

        base._EnterTree();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InputManager.UnregisterReceiver(this);
    }

    public override void _Ready()
    {
        primaryMenu = GetNode<Control>(PrimaryMenuPath);
        loadMenu = GetNode<Control>(LoadMenuPath);
        optionsMenu = GetNode<OptionsMenu>(OptionsMenuPath);
        saveMenu = GetNode<NewSaveMenu>(SaveMenuPath);
        unsavedProgressWarning = GetNode<CustomConfirmationDialog>(UnsavedProgressWarningPath);
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.PAUSE_MENU_CANCEL_PRIORITY)]
    public bool EscapeKeyPressed()
    {
        if (Visible)
        {
            ActiveMenu = ActiveMenuType.Primary;

            Close();
            EmitSignal(nameof(OnResumed));

            return true;
        }

        if (IsPausingBlocked)
            return false;

        Open();
        EmitSignal(nameof(OnOpenWithKeyPress));

        return true;
    }

    [RunOnKeyDown("help")]
    public bool ShowHelpPressed()
    {
        if (IsPausingBlocked)
            return false;

        Open();
        EmitSignal(nameof(OnOpenWithKeyPress));

        ShowHelpScreen();
        return true;
    }

    public void ShowHelpScreen()
    {
        if (ActiveMenu == ActiveMenuType.Help)
            return;

        ActiveMenu = ActiveMenuType.Help;
        helpScreen.RandomizeEasterEgg();
    }

    public void Open()
    {
        if (Visible)
            return;

        animationPlayer.Play("Open");
        PauseManager.Instance.AddPause(nameof(PauseMenu));
    }

    public void Close()
    {
        if (!Visible)
            return;

        animationPlayer.Play("Close");
        PauseManager.Instance.Resume(nameof(PauseMenu));
    }

    public void OpenToHelp()
    {
        Open();
        ShowHelpScreen();
    }

    public void SetNewSaveName(string name)
    {
        saveMenu.SetSaveName(name, true);
    }

    public void SetNewSaveNameFromSpeciesName()
    {
        if (GameProperties == null)
        {
            GD.PrintErr("No game properties set, can't set save name from species");
            return;
        }

        SetNewSaveName(GameProperties.GameWorld.PlayerSpecies.FormattedName.Replace(' ', '_'));
    }

    private Control? GetControlFromMenuEnum(ActiveMenuType value)
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

    private void ClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Close();
        EmitSignal(nameof(OnResumed));
    }

    private void ReturnToMenuPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        exitType = ExitType.ReturnToMenu;

        if (SaveHelper.SavedRecently || !Settings.Instance.ShowUnsavedProgressWarning)
        {
            ConfirmExit();
        }
        else
        {
            unsavedProgressWarning.DialogText = TranslationServer.Translate("RETURN_TO_MENU_WARNING");
            unsavedProgressWarning.PopupCenteredShrink();
        }
    }

    private void ExitPressed()
    {
        exitType = ExitType.QuitGame;

        if (SaveHelper.SavedRecently || !Settings.Instance.ShowUnsavedProgressWarning)
        {
            ConfirmExit();
        }
        else
        {
            GUICommon.Instance.PlayButtonPressSound();
            unsavedProgressWarning.DialogText = TranslationServer.Translate("QUIT_GAME_WARNING");
            unsavedProgressWarning.PopupCenteredShrink();
        }
    }

    private void OpenHelpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Help;
        helpScreen.RandomizeEasterEgg();
    }

    private void ConfirmExit()
    {
        switch (exitType)
        {
            case ExitType.ReturnToMenu:
                ReturnToMenu();
                break;
            case ExitType.QuitGame:
                Quit();
                break;
        }
    }

    private void ReturnToMenu()
    {
        // Unpause the game
        PauseManager.Instance.Resume(nameof(PauseMenu));

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.1f, false);
        TransitionManager.Instance.StartTransitions(this, nameof(OnSwitchToMenu));
    }

    private void Quit()
    {
        GetTree().Quit();
    }

    private void CloseHelpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Load;
    }

    private void CloseLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenOptionsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Options;
    }

    private void OnOptionsClosed()
    {
        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Save;
    }

    private void CloseSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Primary;
    }

    private void ForwardSaveAction(string name)
    {
        ActiveMenu = ActiveMenuType.Primary;

        // Close this first to get the menus out of the way to capture the save screenshot
        Hide();
        EmitSignal(nameof(OnResumed));
        EmitSignal(nameof(MakeSave), name);
    }

    /// <summary>
    ///   Finishes the transition back to the main menu
    /// </summary>
    private void OnSwitchToMenu()
    {
        SceneManager.Instance.ReturnToMenu();
    }

    private void OnLoadSaveConfirmed(SaveListItem item)
    {
        item.LoadThisSave();
    }
}
