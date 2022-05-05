using System;
using Godot;
using Array = Godot.Collections.Array;
using Object = Godot.Object;
using Path = System.IO.Path;

/// <summary>
///   Common helpers for the GUI to work with. This is autoloaded.
/// </summary>
public class GUICommon : NodeWithInput
{
    private static GUICommon? instance;

    private AudioStream buttonPressSound;

    private GUICommon()
    {
        instance = this;

        AudioSource = new AudioStreamPlayer();
        AudioSource.Bus = "GUI";
        AddChild(AudioSource);

        AudioSource2 = new AudioStreamPlayer();
        AudioSource2.Bus = "GUI";
        AddChild(AudioSource2);

        Tween = new Tween();
        AddChild(Tween);

        // Keep this node running even while paused
        PauseMode = PauseModeEnum.Process;

        buttonPressSound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
    }

    public static GUICommon Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   General purpose Tween node for use in various places.
    /// </summary>
    public Tween Tween { get; }

    /// <summary>
    ///   Checks whether the top-most modal on the modal stack is an exclusive popup.
    /// </summary>
    public bool IsAnyExclusivePopupActive => GetCurrentlyActiveExclusivePopup() != null;

    /// <summary>
    ///   The audio player for UI sound effects.
    /// </summary>
    private AudioStreamPlayer AudioSource { get; }

    /// <summary>
    ///   Second audio player for GUI effects. This is used if the primary one is still playing the previous effect.
    /// </summary>
    /// <remarks>
    ///    <para>
    ///      If the user is really fast with the mouse they can click buttons so fast that two sounds need to play at
    ///      once.
    ///    </para>
    /// </remarks>
    private AudioStreamPlayer AudioSource2 { get; }

    public static Vector2 GetFirstChildMinSize(Control control)
    {
        var child = control.GetChild<Control>(0);

        return child.RectMinSize;
    }

    public static void PopupMinSizeMarginPosition(Popup popup)
    {
        var left = popup.MarginLeft;
        var top = popup.MarginTop;
        popup.PopupCenteredMinsize();
        popup.RectPosition = new Vector2(left, top);
    }

    public static void SmoothlyUpdateBar(TextureProgress bar, float target, float delta)
    {
        if (delta <= 0)
        {
            GD.PrintErr("Tried to run SmoothlyUpdateBar with non-positive delta!");
            return;
        }

        var weight = Math.Min(3.0f * delta + 0.2f, 1.0f);
        bar.Value = MathUtils.Lerp((float)bar.Value, target, weight, 1.0f / (float)bar.MaxValue);
    }

    /// <summary>
    ///   Loads a Texture from predefined GUI asset texture folder path.
    /// </summary>
    public static Texture? LoadGuiTexture(string file)
    {
        var assumedPath = Path.Combine(Constants.ASSETS_GUI_BEVEL_FOLDER, file);

        if (ResourceLoader.Exists(assumedPath, "Texture"))
            return GD.Load<Texture>(assumedPath);

        // Fail-safe if file itself is the absolute path
        if (ResourceLoader.Exists(file, "Texture"))
            return GD.Load<Texture>(file);

        return GD.Load(file) as Texture;
    }

    public static void MarkInputAsInvalid(LineEdit control)
    {
        control.Set("custom_colors/font_color", new Color(1.0f, 0.3f, 0.3f));
    }

    public static void MarkInputAsValid(LineEdit control)
    {
        control.Set("custom_colors/font_color", new Color(1, 1, 1));
    }

    /// <summary>
    ///   Closes any currently active exclusive modal popups.
    /// </summary>
    [RunOnKeyDown("ui_cancel", Priority = Constants.POPUP_CANCEL_PRIORITY)]
    public bool HideCurrentlyActiveExclusivePopup()
    {
        var popup = GetCurrentlyActiveExclusivePopup();
        var customPopup = popup as ICustomPopup;

        if (!IsAnyExclusivePopupActive || customPopup is { ExclusiveAllowCloseOnEscape: false })
        {
            return false;
        }

        popup!.Hide();
        popup.EmitSignal(nameof(CustomDialog.Closed));

        return true;
    }

