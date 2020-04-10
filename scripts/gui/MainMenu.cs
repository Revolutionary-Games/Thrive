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

    public AudioStreamPlayer MusicAudio;

    private GUICommon guiCommon;

    public override void _Ready()
    {
        guiCommon = GetNode<GUICommon>("/root/GUICommon");

        // Start intro video
        guiCommon.PlayCutscene("res://assets/videos/intro.webm", this,
            "OnIntroEnded", true);

        RunMenuSetup();
    }

    /// <summary>
    ///   Setup the main menu.
    /// </summary>
    public void RunMenuSetup()
    {
        Background = GetNode<TextureRect>("Background");
        MusicAudio = GetNode<AudioStreamPlayer>("Music");

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

        // Set initial menu to the current menu index
        SetCurrentMenu(CurrentMenuIndex, false);
    }

    /// <summary>
    ///   Randomizes background images.
    /// </summary>
    public void RandomizeBackground()
    {
        Random rand = new Random();
        int num = rand.Next(0, 9);

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

    public void SetBackground(string filepath)
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
    public void SetCurrentMenu(uint index, bool slide = true)
    {
        // Using tween for value interpolation
        var tween = GetNode<Tween>("MenuTween");

        if (index > MenuArray.Count - 1)
        {
            GD.PrintErr("Selected menu index is out of range!");
            return;
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

        CurrentMenuIndex = index;
    }

    private void OnIntroEnded()
    {
        guiCommon.Fade(GUICommon.FadeType.FadeOut, null, string.Empty, 0.5f, false);

        // Play the menu music
        MusicAudio.Play();
    }

    private void OnNewGameFadeFinished()
    {
        // Start microbe intro
        guiCommon.PlayCutscene("res://assets/videos/microbe_intro2.webm", this,
            "OnMicrobeIntroEnded", true);
    }

    private void OnMicrobeIntroEnded()
    {
        // Instantiate a new microbe stage scene
        // TODO: Add loading screen while changing between scenes
        GetTree().ChangeScene("res://src/microbe_stage/MicrobeStage.tscn");
    }

    private void NewGamePressed()
    {
        guiCommon.PlayButtonPressSound();

        var tween = GetNode<Tween>("MenuTween");

        if (MusicAudio.Playing)
        {
            // Tween music volume down to 0, in this case -80 decibles
            tween.InterpolateProperty(MusicAudio, "volume_db", null, -80, 1.5f,
                Tween.TransitionType.Linear, Tween.EaseType.In);
            tween.Start();
        }

        // Start fade
        guiCommon.Fade(GUICommon.FadeType.FadeIn, this,
            "OnNewGameFadeFinished", 0.5f, true);
    }

    private void ToolsPressed()
    {
        guiCommon.PlayButtonPressSound();
        SetCurrentMenu(1);
    }

    private void FreebuildEditorPressed()
    {
        guiCommon.PlayButtonPressSound();

        // Instantiate a new editor scene
        GetTree().ChangeScene("res://src/microbe_stage/editor/MicrobeEditor.tscn");
    }

    private void BackFromToolsPressed()
    {
        guiCommon.PlayButtonPressSound();
        SetCurrentMenu(0);
    }

    private void QuitPressed()
    {
        guiCommon.PlayButtonPressSound();
        GetTree().Quit();
    }
}
