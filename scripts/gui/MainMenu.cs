using System;
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

    public Godot.Collections.Array MenuArray;
    public TextureRect Background;

    private OptionsMenu options;

    public override void _Ready()
    {
        RunMenuSetup();

        // Start intro video
        if (Settings.Instance.PlayIntroVideo)
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
        int num = rand.Next(0, 10);

        if (num <= 3)
        {
            SetBackground("res://assets/textures/gui/BG_Menu01.png");
        }
        else if (num <= 6)
        {
            SetBackground("res://assets/textures/gui/BG_Menu02.png");
        }
        else if (num <= 9)
        {
            SetBackground("res://assets/textures/gui/BG_Menu03.png");
        }
    }

    private void SetBackground(string filepath)
    {
        if (Background == null)
        {
            GD.PrintErr("Background object doesn't exist");
            return;
        }

        var backgroundImage = GD.Load<Texture>(filepath);
        Background.Texture = backgroundImage;
    }

    /// <summary>
    ///   Change the menu displayed on screen to one
    ///   with the menu of the given index.
    /// </summary>
    private void SetCurrentMenu(uint index, bool slide = true)
    {
        // Using tween for value interpolation
        var tween = GetNode<Tween>("MenuTween");

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
                // TODO: Improve how this is done
                if (slide)
                {
                    tween.InterpolateProperty(menu, "custom_constants/separation", -35,
                        10, 0.3f, Tween.TransitionType.Sine);
                    tween.Start();
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
        // Instantiate a new microbe stage scene
        // TODO: Add loading screen while changing between scenes
        GetTree().ChangeScene("res://src/microbe_stage/MicrobeStage.tscn");
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

        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.5f, false);
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
    }

    private void OnReturnFromOptions()
    {
        options.Visible = false;

        // Hide all the other menus
        SetCurrentMenu(0);
    }
}