    /// <summary>
    ///   Returns the top-most exclusive popup in the current Viewport's modal stack. Null if there is none.
    /// </summary>
    public Popup? GetCurrentlyActiveExclusivePopup()
    {
        if (GetViewport().GetModalStackTop() is Popup popup && popup.PopupExclusive)
            return popup;

        return null;
    }

    /// <summary>
    ///   Play the button click sound effect.
    /// </summary>
    public void PlayButtonPressSound()
    {
        PlayCustomSound(buttonPressSound, 0.4f);
    }

    /// <summary>
    ///   Plays the given sound non-positionally.
    /// </summary>
    public void PlayCustomSound(AudioStream sound, float volume = 1.0f)
    {
        volume = Mathf.Clamp(volume, 0.0f, 1.0f);

        if (AudioSource.Playing)
        {
            // Use backup player if it is available
            if (!AudioSource2.Playing)
            {
                AudioSource2.Stream = sound;
                AudioSource2.VolumeDb = GD.Linear2Db(volume);
                AudioSource2.Play();
            }

            return;
        }

        AudioSource.Stream = sound;
        AudioSource.VolumeDb = GD.Linear2Db(volume);
        AudioSource.Play();
    }

    /// <summary>
    ///   Smoothly interpolates the value of a TextureProgress bar.
    /// </summary>
    public void TweenBarValue(TextureProgress bar, float targetValue, float maxValue, float speed)
    {
        bar.MaxValue = maxValue;
        Tween.InterpolateProperty(bar, "value", bar.Value, targetValue, speed,
            Tween.TransitionType.Cubic, Tween.EaseType.Out);
        Tween.Start();
    }

    /// <summary>
    ///   Smoothly interpolates the value of a ProgressBar.
    /// </summary>
    public void TweenBarValue(ProgressBar bar, float targetValue, float maxValue, float speed)
    {
        bar.MaxValue = maxValue;
        Tween.InterpolateProperty(bar, "value", bar.Value, targetValue, speed,
            Tween.TransitionType.Cubic, Tween.EaseType.Out);
        Tween.Start();
    }

    public void ModulateFadeIn(Control control, float duration,
        Tween.TransitionType transitionType = Tween.TransitionType.Sine, Tween.EaseType easeType = Tween.EaseType.In)
    {
        // Make sure the control is visible
        control.Show();
        control.Modulate = new Color(1, 1, 1, 0);

        Tween.InterpolateProperty(control, "modulate:a", 0, 1, duration, transitionType, easeType);
        Tween.Start();
    }

    public void ModulateFadeOut(Control control, float duration, Tween.TransitionType transitionType =
        Tween.TransitionType.Sine, Tween.EaseType easeType = Tween.EaseType.In, bool hideOnFinished = true)
    {
        control.Modulate = new Color(1, 1, 1, 1);

        Tween.InterpolateProperty(control, "modulate:a", 1, 0, duration, transitionType, easeType);
        Tween.Start();

        if (!Tween.IsConnected("tween_completed", this, nameof(HideControlOnFadeOutComplete)) && hideOnFinished)
        {
            Tween.Connect("tween_completed", this, nameof(HideControlOnFadeOutComplete),
                new Array { control }, (int)ConnectFlags.Oneshot);
        }
    }

    /// <summary>
    ///   Creates an icon for the given compound.
    /// </summary>
    public TextureRect CreateCompoundIcon(string compoundName, float sizeX = 20.0f, float sizeY = 20.0f)
    {
        var element = new TextureRect
        {
            Expand = true,
            RectMinSize = new Vector2(sizeX, sizeY),
            SizeFlagsVertical = (int)Control.SizeFlags.ShrinkCenter,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = SimulationParameters.Instance.GetCompound(compoundName).LoadedIcon,
        };

        return element;
    }

    private void HideControlOnFadeOutComplete(Object obj, NodePath key, Control control)
    {
        _ = obj;
        _ = key;

        control.Hide();
    }
}
