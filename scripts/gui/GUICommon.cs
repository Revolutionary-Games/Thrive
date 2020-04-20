using Godot;

/// <summary>
///   Common helpers for the GUI to work with.
///   This singleton class is placed on AutoLoad for
///   global access while still inheriting from Node.
/// </summary>
public class GUICommon : Node
{
    private static GUICommon instance;

    private AudioStream buttonPressSound;

    private GUICommon()
    {
        instance = this;

        AudioSource = new AudioStreamPlayer();

        AddChild(AudioSource);

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;

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
    public AudioStreamPlayer AudioSource { get; private set; }

    /// <summary>
    ///   Play the button click sound effect.
    /// </summary>
    public void PlayButtonPressSound()
    {
        AudioSource.Stream = buttonPressSound;
        AudioSource.Play();
    }

    /// <summary>
    ///   Plays the given sound non-positionally.
    /// </summary>
    public void PlayCustomSound(AudioStream sound)
    {
        AudioSource.Stream = sound;
        AudioSource.Play();
    }
}
