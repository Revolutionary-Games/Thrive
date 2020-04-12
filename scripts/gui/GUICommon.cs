using Godot;

/// <summary>
///   Common helpers for the GUI to work with.
///   This singleton class is placed on AutoLoad.
/// </summary>
public class GUICommon : Node
{
    public AudioStreamPlayer UiAudio;

    private AudioStream buttonPressSound;

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
}
