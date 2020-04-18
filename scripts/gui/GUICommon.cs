using Godot;

/// <summary>
///   Common helpers for the GUI to work with.
///   This singleton class is placed on AutoLoad for
///   global access while still inheriting from Node.
/// </summary>
public class GUICommon : Node
{
    private static readonly GUICommon INSTANCE = new GUICommon();

    private AudioStream buttonPressSound;

    private Tween tween;

    private GUICommon()
    {
        UiAudio = new AudioStreamPlayer();
        tween = new Tween();

        AddChild(UiAudio);
        AddChild(tween);

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;

        buttonPressSound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
    }

    public static GUICommon Instance
    {
        get
        {
            return INSTANCE;
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
