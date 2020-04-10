using Godot;
using System;

/// <summary>
///   Common helpers for the GUI to work with.
///   This singleton class is placed on AutoLoad.
/// </summary>
public class GUICommon : Node
{
    /// <summary>
    ///   Plays the button click sound effect
    ///   when a button is pressed.
    /// </summary>
    public void PlayButtonPressSound(AudioStreamPlayer audioPlayer)
    {
        var sound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");

        audioPlayer.Stream = sound;
        audioPlayer.Play();
    }

    /// <summmary>
    ///   Play a random audio track from an audio array.
    /// </summary>
    /// <param name="continuous">
    ///   Start a new track when the previous ends.
    /// </param>
    public void PlayRandomAudioTrack(AudioStreamPlayer audioPlayer,
        Godot.Collections.Array<AudioStream> audioTrack, bool continuous = false)
    {
        var random = new Random();
        int index = random.Next(audioTrack.Count);

        if (continuous)
        {
            audioPlayer.Connect("finished", this, nameof(PlayRandomAudioTrack));
        }
        else
        {
            if (audioPlayer.IsConnected("finished", this, nameof(PlayRandomAudioTrack)))
                audioPlayer.Disconnect("finished", this, nameof(PlayRandomAudioTrack));
        }

        audioPlayer.Stream = audioTrack[index];
        audioPlayer.Play();
    }

    /// <summary>
    ///   Helper function for screen fading.
    ///   Calls a function when finished.
    /// </summary>
    /// <param name="transition">
    ///   Set 0 for fading to black, and 1 for fading to white.
    /// </param>
    public void Fade(int transition, Godot.Object target, string onFinishedMethod,
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
