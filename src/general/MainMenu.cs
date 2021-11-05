using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    public NodePath ThriveLogoPath;

    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "Set from editor")]
    [Export]
    public List<Texture> MenuBackgrounds;

    [Export]
    public NodePath NewGameButtonPath;

    [Export]
    public NodePath FreebuildButtonPath;

    [Export]
    public NodePath CreditsContainerPath;

    [Export]
    public NodePath CreditsScrollPath;

    [Export]
    public NodePath LicensesDisplayPath;

    [Export]
    public NodePath GLES2PopupPath;

    public Array MenuArray;
    public TextureRect Background;

    public bool IsReturningToMenu;

    private TextureRect thriveLogo;
    private OptionsMenu options;
    private AnimationPlayer guiAnimations;
    private SaveManagerGUI saves;

    private Control creditsContainer;
    private CreditsScroll credits;
    private Control licensesDisplay;

    private Button newGameButton;
    private Button freebuildButton;

    private CustomConfirmationDialog gles2Popup;

    public override void _Ready()
    {
        RunMenuSetup();

        // Start intro video
        if (Settings.Instance.PlayIntroVideo && !IsReturningToMenu)
        {
            TransitionManager.Instance.AddCutscene("res://assets/videos/intro.webm");
            TransitionManager.Instance.StartTransitions(this, nameof(OnIntroEnded));
        }
        else
        {
            OnIntroEnded();
        }

        // Let all suppressed deletions happen (if we came back directly from the editor that was loaded from a save)
        TemporaryLoadedNodeDeleter.Instance.ReleaseAllHolds();
    }

    public void StartMusic()
    {
        Jukebox.Instance.PlayCategory("Menu");
    }

    /// <summary>
    ///   Sets the current menu index and then switches the menu
    /// </summary>
    /// <param name="index">Index of the menu</param>
    /// <param name="slide">If false then the menu slide animation will not be played</param>
    public void SetCurrentMenu(uint index, bool slide = true)
    {
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
        newGameButton = GetNode<Button>(NewGameButtonPath);
        freebuildButton = GetNode<Button>(FreebuildButtonPath);
        creditsContainer = GetNode<Control>(CreditsContainerPath);
        credits = GetNode<CreditsScroll>(CreditsScrollPath);
        licensesDisplay = GetNode<Control>(LicensesDisplayPath);

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
        saves = GetNode<SaveManagerGUI>("SaveManagerGUI");
        gles2Popup = GetNode<CustomConfirmationDialog>(GLES2PopupPath);

        // Set initial menu
        SwitchMenu();

        // Easter egg message
        thriveLogo.RegisterToolTipForControl("thriveLogoEasterEgg", "mainMenu");

        if (OS.GetCurrentVideoDriver() == OS.VideoDriver.Gles2 && !IsReturningToMenu)
            gles2Popup.PopupCenteredShrink();
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
        // Hide other menus and only show the one of the current index
        foreach (Control menu in MenuArray)
        {
            menu.Hide();

            if (menu.GetIndex() == CurrentMenuIndex)
            {
                menu.Show();
            }
        }
    }

    private void OnIntroEnded()
    {
        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeIn, IsReturningToMenu ?
            0.3f :
            0.5f, false);
        TransitionManager.Instance.StartTransitions(null);

        // Start music after the video
        StartMusic();
    }

    private void OnMicrobeIntroEnded()
    {
        OnEnteringGame();

        // TODO: Add loading screen while changing between scenes
        SceneManager.Instance.SwitchToScene(MainGameState.MicrobeStage);
    }

    private void OnFreebuildFadeInEnded()
    {
        OnEnteringGame();

        // Instantiate a new editor scene
        var editor = (MicrobeEditor)SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instance();

        // Start freebuild game
        editor.CurrentGame = GameProperties.StartNewMicrobeGame(true);

        // Switch to the editor scene
        SceneManager.Instance.SwitchToScene(editor);
    }

    private void NewGamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Disable the button to prevent it being executed again.
        newGameButton.Disabled = true;

        // Stop music for the video (stop is used instead of pause to stop the menu music playing a bit after the video
        // before the stage music starts)
        Jukebox.Instance.Stop();

        if (Settings.Instance.PlayMicrobeIntroVideo)
        {
            TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.5f);
            TransitionManager.Instance.AddCutscene("res://assets/videos/microbe_intro2.webm");
        }
        else
        {
            // People who disable the cutscene are impatient anyway so use a reduced fade time
            TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.2f);
        }

        TransitionManager.Instance.StartTransitions(this, nameof(OnMicrobeIntroEnded));
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

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.15f, false);
        TransitionManager.Instance.StartTransitions(this, nameof(OnFreebuildFadeInEnded));
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
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Quit();
    }

    private void OptionsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the options
        options.OpenFromMainMenu();

        thriveLogo.Hide();
    }

    private void OnReturnFromOptions()
    {
        options.Visible = false;

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

        thriveLogo.Hide();
    }

    private void OnReturnFromLoadGame()
    {
        saves.Visible = false;

        SetCurrentMenu(0, false);

        thriveLogo.Show();
    }

    private void CreditsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the credits view
        credits.Restart();
        creditsContainer.Visible = true;

        thriveLogo.Hide();
    }

    private void OnReturnFromCredits()
    {
        creditsContainer.Visible = false;
        credits.Pause();

        SetCurrentMenu(0, false);

        thriveLogo.Show();
    }

    private void LicensesPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Hide all the other menus
        SetCurrentMenu(uint.MaxValue, false);

        // Show the licenses view
        licensesDisplay.Visible = true;

        thriveLogo.Hide();
    }

    private void OnReturnFromLicenses()
    {
        licensesDisplay.Visible = false;

        SetCurrentMenu(0, false);

        thriveLogo.Show();
    }

    private void OnEnteringGame()
    {
        CheatManager.OnCheatsDisabled();
        SaveHelper.ClearLastSaveTime();
    }
}
