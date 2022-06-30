﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   Class managing the main menu and everything in it
/// </summary>
public class MainMenu : NodeWithInput
{
    /// <summary>
    ///   Index of the current menu.
    /// </summary>
    [Export]
    public uint CurrentMenuIndex;

    [Export]
    public NodePath ThriveLogoPath = null!;

    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "Set from editor")]
    [Export]
    public List<Texture> MenuBackgrounds = null!;

    [Export]
    public NodePath FreebuildButtonPath = null!;

    [Export]
    public NodePath CreditsContainerPath = null!;

    [Export]
    public NodePath CreditsScrollPath = null!;

    [Export]
    public NodePath LicensesDisplayPath = null!;

    [Export]
    public NodePath GLES2PopupPath = null!;

    [Export]
    public NodePath ModLoadFailuresPath = null!;

    [Export]
    public NodePath StoreLoggedInDisplayPath = null!;

    [Export]
    public NodePath ModManagerPath = null!;

    [Export]
    public NodePath GalleryViewerPath = null!;

    public Array? MenuArray;
    public TextureRect Background = null!;

    public bool IsReturningToMenu;

    private TextureRect thriveLogo = null!;
    private OptionsMenu options = null!;
    private NewGameSettings newGameSettings = null!;
    private AnimationPlayer guiAnimations = null!;
    private SaveManagerGUI saves = null!;
    private ModManager modManager = null!;
    private GalleryViewer galleryViewer = null!;

    private Control creditsContainer = null!;
    private CreditsScroll credits = null!;
    private LicensesDisplay licensesDisplay = null!;
    private Button freebuildButton = null!;

    private Label storeLoggedInDisplay = null!;

    private CustomConfirmationDialog gles2Popup = null!;
    private ErrorDialog modLoadFailures = null!;

    public static void OnEnteringGame()
    {
        CheatManager.OnCheatsDisabled();
        SaveHelper.ClearLastSaveTime();
    }

    public override void _Ready()
    {
        // Unpause the game as the MainMenu should never be paused.
        PauseManager.Instance.ForceClear();

        RunMenuSetup();

        // Start intro video
        if (Settings.Instance.PlayIntroVideo && LaunchOptions.VideosEnabled && !IsReturningToMenu)
        {
            TransitionManager.Instance.AddSequence(
                TransitionManager.Instance.CreateCutscene("res://assets/videos/intro.ogv", 0.65f), OnIntroEnded);
        }
        else
        {
            OnIntroEnded();
        }

        // Let all suppressed deletions happen (if we came back directly from the editor that was loaded from a save)
        TemporaryLoadedNodeDeleter.Instance.ReleaseAllHolds();

        CheckModFailures();
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
        if (MenuArray == null)
            throw new InvalidOperationException("Main menu has not been initialized");

        // Allow disabling all the menus for going to the options menu
        if (index > MenuArray.Count - 1 && index != uint.MaxValue)
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

    /// <summary>
    ///   Setup the main menu.
    /// </summary>
    private void RunMenuSetup()
    {
        Background = GetNode<TextureRect>("Background");
        guiAnimations = GetNode<AnimationPlayer>("GUIAnimations");
        thriveLogo = GetNode<TextureRect>(ThriveLogoPath);
        freebuildButton = GetNode<Button>(FreebuildButtonPath);
        creditsContainer = GetNode<Control>(CreditsContainerPath);
        credits = GetNode<CreditsScroll>(CreditsScrollPath);
        licensesDisplay = GetNode<LicensesDisplay>(LicensesDisplayPath);
        storeLoggedInDisplay = GetNode<Label>(StoreLoggedInDisplayPath);
        modManager = GetNode<ModManager>(ModManagerPath);
        galleryViewer = GetNode<GalleryViewer>(GalleryViewerPath);

        MenuArray?.Clear();

        // Get all of menu items
        MenuArray = GetTree().GetNodesInGroup("MenuItem");

        if (MenuArray == null)
        {
            GD.PrintErr("Failed to find all the menu items!");
            return;
        }

        RandomizeBackground();

        options = GetNode<OptionsMenu>("OptionsMenu");
        newGameSettings = GetNode<NewGameSettings>("NewGameSettings");
        saves = GetNode<SaveManagerGUI>("SaveManagerGUI");
        gles2Popup = GetNode<CustomConfirmationDialog>(GLES2PopupPath);
        modLoadFailures = GetNode<ErrorDialog>(ModLoadFailuresPath);

        // Set initial menu
        SwitchMenu();

        // Easter egg message
        thriveLogo.RegisterToolTipForControl("thriveLogoEasterEgg", "mainMenu");

        if (OS.GetCurrentVideoDriver() == OS.VideoDriver.Gles2 && !IsReturningToMenu)
            gles2Popup.PopupCenteredShrink();

        UpdateStoreNameLabel();
    }

    /// <summary>
    ///   Randomizes background images.
    /// </summary>
    private void RandomizeBackground()
    {
        Random rand = new Random();

        var chosenBackground = MenuBackgrounds.Random(rand);

        SetBackground(chosenBackground);
    }

    private void SetBackground(Texture backgroundImage)
    {
        Background.Texture = backgroundImage;
    }

    private void UpdateStoreNameLabel()
    {
        if (!SteamHandler.Instance.IsLoaded)
        {
            storeLoggedInDisplay.Visible = false;
        }
        else
        {
            storeLoggedInDisplay.Visible = true;
            storeLoggedInDisplay.Text = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("STORE_LOGGED_IN_AS"), SteamHandler.Instance.DisplayName);
        }
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
        foreach (Control menu in MenuArray!)
        {
            menu.Hide();

            if (menu.GetIndex() == CurrentMenuIndex)
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
        TransitionManager.Instance.AddSequence(
            ScreenFade.FadeType.FadeIn, IsReturningToMenu ? 0.5f : 1.0f, null, false);

        // Start music after the video
        StartMusic();
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
            var editor = (MicrobeEditor)SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instance();

            // Start freebuild game
            editor.CurrentGame = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings(), true);

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
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
        GetTree().Quit();
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
    }

    private void OnReturnFromNewGameSettings()
    {
        newGameSettings.Visible = false;

        SetCurrentMenu(0, false);

        thriveLogo.Show();
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
        galleryViewer.PopupFullRect();
        Jukebox.Instance.PlayCategory("ArtGallery");
    }

    private void OnReturnFromArtGallery()
    {
        SetCurrentMenu(2, false);
        Jukebox.Instance.PlayCategory("Menu");
    }
}
