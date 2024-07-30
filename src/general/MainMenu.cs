﻿using System;
using System.Linq;
using Godot;
using Godot.Collections;
using LauncherThriveShared;
using Xoshiro.PRNG32;

/// <summary>
///   Class managing the main menu and everything in it
/// </summary>
public partial class MainMenu : NodeWithInput
{
    /// <summary>
    ///   Index of the current menu.
    /// </summary>
    [Export]
    public uint CurrentMenuIndex;

    /// <summary>
    ///   How many non-menu items there are in the menu container
    /// </summary>
    [Export]
    public int NonMenuItemsFirst = 1;

    [Export]
    public NodePath? ThriveLogoPath;

    /// <summary>
    ///   Needs to be a collection of <see cref="Texture2D"/>
    /// </summary>
    [Export]
    public Array<Texture2D> MenuBackgrounds = null!;

    /// <summary>
    ///   Needs to be a collection of paths to scenes
    /// </summary>
    [Export]
    public Array<string> Menu3DBackgroundScenes = null!;

    [Export]
    public NodePath FreebuildButtonPath = null!;

    [Export]
    public NodePath MulticellularFreebuildButtonPath = null!;

    [Export]
    public NodePath AutoEvoExploringButtonPath = null!;

    [Export]
    public NodePath MicrobeBenchmarkButtonPath = null!;

    [Export]
    public NodePath ExitToLauncherButtonPath = null!;

    [Export]
    public NodePath CreditsContainerPath = null!;

    [Export]
    public NodePath CreditsScrollPath = null!;

    [Export]
    public NodePath LicensesDisplayPath = null!;

    [Export]
    public NodePath SteamFailedPopupPath = null!;

    [Export]
    public NodePath ModLoadFailuresPath = null!;

    [Export]
    public NodePath SafeModeWarningPath = null!;

    [Export]
    public NodePath ModsInstalledButNotEnabledWarningPath = null!;

    [Export]
    public NodePath LowPerformanceWarningPath = null!;

    [Export]
    public NodePath SocialMediaContainerPath = null!;

    [Export]
    public NodePath ItchButtonPath = null!;

    [Export]
    public NodePath PatreonButtonPath = null!;

    [Export]
    public NodePath StoreLoggedInDisplayPath = null!;

    [Export]
    public NodePath ModManagerPath = null!;

    [Export]
    public NodePath GalleryViewerPath = null!;

    [Export]
    public NodePath NewsFeedPath = null!;

    [Export]
    public NodePath NewsFeedDisablerPath = null!;

    [Export]
    public NodePath PatchNotesPath = null!;

    [Export]
    public NodePath PatchNotesDisablerPath = null!;

    [Export]
    public NodePath FeedPositionerPath = null!;

    [Export]
    public NodePath ThanksDialogPath = null!;

    [Export]
    public NodePath MenusPath = null!;

#pragma warning disable CA2213
    private TextureRect background = null!;
    private Node3D? created3DBackground;

    private TextureRect thriveLogo = null!;
    private OptionsMenu options = null!;
    private NewGameSettings newGameSettings = null!;
    private AnimationPlayer guiAnimations = null!;
    private SaveManagerGUI saves = null!;
    private Thriveopedia thriveopedia = null!;
    private ModManager modManager = null!;
    private GalleryViewer galleryViewer = null!;

    private ThriveFeedDisplayer newsFeed = null!;
    private Control newsFeedDisabler = null!;

    private PatchNotesDisplayer patchNotes = null!;
    private Control patchNotesDisabler = null!;

    private Control feedPositioner = null!;

    private Control creditsContainer = null!;
    private CreditsScroll credits = null!;
    private LicensesDisplay licensesDisplay = null!;
    private Button freebuildButton = null!;
    private Button multicellularFreebuildButton = null!;
    private Button autoEvoExploringButton = null!;
    private Button microbeBenchmarkButton = null!;

    private Button exitToLauncherButton = null!;

    private Label storeLoggedInDisplay = null!;

    private Control socialMediaContainer = null!;

    [Export]
    private CustomWindow websiteButtonsContainer = null!;

