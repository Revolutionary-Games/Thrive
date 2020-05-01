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

    private Tween tween;

    private GUICommon()
    {
        instance = this;

        AudioSource = new AudioStreamPlayer();
        tween = new Tween();

        AddChild(AudioSource);
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

    /// <summary>
    ///   Smoothly interpolates TextureProgress bar value.
    /// </summary>
    public void TweenBarValue(TextureProgress bar, float targetValue, float maxValue)
    {
        var percentage = (targetValue / maxValue) * 100;
        tween.InterpolateProperty(bar, "value", bar.Value, percentage, 0.3f,
            Tween.TransitionType.Linear, Tween.EaseType.Out);
        tween.Start();
    }

    public void TweenUIProperty(Control ui, string property, object initialValue, object targetValue, 
        float duration, Tween.TransitionType transitionType = Tween.TransitionType.Linear,
        Tween.EaseType easeType = Tween.EaseType.InOut, float delay = 0)
    {
        tween.InterpolateProperty(ui, property, initialValue, targetValue, duration,
            transitionType, easeType, delay);
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
