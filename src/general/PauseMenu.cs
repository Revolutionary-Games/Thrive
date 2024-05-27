using System;
using Godot;

/// <summary>
///   Handles logic in the pause menu
/// </summary>
public partial class PauseMenu : TopLevelContainer
{
    [Export]
    public string HelpCategory = null!;

    [Export]
    public NodePath? PrimaryMenuPath;

    [Export]
    public NodePath ThriveopediaPath = null!;

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

#pragma warning disable CA2213
    private Control primaryMenu = null!;
    private Thriveopedia? thriveopedia;
    private HelpScreen helpScreen = null!;
    private Control loadMenu = null!;
    private OptionsMenu optionsMenu = null!;
    private NewSaveMenu saveMenu = null!;
    private CustomConfirmationDialog unsavedProgressWarning = null!;
    private AnimationPlayer animationPlayer = null!;
#pragma warning restore CA2213

    private GameProperties? gameProperties;

    private bool paused;

    /// <summary>
    ///   The assigned pending exit type, will be used to specify what kind of
    ///   game exit will be performed on exit confirmation.
    /// </summary>
    private ExitType exitType;

    private bool exiting;

    private int exitTries;

    [Signal]
    public delegate void OnResumedEventHandler();

    /// <summary>
    ///   Triggered when the user hits ESC to open the pause menu
    /// </summary>
    [Signal]
    public delegate void OnOpenWithKeyPressEventHandler();