    private TextureButton itchButton = null!;
    private TextureButton patreonButton = null!;

    [Export]
    private CustomConfirmationDialog openGlPopup = null!;

    private ErrorDialog modLoadFailures = null!;

    private CustomConfirmationDialog steamFailedPopup = null!;

    private CustomWindow safeModeWarning = null!;

    private PermanentlyDismissibleDialog modsInstalledButNotEnabledWarning = null!;
    private PermanentlyDismissibleDialog lowPerformanceWarning = null!;
    private PermanentlyDismissibleDialog thanksDialog = null!;

    private CenterContainer menus = null!;
#pragma warning restore CA2213

    private Array<Node>? menuArray;

    private bool introVideoPassed;

    private double timerForStartupSuccess = Constants.MAIN_MENU_TIME_BEFORE_STARTUP_SUCCESS;

    /// <summary>
    ///   True when we are able to show the thanks for buying popup due to being a store version
    /// </summary>
    private bool canShowThanks;

    /// <summary>
    ///   The store specific page link. Defaults to the website link if we don't know a valid store name
    /// </summary>
    private string storeBuyLink = "https://revolutionarygamesstudio.com/releases/";

    private double averageFrameRate;

    /// <summary>
    ///   Time tracking related to performance. Note that this is reset when performance tracking is restarted.
    /// </summary>
    private double secondsInMenu;

    private bool canShowLowPerformanceWarning = true;

    public bool IsReturningToMenu { get; set; }

    public static void OnEnteringGame()
    {
        CheatManager.OnCheatsDisabled();
        SaveHelper.ClearLastSaveTime();
        LastPlayedVersion.MarkCurrentVersionAsPlayed();
    }

