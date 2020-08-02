using Godot;

/// <summary>
///   Common helpers for the GUI to work with. This is autoloaded.
/// </summary>
public class GUICommon : Node
{
    private static GUICommon instance;

    private AudioStream buttonPressSound;

    private Tween tween;

    private GUICommon()
    {
        instance = this;

        AudioSource = new AudioStreamPlayer();
        tween = new Tween();

        AddChild(AudioSource);
        AddChild(tween);

        // Keep this node running even while paused
        PauseMode = PauseModeEnum.Process;

        buttonPressSound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
    }

    public static GUICommon Instance => instance;

    /// <summary>
    ///   The audio player for UI sound effects.
    /// </summary>
    public AudioStreamPlayer AudioSource { get; }

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

    /// <summary>
    ///   Smoothly interpolates the value of a TextureProgress bar.
    /// </summary>
    public void TweenBarValue(TextureProgress bar, float targetValue, float maxValue)
    {
        var percentage = (targetValue / maxValue) * 100;
        tween.InterpolateProperty(bar, "value", bar.Value, percentage, 0.3f,
            Tween.TransitionType.Linear, Tween.EaseType.Out);
        tween.Start();
    }

    /// <summary>
    ///   Creates an icon for the given compound.
    /// </summary>
    public TextureRect CreateCompoundIcon(string compoundName, float sizeX = 20.0f, float sizeY = 20.0f)
    {
        var element = new TextureRect();
        element.Expand = true;
        element.RectMinSize = new Vector2(sizeX, sizeY);

        var icon = GD.Load<Texture>("res://assets/textures/gui/bevel/" + compoundName.ReplaceN(
            " ", string.Empty) + ".png");

        element.Texture = icon;

        return element;
    }
}