    /// <summary>
    ///   Called when a save needs to be made
    /// </summary>
    /// <param name="name">Name of the save to make or empty string</param>
    [Signal]
    public delegate void MakeSaveEventHandler(string name);

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
        Thriveopedia,
        Help,
        Load,
        Options,
        Save,
        None,
    }

    /// <summary>
    ///   The GameProperties object holding settings and state for the current game session.
    /// </summary>
    public GameProperties? GameProperties
    {
        get => gameProperties;
        set
        {
            gameProperties = value;

            // Forward the game properties to the Thriveopedia, even before it is opened for it to respond to
            // data requests
            if (gameProperties != null && thriveopedia != null)
                thriveopedia.CurrentGame = value;
        }
    }

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

            if (ModalManager.Instance.IsTopMostPopupExclusive)
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
                case ActiveMenuType.Thriveopedia:
                    thriveopedia!.OpenInGame(GameProperties ??
                        throw new InvalidOperationException(
                            $"{nameof(GameProperties)} is required before opening Thriveopedia in-game"));
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

    private bool Paused
    {
        set
        {
            if (paused == value)
                return;

            if (paused)
            {
                PauseManager.Instance.Resume(nameof(PauseMenu));
            }
            else
            {
                PauseManager.Instance.AddPause(nameof(PauseMenu));
            }

            paused = value;
        }
    }

    public override void _Ready()
    {
        // We have our custom logic for this
        PreventsMouseCaptureWhileOpen = false;

        primaryMenu = GetNode<Control>(PrimaryMenuPath);
        thriveopedia = GetNode<Thriveopedia>(ThriveopediaPath);
        loadMenu = GetNode<Control>(LoadMenuPath);
        optionsMenu = GetNode<OptionsMenu>(OptionsMenuPath);
        saveMenu = GetNode<NewSaveMenu>(SaveMenuPath);
        unsavedProgressWarning = GetNode<CustomConfirmationDialog>(UnsavedProgressWarningPath);
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        unsavedProgressWarning.Connect(CustomWindow.SignalName.Canceled, new Callable(this, nameof(CancelExit)));

        if (GameProperties != null)
            thriveopedia.CurrentGame = GameProperties;
    }

    public override void _EnterTree()
    {
        // This needs to be done early here to make sure the help screen loads the right text
        helpScreen = GetNode<HelpScreen>(HelpScreenPath);
        helpScreen.Category = HelpCategory;
        InputManager.RegisterReceiver(this);

        GetTree().AutoAcceptQuit = false;

        ThriveopediaManager.Instance.OnPageOpenedHandler += OnThriveopediaOpened;

        base._EnterTree();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InputManager.UnregisterReceiver(this);
        Paused = false;

        GetTree().AutoAcceptQuit = true;

        ThriveopediaManager.Instance.OnPageOpenedHandler -= OnThriveopediaOpened;
    }

    public override void _Notification(int notification)
    {
        base._Notification(notification);

        if (notification == NotificationWMCloseRequest)
        {
            // For some reason we need to perform this later, otherwise Godot complains about a node being busy
            // setting up children
            Invoke.Instance.Perform(ExitPressed);
        }
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.PAUSE_MENU_CANCEL_PRIORITY)]
    public bool EscapeKeyPressed()
    {
        if (Visible)
        {
            ActiveMenu = ActiveMenuType.Primary;

            Close();
            EmitSignal(SignalName.OnResumed);

            return true;
        }

        if (IsPausingBlocked)
            return false;

        Open();
        EmitSignal(SignalName.OnOpenWithKeyPress);

        return true;
    }

    [RunOnKeyDown("help")]
    public bool ShowHelpPressed()
    {
        if (IsPausingBlocked)
            return false;

        Open();
        EmitSignal(SignalName.OnOpenWithKeyPress);

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

    protected override void OnOpen()
    {
        // Godot being very silly: https://github.com/godotengine/godot/issues/73908
        if (animationPlayer == null!)
            return;

        animationPlayer.Play("Open");
        Paused = true;
        exiting = false;
    }

    protected override void OnClose()
    {
        animationPlayer.Play("Close");
        Paused = false;

        // Uncapture the mouse while we are playing the close animation, this doesn't seem to actually uncapture the
        // mouse any faster, though, likely an engine problem
        MouseUnCaptureActive = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PrimaryMenuPath != null)
            {
                PrimaryMenuPath.Dispose();
                ThriveopediaPath.Dispose();
                HelpScreenPath.Dispose();
                LoadMenuPath.Dispose();
                OptionsMenuPath.Dispose();
                SaveMenuPath.Dispose();
                LoadSaveListPath.Dispose();
                UnsavedProgressWarningPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private Control? GetControlFromMenuEnum(ActiveMenuType value)
    {
        return value switch
        {
            ActiveMenuType.Primary => primaryMenu,
            ActiveMenuType.Thriveopedia => thriveopedia,
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
        EmitSignal(SignalName.OnResumed);
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
            unsavedProgressWarning.DialogText = Localization.Translate("RETURN_TO_MENU_WARNING");
            unsavedProgressWarning.PopupCenteredShrink();
        }
    }

    private void ExitPressed()
    {
        exitType = ExitType.QuitGame;

        ++exitTries;

        if (SaveHelper.SavedRecently || !Settings.Instance.ShowUnsavedProgressWarning
            || exitTries >= Constants.FORCE_CLOSE_AFTER_TRIES)
        {
            ConfirmExit();
        }
        else
        {
            GUICommon.Instance.PlayButtonPressSound();
            unsavedProgressWarning.DialogText = Localization.Translate("QUIT_GAME_WARNING");
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
        if (exiting)
            return;

        exiting = true;

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

    private void CancelExit()
    {
        exitTries = 0;
    }

    private void ReturnToMenu()
    {
        Paused = false;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, OnSwitchToMenu, false);
    }

    private void Quit()
    {
        SceneManager.Instance.QuitThrive();
    }

    private void OnThriveopediaOpened(string pageName)
    {
        if (thriveopedia == null)
            throw new InvalidOperationException("Pause menu needs to be added to the scene first");

        Open();
        OpenThriveopediaPressed();
        thriveopedia.ChangePage(pageName);
    }

    private void OpenThriveopediaPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Thriveopedia;
    }

    private void CloseHelpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (GameProperties != null)
        {
            if (GameProperties.GameWorld.WorldSettings.HardcoreMode)
            {
                return;
            }
        }

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

    private void OpenReportBugPressed()
    {
        OS.ShellOpen("https://community.revolutionarygamesstudio.com/c/bug-reports/13");
    }

    private void OnThriveopediaClosed()
    {
        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OnSceneChangedFromThriveopedia()
    {
        // Remove all pause locks before changing to the new game
        PauseManager.Instance.ForceClear();

        MouseUnCaptureActive = false;
    }

    private void OnOptionsClosed()
    {
        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (GameProperties != null)
        {
            if (GameProperties.GameWorld.WorldSettings.HardcoreMode)
            {
                return;
            }
        }

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
        EmitSignal(SignalName.OnResumed);
        EmitSignal(SignalName.MakeSave, name);
        Paused = false;
    }

    /// <summary>
    ///   Finishes the transition back to the main menu
    /// </summary>
    private void OnSwitchToMenu()
    {
        MouseUnCaptureActive = false;
        SceneManager.Instance.ReturnToMenu();
    }

    private void OnLoadSaveConfirmed(SaveListItem item)
    {
        item.LoadThisSave();
    }

    private void OnSaveLoaded(string saveName)
    {
        _ = saveName;
        Paused = false;
        MouseUnCaptureActive = false;
    }
}
