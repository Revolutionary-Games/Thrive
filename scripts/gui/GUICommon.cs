using Godot;

/// <summary>
///   Common helpers for the GUI to work with.
///   This singleton class is placed on AutoLoad.
/// </summary>
public class GUICommon : Node
{
    public AudioStreamPlayer UiAudio;

    private AudioStream buttonPressSound;

    public enum FadeType
    {
        FadeIn,
        FadeOut,
    }

    public override void _Ready()
    {
        UiAudio = new AudioStreamPlayer();
        AddChild(UiAudio);

        // Keep running the audio player process when the game paused
        UiAudio.PauseMode = PauseModeEnum.Process;

        buttonPressSound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
    }

    /// <summary>
    ///   Play the button click sound effect
    ///   when a button is pressed.
    /// </summary>
    public void PlayButtonPressSound()
    {
        UiAudio.Stream = buttonPressSound;
        UiAudio.Play();
    }

    /// <summary>
    ///   Helper function for screen fading.
    ///   Calls a function when finished.
    /// </summary>
    /// <param name="transition">
    ///   The FadeType enum.
    /// </param>
    public void Fade(FadeType transition, Godot.Object target, string onFinishedMethod,
        float fadeDuration, bool allowSkipping)
    {
        var scene = GD.Load<PackedScene>("res://scripts/gui/Fade.tscn");

        // Instantiate scene
        var screenFade = (Fade)scene.Instance();
        AddChild(screenFade);

        screenFade.AllowSkipping = allowSkipping;

        if (transition == FadeType.FadeIn)
        {
            screenFade.FadeToBlack(fadeDuration);
        }
        else if (transition == FadeType.FadeOut)
        {
            screenFade.FadeToWhite(fadeDuration);
        }

        if (onFinishedMethod != string.Empty)
            screenFade.Connect("FadeFinished", target, onFinishedMethod);
    }

    /// <summary>
    ///   Helper function for playing a video stream.
    ///   Calls a function when finished.
    /// </summary>
    public void PlayCutscene(string path, Godot.Object target, string onFinishedMethod,
        bool allowSkipping)
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
            cutscene.Connect("CutsceneFinished", target, onFinishedMethod);

        // Initially adjust video player frame size
        cutscene.OnCutsceneResized();
    }
}