    public override void _Ready()
    {
        if (SceneManager.QuitOrQuitting)
        {
            GD.Print("Skipping main menu initialization due to quitting");
            return;
        }

        // Unpause the game as the MainMenu should never be paused.
        PauseManager.Instance.ForceClear();
        MouseCaptureManager.ForceDisableCapture();

        RunMenuSetup();

        // Start intro video
        if (Settings.Instance.PlayIntroVideo && LaunchOptions.VideosEnabled && !IsReturningToMenu &&
            SafeModeStartupHandler.AreVideosAllowed())
        {
            // Hide menu buttons to prevent them grabbing focus during intro video
            GetCurrentMenu()?.Hide();

            SafeModeStartupHandler.ReportBeforeVideoPlaying();
            TransitionManager.Instance.AddSequence(
                TransitionManager.Instance.CreateCutscene("res://assets/videos/intro.ogv"), OnIntroEnded);
        }
        else
        {
            OnIntroEnded();
        }

        // Let all suppressed deletions happen (if we came back directly from the editor that was loaded from a save)
        TemporaryLoadedNodeDeleter.Instance.ReleaseAllHolds();

        CheckModFailures();

        // Start this early here to make sure this is ready as soon as possible
        // In the case where patch notes take up the news feed, this is still not a complete waste as if the player
        // exits to the menu after playing a bit they'll see the news feed
        if (Settings.Instance.ThriveNewsFeedEnabled)
        {
            ThriveNewsFeed.GetFeedContents();
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Settings.Instance.Menu3DBackgroundEnabled.OnChanged += OnMenuBackgroundTypeChanged;
        ThriveopediaManager.Instance.OnPageOpenedHandler += OnThriveopediaOpened;
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.Menu3DBackgroundEnabled.OnChanged -= OnMenuBackgroundTypeChanged;
        ThriveopediaManager.Instance.OnPageOpenedHandler -= OnThriveopediaOpened;
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Do startup success only after the intro video is played or skipped (and this is the first time in this run
        // that we are in the menu)
        if (introVideoPassed && !IsReturningToMenu)
        {
            if (canShowThanks)
            {
                if (!IsReturningToMenu &&
                    !Settings.Instance.IsNoticePermanentlyDismissed(DismissibleNotice.ThanksForBuying)
                    && !SteamFailed())
                {
                    GD.Print("We are most likely a store version of Thrive, showing the thanks dialog");

                    // The text has a store link template, so we need to update the right links into it
                    thanksDialog.DialogText =
                        Localization.Translate("THANKS_FOR_BUYING_THRIVE").FormatSafe(storeBuyLink);

                    thanksDialog.PopupCenteredShrink();
                }

                canShowThanks = false;
            }

            if (timerForStartupSuccess > 0)
            {
                timerForStartupSuccess -= delta;

                if (timerForStartupSuccess <= 0)
                {
                    CheckStartupSuccess();
                    WarnAboutNoEnabledMods();

                    if (SteamFailed())
                    {
                        GD.PrintErr("Steam init has failed, showing failure popup");
                        steamFailedPopup.PopupCenteredShrink();
                    }
                }
            }

            // Low menu performance will never be warned about if the popup has been dismissed,
            // if 3D backgrounds have been disabled, if the popup has been shown but not dismissed
            // on this menu session, or if the max framerate is set to 30 (or lower)
            // In addition, tracking only begins after one second in the menu
            if (!Settings.Instance.IsNoticePermanentlyDismissed(DismissibleNotice.LowPerformanceWarning)
                && Settings.Instance.Menu3DBackgroundEnabled && canShowLowPerformanceWarning
                && (Settings.Instance.MaxFramesPerSecond > 30 || Settings.Instance.MaxFramesPerSecond == 0))
            {
                secondsInMenu += delta;

                // Don't track performance when the 3D background aren't actually visible. For example when going to
                // the art gallery
                if (secondsInMenu >= 1 && created3DBackground?.Visible == true)
                {
                    averageFrameRate = TrackMenuPerformance();

                    WarnAboutLowPerformance();
                }
            }
        }
    }

    public override void _Notification(int notification)
    {
        base._Notification(notification);

        if (notification == NotificationWMCloseRequest)
        {
            GD.Print("Main window close signal detected");
            Invoke.Instance.Queue(QuitPressed);
        }
    }

    public void StartMusic()
    {
        Jukebox.Instance.PlayCategory("Menu");
    }

    /// <summary>
    ///   Sets the current menu index and then switches the menu
    /// </summary>
    /// <param name="index">Index of the menu. Set to <see cref="uint.MaxValue"/> to hide all menus</param>
    /// <param name="slide">If false then the menu slide animation will not be played</param>
    public void SetCurrentMenu(uint index, bool slide = true)
    {
        if (menuArray == null)
            throw new InvalidOperationException("Main menu has not been initialized");

        // Hide the website button container whenever anything else is pressed, and only display the social media icons
        // if a menu is visible
        websiteButtonsContainer.Visible = false;
        socialMediaContainer.Visible = index != uint.MaxValue;
        feedPositioner.Visible = index != uint.MaxValue;

        // Allow disabling all the menus for going to the options menu
        if (index > menuArray.Count - 1 && index != uint.MaxValue)
        {
            GD.PrintErr("Selected menu index is out of range!");
            return;
        }

        CurrentMenuIndex = index;

        if (slide)
        {
            PlayGUIAnimation("MenuSlide");
        }
        else
        {
            // Just switch the menu
            SwitchMenu();
        }
    }

    /// <summary>
    ///   This is when ESC is pressed. Main menu priority is lower than Options Menu
    ///   to avoid capturing ESC presses in the Options Menu.
    /// </summary>
    [RunOnKeyDown("ui_cancel", Priority = Constants.MAIN_MENU_CANCEL_PRIORITY)]
    public bool OnEscapePressed()
    {
        // In a sub menu (that doesn't have its own class)
        if (CurrentMenuIndex != 0 && CurrentMenuIndex < uint.MaxValue)
        {
            SetCurrentMenu(0);

            // Handled, stop here.
            return true;
        }

        if (CurrentMenuIndex == uint.MaxValue && saves.Visible)
        {
            OnReturnFromLoadGame();
            return true;
        }

        // Not handled, pass through.
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ThriveLogoPath != null)
            {
                ThriveLogoPath.Dispose();
                FreebuildButtonPath.Dispose();
                MulticellularFreebuildButtonPath.Dispose();
                AutoEvoExploringButtonPath.Dispose();
                MicrobeBenchmarkButtonPath.Dispose();
                ExitToLauncherButtonPath.Dispose();
                CreditsContainerPath.Dispose();
                CreditsScrollPath.Dispose();
                LicensesDisplayPath.Dispose();
                SteamFailedPopupPath.Dispose();
                ModLoadFailuresPath.Dispose();
                SafeModeWarningPath.Dispose();
                ModsInstalledButNotEnabledWarningPath.Dispose();
                LowPerformanceWarningPath.Dispose();
                SocialMediaContainerPath.Dispose();
                ItchButtonPath.Dispose();
                PatreonButtonPath.Dispose();
                StoreLoggedInDisplayPath.Dispose();
                ModManagerPath.Dispose();
                GalleryViewerPath.Dispose();
                NewsFeedPath.Dispose();
                NewsFeedDisablerPath.Dispose();
                PatchNotesPath.Dispose();
                PatchNotesDisablerPath.Dispose();
                FeedPositionerPath.Dispose();
                ThanksDialogPath.Dispose();
                MenusPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Setup the main menu.
    /// </summary>
    private void RunMenuSetup()
    {
        background = GetNode<TextureRect>("Background");
        guiAnimations = GetNode<AnimationPlayer>("GUIAnimations");
        thriveLogo = GetNode<TextureRect>(ThriveLogoPath);
        freebuildButton = GetNode<Button>(FreebuildButtonPath);
        multicellularFreebuildButton = GetNode<Button>(MulticellularFreebuildButtonPath);
        autoEvoExploringButton = GetNode<Button>(AutoEvoExploringButtonPath);
        microbeBenchmarkButton = GetNode<Button>(MicrobeBenchmarkButtonPath);
        exitToLauncherButton = GetNode<Button>(ExitToLauncherButtonPath);
        creditsContainer = GetNode<Control>(CreditsContainerPath);
        credits = GetNode<CreditsScroll>(CreditsScrollPath);
        licensesDisplay = GetNode<LicensesDisplay>(LicensesDisplayPath);
        storeLoggedInDisplay = GetNode<Label>(StoreLoggedInDisplayPath);
        modManager = GetNode<ModManager>(ModManagerPath);
        galleryViewer = GetNode<GalleryViewer>(GalleryViewerPath);
        newsFeed = GetNode<ThriveFeedDisplayer>(NewsFeedPath);
        newsFeedDisabler = GetNode<Control>(NewsFeedDisablerPath);
        patchNotes = GetNode<PatchNotesDisplayer>(PatchNotesPath);
        patchNotesDisabler = GetNode<Control>(PatchNotesDisablerPath);
        feedPositioner = GetNode<Control>(FeedPositionerPath);
        socialMediaContainer = GetNode<Control>(SocialMediaContainerPath);

        itchButton = GetNode<TextureButton>(ItchButtonPath);
        patreonButton = GetNode<TextureButton>(PatreonButtonPath);

        menuArray?.Clear();

        // Get all the menu items
        menuArray = GetTree().GetNodesInGroup("MenuItem");

        if (menuArray == null)
        {
            GD.PrintErr("Failed to find all the menu items!");
            return;
        }

        options = GetNode<OptionsMenu>("OptionsMenu");
        newGameSettings = GetNode<NewGameSettings>("NewGameSettings");
        saves = GetNode<SaveManagerGUI>("SaveManagerGUI");
        thriveopedia = GetNode<Thriveopedia>("Thriveopedia");
        modLoadFailures = GetNode<ErrorDialog>(ModLoadFailuresPath);
        safeModeWarning = GetNode<CustomWindow>(SafeModeWarningPath);
        steamFailedPopup = GetNode<CustomConfirmationDialog>(SteamFailedPopupPath);

        modsInstalledButNotEnabledWarning =
            GetNode<PermanentlyDismissibleDialog>(ModsInstalledButNotEnabledWarningPath);
        lowPerformanceWarning = GetNode<PermanentlyDismissibleDialog>(LowPerformanceWarningPath);
        thanksDialog = GetNode<PermanentlyDismissibleDialog>(ThanksDialogPath);
        menus = GetNode<CenterContainer>(MenusPath);

        // Set initial menu
        SwitchMenu();

        // Easter egg message
        thriveLogo.RegisterToolTipForControl("thriveLogoEasterEgg", "mainMenu");

        if (FeatureInformation.GetVideoDriver() == OS.RenderingDriver.Opengl3 && !IsReturningToMenu)
            openGlPopup.PopupCenteredShrink();

        UpdateStoreVersionStatus();
        UpdateLauncherState();

        // Hide patch notes when it does not want to be shown
        if (!Settings.Instance.ShowNewPatchNotes)
        {
            patchNotesDisabler.Visible = false;
        }
        else
        {
            ShowPatchInfoIfPossible();
        }
    }

    /// <summary>
    ///   Randomizes background images.
    /// </summary>
    private void RandomizeBackground()
    {
        var random = new XoShiRo128starstar();

        // Some of the 3D backgrounds render very incorrectly in opengl so they are disabled (even with Godot 4 this
        // hasn't improved a lot)
        if (Settings.Instance.Menu3DBackgroundEnabled &&
            FeatureInformation.GetVideoDriver() != OS.RenderingDriver.Opengl3)
        {
            SetBackgroundScene(Menu3DBackgroundScenes.Random(random));
        }
        else
        {
            var chosenBackground = MenuBackgrounds.Random(random);

            SetBackground(chosenBackground);
        }
    }

    private void SetBackground(Texture2D backgroundImage)
    {
        background.Visible = true;
        background.Texture = backgroundImage;

        if (created3DBackground != null)
        {
            created3DBackground.DetachAndQueueFree();
            created3DBackground = null;
        }
    }

    private void SetBackgroundScene(string path)
    {
        var backgroundScene = GD.Load<PackedScene>(path);

        if (backgroundScene == null)
        {
            GD.PrintErr("Failed to load menu background: ", path);
            return;
        }

        // We can get by waiting one frame before the missing background is visible, this slightly reduces the lag
        // lag spike when loading the main menu
        Invoke.Instance.Queue(() =>
        {
            // These are done here to ensure there isn't a weird single frame with a grey menu background
            background.Visible = false;
            if (created3DBackground != null)
            {
                created3DBackground.DetachAndQueueFree();
                created3DBackground = null;
            }

            created3DBackground = backgroundScene.Instantiate<Node3D>();
            AddChild(created3DBackground);
        });
    }

    /// <summary>
    ///   Returns the container for the current menu.
    /// </summary>
    /// <returns>Null if we aren't in any available menu or the menu container if there is one.</returns>
    /// <exception cref="System.InvalidOperationException">The main menu hasn't been initialized.</exception>
    private Control? GetCurrentMenu()
    {
        if (menuArray == null)
            throw new InvalidOperationException("Main menu has not been initialized");
        if (menuArray.Count <= 0)
            throw new InvalidOperationException("Main menu has no menus");

        return CurrentMenuIndex == uint.MaxValue ? null : menus.GetChild<Control>((int)CurrentMenuIndex);
    }

    private void OnMenuBackgroundTypeChanged(bool value)
    {
        RandomizeBackground();
    }

    private void UpdateStoreVersionStatus()
    {
        if (!IsReturningToMenu)
        {
            if (!string.IsNullOrEmpty(LaunchOptions.StoreVersionName))
            {
                GD.Print($"Launcher tells us that we are store version: {LaunchOptions.StoreVersionName}");
            }
        }

        canShowThanks = false;

        if (!string.IsNullOrEmpty(LaunchOptions.StoreVersionName))
        {
            GD.Print("Launcher told us store name: ", LaunchOptions.StoreVersionName);
            canShowThanks = true;

            switch (LaunchOptions.StoreVersionName)
            {
                case "steam":
                    // This is detected separately
                    break;
                case "itch":
                    storeBuyLink = "https://revolutionarygames.itch.io/thrive";
                    break;
                default:
                    GD.PrintErr("Unknown store name for link: ", LaunchOptions.StoreVersionName);
                    break;
            }
        }

        if (!SteamHandler.Instance.IsLoaded)
        {
            storeLoggedInDisplay.Visible = false;

            itchButton.Visible = true;
            patreonButton.Visible = true;
        }
        else
        {
            storeLoggedInDisplay.Visible = true;
            UpdateSteamLoginText();

            // This is maybe unnecessary but this wasn't too difficult to add so this hiding logic is here
            itchButton.Visible = false;
            patreonButton.Visible = false;

            canShowThanks = true;
            storeBuyLink = "https://store.steampowered.com/app/1779200";
        }
    }

    private bool SteamFailed()
    {
        return SteamHandler.IsTaggedSteamRelease() && !SteamHandler.Instance.IsLoaded;
    }

    private void UpdateSteamLoginText()
    {
        storeLoggedInDisplay.Text = Localization.Translate("STORE_LOGGED_IN_AS")
            .FormatSafe(SteamHandler.Instance.DisplayName);
    }

    private void UpdateLauncherState()
    {
        if (!LaunchOptions.LaunchedThroughLauncher)
        {
            GD.Print("We are not started through the Thrive Launcher");
            exitToLauncherButton.Visible = false;
            return;
        }

        GD.Print("Thrive Launcher started us, launcher hidden: ", LaunchOptions.LaunchingLauncherIsHidden);

        // Exit to launcher button when the user might otherwise have trouble getting back there
        exitToLauncherButton.Visible = LaunchOptions.LaunchingLauncherIsHidden;
    }

    /// <summary>
    ///   Stops any currently playing animation and plays
    ///   the given one instead
    /// </summary>
    private void PlayGUIAnimation(string animation)
    {
        if (guiAnimations.IsPlaying())
            guiAnimations.Stop();

        guiAnimations.Play(animation);
    }

    /// <summary>
    ///   Switches the displayed menu
    /// </summary>
    private void SwitchMenu()
    {
        thriveLogo.Hide();

        // Hide other menus and only show the one of the current index
        foreach (var menu in menuArray!.OfType<Control>())
        {
            menu.Hide();

            if (menu.GetIndex() - NonMenuItemsFirst == CurrentMenuIndex)
            {
                menu.Show();
                thriveLogo.Show();
            }
        }
    }

    private void CheckModFailures()
    {
        var errors = ModLoader.Instance.GetAndClearModErrors();

        if (errors.Count > 0)
        {
            modLoadFailures.ExceptionInfo = string.Join("\n", errors);
            modLoadFailures.PopupCenteredShrink();
        }
    }

    private void OnIntroEnded()
    {
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, IsReturningToMenu ? 0.5f : 1.0f, null,
            false);

        // Start music after the video
        StartMusic();

        introVideoPassed = true;

        // Display menu buttons that were hidden to prevent them grabbing focus during intro video
        GetCurrentMenu()?.Show();

        // Load the menu background only here as the 3D ones are performance intensive so they aren't very nice to
        // consume power unnecessarily while showing the video
        RandomizeBackground();
    }

    private void CheckStartupSuccess()
    {
        if (SafeModeStartupHandler.StartedInSafeMode())
        {
            GD.Print("We started in safe mode");
            safeModeWarning.PopupCenteredShrink();
        }

        SafeModeStartupHandler.ReportGameStartSuccessful();
    }

    /// <summary>
    ///   Updates feed visibilities if settings have been changed
    /// </summary>
    private void UpdateFeedVisibilities()
    {
        var settings = Settings.Instance;

        if (!settings.ShowNewPatchNotes && patchNotesDisabler.Visible)
        {
            patchNotesDisabler.Visible = false;
            newsFeedDisabler.Visible = true;
        }
        else if (settings.ShowNewPatchNotes && !patchNotesDisabler.Visible)
        {
            ShowPatchInfoIfPossible();
        }
    }

    private void ShowPatchInfoIfPossible()
    {
        if (patchNotes.ShowIfNewPatchNotesExist())
        {
            GD.Print("We are playing a new version of Thrive for the first time, showing patch notes");

            // Hide the news when patch notes are visible (and there's something to show there)
            newsFeedDisabler.Visible = false;

            patchNotesDisabler.Visible = true;
        }
        else
        {
            patchNotesDisabler.Visible = false;
        }
    }

    private void WarnAboutNoEnabledMods()
    {
        if (!ModLoader.Instance.HasEnabledMods() && ModLoader.Instance.HasAvailableMods())
        {
            GD.Print("Player has installed mods but no enabled ones, giving a heads up");
            modsInstalledButNotEnabledWarning.PopupIfNotDismissed();
        }
    }

    private double TrackMenuPerformance()
    {
        var currentFrameRate = Engine.GetFramesPerSecond();

        // If this is the first tracked frame, do not use the average of the frame delta and 0
        if (averageFrameRate == 0)
            return currentFrameRate;

        // Not an exact average by any means, but good enough for this purpose
        return (averageFrameRate + currentFrameRate) / 2;
    }

    private void WarnAboutLowPerformance()
    {
        if (averageFrameRate < Constants.MAIN_MENU_LOW_PERFORMANCE_FPS &&
            secondsInMenu >= Constants.MAIN_MENU_LOW_PERFORMANCE_CHECK_AFTER && !AreAnyMenuPopupsOpen() &&
            !options.Visible)
        {
            GD.Print($"Average frame rate is {averageFrameRate}, prompting to disable 3D backgrounds");
            lowPerformanceWarning.PopupIfNotDismissed();
            canShowLowPerformanceWarning = false;
        }
    }

    private void OnLowPerformanceDialogConfirmed()
    {
        Settings.Instance.Menu3DBackgroundEnabled.Value = false;
        Settings.Instance.Save();
    }

    /// <summary>
    ///   True when any popup that appears in the menu is currently displayed.
    /// </summary>
    private bool AreAnyMenuPopupsOpen()
    {
        return openGlPopup.Visible || modLoadFailures.Visible || steamFailedPopup.Visible || safeModeWarning.Visible
            || modsInstalledButNotEnabledWarning.Visible || thanksDialog.Visible || lowPerformanceWarning.Visible;
    }

    private void NewGamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the options
        newGameSettings.OpenFromMainMenu();

        thriveLogo.Hide();
    }

    private void ToolsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SetCurrentMenu(1);
    }

