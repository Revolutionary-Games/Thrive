using System;
using System.Diagnostics;
using Godot;

/// <summary>
///   This is a singleton pause menu shared by all the stages in the game.
/// </summary>
[GodotAutoload]
public partial class PauseMenu : CanvasLayer
{
#pragma warning disable CA2213
    private static PauseMenu? instance;

    [Export]
    private Control primaryMenu = null!;

    [Export]
    private Thriveopedia thriveopedia = null!;

    [Export]
    private AchievementsView achievementsView = null!;

    [Export]
    private Control loadMenu = null!;

    [Export]
    private OptionsMenu optionsMenu = null!;

    [Export]
    private NewSaveMenu saveMenu = null!;

    [Export]
    private CustomConfirmationDialog unsavedProgressWarning = null!;

    private AnimationPlayer animationPlayer = null!;
#pragma warning restore CA2213

    private GameProperties? gameProperties;

    private bool paused;

    /// <summary>
    ///   The assigned pending exit type will be used to specify what kind of
    ///   game exit will be performed on exit confirmation.
    /// </summary>
    private ExitType exitType;

    private bool exiting;

    private int exitTries;
    private bool mouseUnCaptureActive;

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
        Achievements,
        Load,
        Options,
        Save,
        None,
    }

    public static PauseMenu Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   Main game state. Needs to be set by each stage once it is ready for the pause menu and unset once the stage
    ///   is exiting.
    /// </summary>
    public MainGameState GameState { get; private set; } = MainGameState.Invalid;

    /// <summary>
    ///   The GameProperties object holding settings and state for the current game session.
    /// </summary>
    public GameProperties? GameProperties
    {
        get => gameProperties;
        private set
        {
            gameProperties = value;

            // Forward the game properties to the Thriveopedia, even before it is opened for it to respond to
            // data requests
            if (gameProperties != null)
                thriveopedia.CurrentGame = value;
        }
    }

    /// <summary>
    ///   True when the game is in a state that isn't properly in a stage
    /// </summary>
    public bool GameLoading => GameState == MainGameState.Invalid;

    /// <summary>
    ///   If true, the user may not open the pause menu.
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

    public bool MouseUnCaptureActive
    {
        get => mouseUnCaptureActive;
        private set
        {
            if (value == mouseUnCaptureActive)
                return;

            mouseUnCaptureActive = value;

            if (mouseUnCaptureActive)
            {
                MouseCaptureManager.ReportOpenCapturePrevention(nameof(PauseMenu));
            }
            else
            {
                MouseCaptureManager.ReportClosedCapturePrevention(nameof(PauseMenu));
            }
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
                    thriveopedia.OpenInGame(GameProperties ??
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
        if (instance != null)
        {
            GD.PrintErr("Multiple PauseMenu singletons exist!");

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif
            return;
        }

        // Pause menu starts off hidden
        Visible = false;

        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        unsavedProgressWarning.Connect(CustomWindow.SignalName.Canceled, new Callable(this, nameof(CancelExit)));

        if (GameProperties != null)
            thriveopedia.CurrentGame = GameProperties;

        instance = this;
    }

    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);

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

    public void ReportStageTransition()
    {
        GameState = MainGameState.Invalid;
        ApplyGameState();
    }

    public void ReportEnterGameState(MainGameState gameState, GameProperties? gameProperties)
    {
        GameState = gameState;
        GameProperties = gameProperties;

        ApplyGameState();
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.PAUSE_MENU_CANCEL_PRIORITY)]
    public bool EscapeKeyPressed()
    {
        if (Visible)
        {
            // TODO: should this force player back to the base of the menu?
            ActiveMenu = ActiveMenuType.Primary;

            Close();
            EmitSignal(SignalName.OnResumed);

            return true;
        }

        // If the pause menu is unused currently, ignore
        if (GameLoading)
            return false;

        if (IsPausingBlocked)
            return false;

        Open();
        EmitSignal(SignalName.OnOpenWithKeyPress);

        return true;
    }

    [RunOnKeyDown("help")]
    public void OpenToHelp()
    {
        if (GameLoading)
        {
            GD.Print("Can't open pause menu as not in a stage");
            return;
        }

        Open();
        OpenThriveopediaPressed();
        ThriveopediaManager.OpenPage("MechanicsRoot");
    }

    /// <summary>
    ///   Only show the pause menu with this and not by directly setting this visible!
    /// </summary>
    public void Open()
    {
        if (GameLoading)
        {
            GD.Print("Can't open pause menu as not in a stage");
            return;
        }

        Show();
        OnOpen();
    }

    public void Close(bool playAnimation = true)
    {
        if (!Visible)
            return;

        OnClose(playAnimation);
    }

    public void OpenToSpeciesPage(Species species)
    {
        if (GameLoading)
            return;

        Open();
        OpenThriveopediaPressed();

        // TODO: implement species specific pages: https://github.com/Revolutionary-Games/Thrive/issues/4043
        _ = species;
        GD.PrintErr("TODO: implement per-species pages in the Thriveopedia");
        ThriveopediaManager.OpenPage("EvolutionaryTree");
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

    /// <summary>
    ///   When next opened, the pause menu opens to the root
    /// </summary>
    public void ForgetCurrentlyOpenPage()
    {
        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OnOpen()
    {
        // Godot being very silly: https://github.com/godotengine/godot/issues/73908
        if (animationPlayer == null!)
            return;

        animationPlayer.Play("Open");
        Paused = true;
        exiting = false;

        MouseUnCaptureActive = true;
    }

    private void OnClose(bool playAnimation)
    {
        if (playAnimation)
        {
            animationPlayer.Play("Close");
        }
        else
        {
            Visible = false;
        }

        Paused = false;

        // Uncapture the mouse while we are playing the close animation, this doesn't seem to actually uncapture the
        // mouse any faster, though, likely an engine problem
        MouseUnCaptureActive = false;
    }

    private Control? GetControlFromMenuEnum(ActiveMenuType value)
    {
        return value switch
        {
            ActiveMenuType.Primary => primaryMenu,
            ActiveMenuType.Thriveopedia => thriveopedia,
            ActiveMenuType.Achievements => achievementsView,
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
        // If the pause menu is unused currently, ignore
        if (GameLoading)
            return;

        if (thriveopedia == null)
            throw new InvalidOperationException("Pause menu needs to be added to the scene first");

        Open();
        SwitchToThriveopedia();
        thriveopedia.ChangePage(pageName, false);
    }

    private void OpenThriveopediaPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SwitchToThriveopedia();
    }

    private void SwitchToThriveopedia()
    {
        // Switch without the sound
        ActiveMenu = ActiveMenuType.Thriveopedia;
    }

    private void CloseHelpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OpenAchievementsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ActiveMenu = ActiveMenuType.Achievements;
        achievementsView.OpenPopup();
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

    private void OpenReportBugPressed()
    {
        OS.ShellOpen("https://community.revolutionarygamesstudio.com/c/bug-reports/13");
    }

    private void OnThriveopediaClosed()
    {
        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OnAchievementsClosed()
    {
        ActiveMenu = ActiveMenuType.Primary;
    }

    private void OnSceneChangedFromThriveopedia()
    {
        // Remove all pause locks before changing to the new game
        PauseManager.Instance.ForceClear();
        ReportStageTransition();

        MouseUnCaptureActive = false;
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
        // This skips the animation explicitly
        Hide();
        EmitSignal(SignalName.OnResumed);
        EmitSignal(SignalName.MakeSave, name);
        Paused = false;
        MouseUnCaptureActive = false;
    }

    /// <summary>
    ///   Finishes the transition back to the main menu
    /// </summary>
    private void OnSwitchToMenu()
    {
        MouseUnCaptureActive = false;
        ReportStageTransition();
        Close(false);
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

    private void ApplyGameState()
    {
        GetTree().AutoAcceptQuit = GameLoading;
    }
}
