using Godot;

/// <summary>
///   Common helpers for the GUI to work with.
///   This class is placed on AutoLoad for global access
///   while still inheriting Node.
/// </summary>
public class GUICommon : Node
{
    private static GUICommon instance;

    private AudioStream buttonPressSound;

    private GUICommon()
    {
        instance = this;

        UiAudio = new AudioStreamPlayer();
        AddChild(UiAudio);

        // Keep running the audio player process while paused
        UiAudio.PauseMode = PauseModeEnum.Process;

        buttonPressSound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
    }

    public static GUICommon Instance
    {
        get
        {
            return instance;
        }
    }

    /// <summary>
    ///   The audio player for UI sound effects.
    /// </summary>
    public AudioStreamPlayer UiAudio { get; private set; }

    /// <summary>
    ///   Play the button click sound effect.
    /// </summary>
    public void PlayButtonPressSound()
    {
        UiAudio.Stream = buttonPressSound;
        UiAudio.Play();
    }
}
