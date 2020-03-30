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
    public AudioStreamPlayer GUIAudio;

    public override void _Ready()
    {
        // Start intro video
        PlayCutscene("res://assets/videos/intro.webm", "RunMenuSetup", true);
    }

    /// <summary>
    ///   Setup the main menu.
    /// </summary>
    public void RunMenuSetup()
    {
        Fade(1, string.Empty, 1.5f, false);

        if (HasNode("Background"))
            Background = GetNode<TextureRect>("Background");

        if (HasNode("GUIAudio"))
            GUIAudio = GetNode<AudioStreamPlayer>("GUIAudio");

        if (HasNode("Music"))
            MusicAudio = GetNode<AudioStreamPlayer>("Music");

        // Play the menu music
        MusicAudio.Play();

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
    ///   Plays the button click sound effect.
    /// </summary>
    public void PlayButtonPressSound()
    {
        var sound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");

        GUIAudio.Stream = sound;
        GUIAudio.Play();
    }

    /// <summary>
    ///   Helper function for fading to black.
    ///   Calls a function when finished.
    /// </summary>
    /// <param name="transition">
    ///   Set 0 for fading to black, and 1 for fading to white.
    /// </param>
    public void Fade(int transition, string onFinishedMethod,
        float fadeDuration, bool allowSkipping)
    {
        var scene = GD.Load<PackedScene>("res://scripts/gui/Fade.tscn");

        // Instantiate scene
        var screenFade = (Fade)scene.Instance();
        AddChild(screenFade);

        screenFade.AllowSkipping = allowSkipping;

        if (transition == 0)
        {
            screenFade.FadeToBlack(fadeDuration);
        }
        else if (transition == 1)
        {
            screenFade.FadeToWhite(fadeDuration);
        }

        if (onFinishedMethod != string.Empty)
            screenFade.Connect("FadeFinished", this, onFinishedMethod);
    }

    /// <summary>
    ///   Helper function for playing a video stream.
    ///   Calls a function when finished.
    /// </summary>
    public void PlayCutscene(string path, string onFinishedMethod, bool allowSkipping)
    {
        var scene = GD.Load<PackedScene>("res://scripts/gui/Cutscene.tscn");

        if (scene == null)
        {
            GD.PrintErr("Failed to load the cutscene player scene");
            return;
        }

        // Instantiate scene
        var cutscene = (Cutscene)scene.Instance();
        AddChild(cutscene);

        cutscene.AllowSkipping = allowSkipping;

        var stream = GD.Load<VideoStream>(path);

        // Play the video stream
        cutscene.CutsceneVideoPlayer.Stream = stream;
        cutscene.CutsceneVideoPlayer.Play();

        // Connect finished signal
        if (onFinishedMethod != string.Empty)
            cutscene.Connect("CutsceneFinished", this, onFinishedMethod);

        // Initially adjust video player frame size
        cutscene.OnCutsceneResized();
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

    public void OnNewGameFadeFinished()
    {
        // Start microbe intro
        PlayCutscene("res://assets/videos/microbe_intro2.webm", "OnMicrobeIntroEnded",
            true);
    }

    public void OnMicrobeIntroEnded()
    {
        // Change the current scene to microbe stage
        // TODO: Add loading screen while changing between scenes
        GetTree().ChangeScene("res://src/microbe_stage/MicrobeStage.tscn");
    }

    public void NewGamePressed()
    {
        PlayButtonPressSound();

        var tween = GetNode<Tween>("MenuTween");

        if (MusicAudio.Playing)
        {
            // Tween music volume down to 0, in this case -80 decibles
            tween.InterpolateProperty(MusicAudio, "volume_db", null, -80, 1.5f,
                Tween.TransitionType.Linear, Tween.EaseType.In);
            tween.Start();
        }

        // Start fade
        Fade(0, "OnNewGameFadeFinished", 0.8f, true);
    }

    public void ToolsPressed()
    {
        PlayButtonPressSound();
        SetCurrentMenu(1);
    }

    public void FreebuildEditorPressed()
    {
        PlayButtonPressSound();
    }

    public void BackFromToolsPressed()
    {
        PlayButtonPressSound();
        SetCurrentMenu(0);
    }

    public void QuitPressed()
    {
        PlayButtonPressSound();
        GetTree().Quit();
    }
}
