using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Class managing the main menu and everything in it
/// </summary>
public class MainMenu : Node
{
    /// <summary>
    ///   Index of the current menu.
    /// </summary>
    [Export]
    public uint CurrentMenuIndex;

    [Export]
    public NodePath ThriveLogoPath;

    [Export]
    public List<Texture> MenuBackgrounds;

    public Godot.Collections.Array MenuArray;
    public TextureRect Background;

    public bool IsReturningToMenu = false;

    private TextureRect thriveLogo;
    private OptionsMenu options;
    private AnimationPlayer GUIAnimations;

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
    }

    public void StartMusic()
    {
        Jukebox.Instance.PlayingCategory = "Menu";
        Jukebox.Instance.Resume();
    }

    /// <summary>
    ///   Setup the main menu.
    /// </summary>
    private void RunMenuSetup()
    {
        Background = GetNode<TextureRect>("Background");
        GUIAnimations = GetNode<AnimationPlayer>("GUIAnimations");
        thriveLogo = GetNode<TextureRect>(ThriveLogoPath);

        if (MenuArray != null)
            MenuArray.Clear();

        // Get all of menu items
        MenuArray = GetTree().GetNodesInGroup("MenuItem");

        if (MenuArray == null)
        {
            GD.PrintErr("Failed to find all the menu items!");
            return;
        }

        RandomizeBackground();

        options = GetNode<OptionsMenu>("OptionsMenu");

        // Load settings
        options.SetSettingsFrom(Settings.Instance);

        // Set initial menu to the current menu index
        SetCurrentMenu(CurrentMenuIndex, false);
    }

    /// <summary>
    ///   Randomizes background images.
    /// </summary>
    private void RandomizeBackground()
    {
        Random rand = new Random();

        // Exported lists crashes the game, so as a workaround ToList() is added
        // https://github.com/godotengine/godot/issues/37934
        // This is a Godot issue that may get fixed in 4.0
        var chosenBackground = MenuBackgrounds.ToList().Random(rand);

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
        if (GUIAnimations.IsPlaying())
            GUIAnimations.Stop();

        GUIAnimations.Play(animation);
    }

    /// <summary>
    ///   Change the menu displayed on screen to one
    ///   with the menu of the given index.
    /// </summary>
    private void SetCurrentMenu(uint index, bool slide = true)
    {
        // Allow disabling all the menus for going to the options menu
        if (index > MenuArray.Count - 1 && index != uint.MaxValue)
        {
            GD.PrintErr("Selected menu index is out of range!");
            return;
        }
        else
        {
            CurrentMenuIndex = index;
        }

        // Hide all menu and only show the one
        // with the correct index
        foreach (Control menu in MenuArray)
        {
            menu.Hide();

            if (menu.GetIndex() == index)
            {
                menu.Show();

                // Play the slide down animation
                if (slide)
                {
                    PlayGUIAnimation("MenuSlideDown");
                }
            }
        }
    }

    private void OnIntroEnded()
    {
        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeOut, 0.5f, false);
        TransitionManager.Instance.StartTransitions(null, string.Empty);

        // Start music after the video
        StartMusic();
    }

    private void OnMicrobeIntroEnded()
    {
        // TODO: Add loading screen while changing between scenes

        var scene = GD.Load<PackedScene>("res://src/microbe_stage/MicrobeStage.tscn");

        // Instantiate a new microbe stage scene
        var stage = (MicrobeStage)scene.Instance();

        var parent = GetParent();
        parent.RemoveChild(this);
        parent.AddChild(stage);
    }

    private void OnFreebuildFadeInEnded()
    {
        // Instantiate a new editor scene
        var scene = GD.Load<PackedScene>("res://src/microbe_stage/editor/MicrobeEditor.tscn");

        var editor = (MicrobeEditor)scene.Instance();

        // Start freebuild game
        editor.CurrentGame = GameProperties.StartNewMicrobeGame(true);

        // Switch to the editor scene
        var parent = GetParent();
        parent.RemoveChild(this);
        parent.AddChild(editor);
    }

    private void NewGamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Stop music for the video (stop is used instead of pause to stop the menu music playing a bit after the video
        // before the stage music starts)
        Jukebox.Instance.Stop();

        if (Settings.Instance.PlayMicrobeIntroVideo)
        {
            TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.5f);
            TransitionManager.Instance.AddCutscene("res://assets/videos/microbe_intro2.webm");
        }
        else
        {
            // People who disable the cutscene are impatient anyway so use a reduced fade time
            TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.2f);
        }

        TransitionManager.Instance.StartTransitions(this, nameof(OnMicrobeIntroEnded));
    }

    private void ToolsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SetCurrentMenu(1);
    }

    private void FreebuildEditorPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.3f, false);
        TransitionManager.Instance.StartTransitions(this, nameof(OnFreebuildFadeInEnded));
    }

    private void BackFromToolsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SetCurrentMenu(0);
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
        SetCurrentMenu(uint.MaxValue);

        // Show the options
        options.Visible = true;

        thriveLogo.Hide();
    }

    private void OnReturnFromOptions()
    {
        options.Visible = false;

        // Hide all the other menus
        SetCurrentMenu(0);

        thriveLogo.Show();
    }
}
