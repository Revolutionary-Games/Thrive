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

    /// <summary>
    ///   Reference to the instanced cutscene node.
    /// </summary>
    /// <remarks>Set this to null after use.</remarks>
    private Cutscene cutscene;

    /// <summary>
    ///   Reference to the instanced screen fade node.
    /// </summary>
    /// <remarks>Set this to null after use.</remarks>
    private Fade screenFade;

    public override void _Ready()
    {
        GetViewport().Connect("size_changed", this, "OnCutsceneResized");

        // Start intro video
        PlayCutscene("res://assets/videos/intro.webm", "RunMenuSetup");
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsKeyPressed((int)Godot.KeyList.Escape))
        {
            CancelCutscene();
        }
    }

    /// <summary>
    ///   Setup the main menu.
    /// </summary>
    public void RunMenuSetup()
    {
        // Remove the instantiated intro cutscene
        cutscene = null;

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
        int num = rand.Next() % 9;

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
    ///   Helper method for fading to black.
    ///   Calls a method when finished.
    /// </summary>
    public void FadeIn(string onFinishedMethod, float fadeDuration)
    {
        var scene = GD.Load<PackedScene>("res://scripts/gui/Fade.tscn");

        // Instantiate scene
        screenFade = (Fade)scene.Instance();
        AddChild(screenFade);

        screenFade.FadeToBlack(fadeDuration);

        screenFade.Connect("FadeFinished", this, onFinishedMethod);
    }

    /// <summary>
    ///   Helper method for playing a video stream.
    ///   Calls a method when finished.
    /// </summary>
    public void PlayCutscene(string path, string onFinishedMethod)
    {
        var scene = GD.Load<PackedScene>("res://scripts/gui/Cutscene.tscn");

        if (scene == null)
        {
            GD.PrintErr("Failed to load the cutscene player scene");
            return;
        }

        // Instantiate scene
        cutscene = (Cutscene)scene.Instance();
        AddChild(cutscene);

        var stream = GD.Load<VideoStream>(path);

        // Play the video stream
        cutscene.CutsceneVideoPlayer.Stream = stream;
        cutscene.CutsceneVideoPlayer.Play();

        // Connect finished signal
        cutscene.Connect("CutsceneFinished", this, onFinishedMethod);

        // Initially adjust video player frame size
        OnCutsceneResized();
    }

    /// <summary>
    ///   Keeps aspect ratio of the cutscene whenever
    ///   the window is being resized.
    /// </summary>
    public void OnCutsceneResized()
    {
        if (cutscene == null)
            return;

        var currentSize = OS.WindowSize;

        // Scaling factors
        var scaleHeight = currentSize.x / cutscene.FrameSize.x;
        var scaleWidth = currentSize.y / cutscene.FrameSize.y;

        var scale = Math.Min(scaleHeight, scaleWidth);

        var newSize = new Vector2(cutscene.FrameSize.x * scale,
            cutscene.FrameSize.y * scale);

        // Adjust the cutscene size and center it
        cutscene.CutsceneVideoPlayer.SetSize(newSize);
        cutscene.CutsceneVideoPlayer.SetAnchorsAndMarginsPreset(
            Control.LayoutPreset.Center, Control.LayoutPresetMode.KeepSize);
    }

    /// <summary>
    ///   Skips the current playing cutscene.
    /// </summary>
    public void CancelCutscene()
    {
        // Also skips the fade sequence if there is any
        if (cutscene == null && screenFade != null)
        {
            screenFade.OnTweenCompleted();
        }

        if (cutscene != null && screenFade == null)
        {
            cutscene.OnStreamFinished();
        }
    }

    /// <summary>
    ///   Change the menu displayed on screen to one
    ///   with the menu of the given index.
    /// </summary>
    public void SetCurrentMenu(uint index, bool slide = true)
    {
        // Using tween for value interpolation
        var tween = GetNode<Tween>("MenuContainers/MenuTween");

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
        screenFade = null;

        // Start microbe intro
        PlayCutscene("res://assets/videos/microbe_intro2.webm", "OnMicrobeIntroEnded");
    }

    public void OnMicrobeIntroEnded()
    {
        cutscene = null;

        // Change the current scene to microbe stage
        // TODO: Add loading screen while changing between scenes
        GetTree().ChangeScene("res://src/microbe_stage/MicrobeStage.tscn");
    }

    public void NewGamePressed()
    {
        PlayButtonPressSound();
        MusicAudio.Stop();
        FadeIn("OnNewGameFadeFinished", 0.8f);
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
