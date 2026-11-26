using System;
using System.Diagnostics;
using Godot;

/// <summary>
///   This is a singleton pause menu shared by all the stages in the game.
/// </summary>
[GodotAutoload]
public partial class PauseMenu : CanvasLayer
{
    private const double MENU_FADE_OUT_DURATION = 0.1;
    private const double MENU_FADE_IN_DURATION = 0.2;

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
    private Tween? menuTransition;
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

            if (currentActiveMenu == ActiveMenuType.Achievements)
                achievementsView.ClosePopup();

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
                case ActiveMenuType.Achievements:
                {
                    achievementsView.Show();
                    achievementsView.OpenPopup();
                    break;
                }

                case ActiveMenuType.None:
                    // just close the current menu
                    break;

                default:
                {
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
        // Pause menu starts off hidden
        Visible = false;

        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // We want to animate the achievements closing so we disable the default close
        achievementsView.DeferPopupClose = true;

        unsavedProgressWarning.Connect(CustomWindow.SignalName.Canceled, new Callable(this, nameof(CancelExit)));

        if (GameProperties != null)
            thriveopedia.CurrentGame = GameProperties;
    }

    public override void _EnterTree()
    {
        // Register instance early so that directly started game scenes work correctly and can register things
        if (instance != null)
        {
            GD.PrintErr("Multiple PauseMenu singletons exist!");

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif
            return;
        }

        instance = this;

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

        if (instance == this)
            instance = null;
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
        StopMenuTransition();
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
            ForgetCurrentlyOpenPage();

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

        GUICommon.Instance.PlayButtonPressSound();
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
        OpenToSpeciesPage(species.ID);
    }

    public void OpenToSpeciesPage(uint speciesId)
    {
        if (GameLoading)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        ThriveopediaManager.OpenPage($"species:{speciesId}");
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
        StopMenuTransition();
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
        StopMenuTransition();

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

    private void TransitionToMenu(ActiveMenuType nextMenu, bool skipFadeOut = false)
    {
        if (!paused)
            return;

        var currentMenu = ActiveMenu;
        if (currentMenu == nextMenu)
        {
            if (skipFadeOut)
                StopMenuTransition();

            return;
        }

        var currentOpacity = GetControlFromMenuEnum(currentMenu)?.Modulate.A ?? 1.0f;
        StopMenuTransition();

        var tween = CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        menuTransition = tween;

        if (skipFadeOut)
        {
            SwitchAtMidpoint();
        }
        else
        {
            SetMenuOpacity(currentMenu, currentOpacity);
            tween.TweenMethod(Callable.From<float>(opacity => SetMenuOpacity(currentMenu, opacity)),
                currentOpacity, 0.0f, MENU_FADE_OUT_DURATION);
            tween.TweenCallback(Callable.From(SwitchAtMidpoint));
        }

        tween.TweenMethod(Callable.From<float>(opacity => SetMenuOpacity(nextMenu, opacity)),
            0.0f, 1.0f, MENU_FADE_IN_DURATION);
        tween.TweenCallback(Callable.From(() => menuTransition = null));

        void SwitchAtMidpoint()
        {
            SetMenuOpacity(nextMenu, 0.0f);
            ActiveMenu = nextMenu;
            SetMenuOpacity(currentMenu, 1.0f);
        }
    }

    private void StopMenuTransition()
    {
        if (menuTransition == null)
            return;

        menuTransition.Kill();
        menuTransition = null;
        SetMenuOpacity(ActiveMenu, 1.0f);
    }

    private void SetMenuOpacity(ActiveMenuType menu, float opacity)
    {
        var control = GetControlFromMenuEnum(menu);
        control?.Modulate = control.Modulate with { A = opacity };

        // The achievements window is a top-level control and needs its own modulation
        if (menu == ActiveMenuType.Achievements)
            achievementsView.SetPopupOpacity(opacity);
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
        StopMenuTransition();
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

        if (!paused)
            Open();

        // The requested page needs the Thriveopedia to be open when ChangePage is called
        TransitionToMenu(ActiveMenuType.Thriveopedia, true);
        thriveopedia.ChangePage(pageName, false);
    }

    private void OpenThriveopediaPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        TransitionToMenu(ActiveMenuType.Thriveopedia);
    }

    private void CloseHelpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionToMenu(ActiveMenuType.Primary);
    }

    private void OpenAchievementsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionToMenu(ActiveMenuType.Achievements);
    }

    private void OpenLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionToMenu(ActiveMenuType.Load);
    }

    private void CloseLoadPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionToMenu(ActiveMenuType.Primary);
    }

    private void OpenOptionsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionToMenu(ActiveMenuType.Options);
    }

    private void OpenReportBugPressed()
    {
        OS.ShellOpen("https://community.revolutionarygamesstudio.com/c/bug-reports/13");
    }

    private void OnThriveopediaClosed()
    {
        TransitionToMenu(ActiveMenuType.Primary);
    }

    private void OnAchievementsClosed()
    {
        TransitionToMenu(ActiveMenuType.Primary);
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
        TransitionToMenu(ActiveMenuType.Primary);
    }

    private void OpenSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionToMenu(ActiveMenuType.Save);
    }

    private void CloseSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionToMenu(ActiveMenuType.Primary);
    }

    private void ForwardSaveAction(string name)
    {
        ForgetCurrentlyOpenPage();

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