    private void ExtrasPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SetCurrentMenu(2);
    }

    private void FreebuildEditorPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Disable the button to prevent it being executed again.
        freebuildButton.Disabled = true;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, () =>
        {
            OnEnteringGame();

            // Instantiate a new editor scene
            var editor = (MicrobeEditor)SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instantiate();

            // Start freebuild game
            editor.CurrentGame = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings(), true);

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
    }

    private void MulticellularFreebuildEditorPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Disable the button to prevent it being executed again.
        multicellularFreebuildButton.Disabled = true;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, () =>
        {
            OnEnteringGame();

            // Instantiate a new editor scene
            var editor = (EarlyMulticellularEditor)SceneManager.Instance
                .LoadScene(MainGameState.EarlyMulticellularEditor).Instantiate();

            // Start freebuild game
            editor.CurrentGame = GameProperties.StartNewEarlyMulticellularGame(new WorldGenerationSettings(), true);

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
    }

    private void OnAutoEvoExploringPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        autoEvoExploringButton.Disabled = true;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f,
            () => { SceneManager.Instance.SwitchToScene("res://src/auto-evo/AutoEvoExploringTool.tscn"); }, false);
    }

    // TODO: this is now used by another sub menu as well so renaming this to be more generic would be good
    private void BackFromToolsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SetCurrentMenu(0);
    }

    private void ViewSourceCodePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        OS.ShellOpen("https://github.com/Revolutionary-Games/Thrive");
    }

    private void QuitPressed()
    {
        SceneManager.Instance.QuitThrive();
    }

    private void QuitToLauncherPressed()
    {
        GD.Print("Exit to launcher pressed");

        // Output a special message which the launcher should detect
        GD.Print(ThriveLauncherSharedConstants.REQUEST_LAUNCHER_OPEN);

        // To make sure this always works even with buffering, output this as an error
        GD.PrintErr("Printing request as \"error\" to ensure it isn't buffered:");
        GD.PrintErr(ThriveLauncherSharedConstants.REQUEST_LAUNCHER_OPEN);

        // Probably unnecessary, but we exit with a delay here
        Invoke.Instance.Queue(QuitPressed);
    }

    private void OptionsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the options
        options.OpenFromMainMenu();
    }

    private void OnReturnFromOptions()
    {
        options.Visible = false;
        SetCurrentMenu(0, false);

        // In case news settings are changed, update that state
        UpdateFeedVisibilities();
        newsFeed.CheckStartFetchNews();
    }

    private void OnReturnFromNewGameSettings()
    {
        newGameSettings.Visible = false;

        SetCurrentMenu(0, false);

        thriveLogo.Show();
    }

    private void OnRedirectedToOptionsMenuFromNewGameSettings()
    {
        OnReturnFromNewGameSettings();
        OptionsPressed();
        options.SelectOptionsTab(OptionsMenu.OptionsTab.Performance);
    }

    private void OnReturnFromThriveopedia()
    {
        thriveopedia.Visible = false;
        SetCurrentMenu(0, false);
    }

    private void LoadGamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the options
        saves.Visible = true;
    }

    private void OnReturnFromLoadGame()
    {
        saves.Visible = false;
        SetCurrentMenu(0, false);
    }

    private void CreditsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the credits view
        credits.Restart();
        creditsContainer.Visible = true;
    }

    private void OnReturnFromCredits()
    {
        creditsContainer.Visible = false;
        credits.Pause();

        SetCurrentMenu(0, false);
    }

    private void ThriveopediaPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the Thriveopedia
        thriveopedia.OpenFromMainMenu();
    }

    private void VisitSuggestionsSitePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        OS.ShellOpen("https://suggestions.revolutionarygamesstudio.com/");
    }

    private void LicensesPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the licenses view
        licensesDisplay.PopupCenteredShrink();
    }

    private void OnReturnFromLicenses()
    {
        SetCurrentMenu(2, false);
    }

    private void ModsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the mods view
        modManager.Visible = true;
    }

    private void OnReturnFromMods()
    {
        modManager.Visible = false;
        SetCurrentMenu(0, false);
    }

    private void ArtGalleryPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SetCurrentMenu(uint.MaxValue, false);
        galleryViewer.OpenFullRect();
        Jukebox.Instance.PlayCategory("ArtGallery");

        if (created3DBackground != null)
        {
            // Hide the 3D background while in the gallery as it is a fullscreen popup and rendering the expensive 3D
            // scene underneath it is not the best
            created3DBackground.Visible = false;
        }
    }

    private void OnReturnFromArtGallery()
    {
        SetCurrentMenu(2, false);
        Jukebox.Instance.PlayCategory("Menu");

        if (created3DBackground != null)
        {
            created3DBackground.Visible = true;
        }

        ResetPerformanceTracking();
    }

    private void OnWebsitesButtonPressed()
    {
        websiteButtonsContainer.OpenModal();
    }

    private void OnSocialMediaButtonPressed(string url)
    {
        GD.Print($"Opening social link: {url}");
        OS.ShellOpen(url);
    }

    private void BenchmarksPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetCurrentMenu(3, true);
    }

    private void OnReturnFromBenchmarks()
    {
        GUICommon.Instance.PlayButtonPressSound();

        SetCurrentMenu(1, true);
    }

    private void MicrobeBenchmarkPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        microbeBenchmarkButton.Disabled = true;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f,
            () => { SceneManager.Instance.SwitchToScene("res://src/benchmark/microbe/MicrobeBenchmark.tscn"); }, false);
    }

    private void OnNewGameIntroVideoStarted()
    {
        if (created3DBackground != null)
        {
            // Hide the background again when playing a video as the 3D backgrounds are performance intensive
            created3DBackground.Visible = false;
        }
    }

    private void OnThriveopediaOpened(string pageName)
    {
        thriveopedia.OpenFromMainMenu();
        thriveopedia.ChangePage(pageName);
    }

    private void ResetPerformanceTracking()
    {
        secondsInMenu = 0;
        averageFrameRate = 0;
    }

    private void OnTranslationsChanged()
    {
        if (SteamHandler.Instance.IsLoaded)
        {
            UpdateSteamLoginText();
        }
    }
}
