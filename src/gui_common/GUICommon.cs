﻿using System;
using System.Collections.Generic;
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

#pragma warning disable CA2213
    private AudioStream buttonPressSound = null!;
#pragma warning restore CA2213

    private GUICommon()
    {
        instance = this;

        Tween = new Tween();
        AddChild(Tween);
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
    ///   The audio players for UI sound effects.
    /// </summary>
    private List<AudioStreamPlayer> AudioSources { get; } = new();

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

    public static void SmoothlyUpdateBar(Range bar, float target, float delta)
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

    public override void _Ready()
    {
        base._Ready();

        // Keep this node running even while paused
        PauseMode = PauseModeEnum.Process;

        buttonPressSound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
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

        if (customPopup != null)
        {
            customPopup.CustomHide();
            popup!.EmitSignal(nameof(CustomDialog.Closed));
        }
        else
        {
            popup!.Hide();
        }

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

        // Find a player not in use or create a new one if none are available.
        var player = AudioSources.Find(nextPlayer => !nextPlayer.Playing);

        if (player == null)
        {
            // If we hit the player limit just return and ignore the sound.
            if (AudioSources.Count >= Constants.MAX_CONCURRENT_UI_AUDIO_PLAYERS)
                return;

            player = new AudioStreamPlayer();
            player.Bus = "GUI";

            AddChild(player);
            AudioSources.Add(player);
        }

        player.VolumeDb = GD.Linear2Db(volume);
        player.Stream = sound;
        player.Play();
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

    public void ModulateFadeIn(Control control, float duration, float delay = 0,
        Tween.TransitionType transitionType = Tween.TransitionType.Sine, Tween.EaseType easeType = Tween.EaseType.In)
    {
        // Make sure the control is visible
        control.Show();

        Tween.InterpolateProperty(control, "modulate:a", null, 1, duration, transitionType, easeType, delay);
        Tween.Start();
    }

    public void ModulateFadeOut(Control control, float duration, float delay = 0, Tween.TransitionType transitionType =
        Tween.TransitionType.Sine, Tween.EaseType easeType = Tween.EaseType.In, bool hideOnFinished = true)
    {
        if (!control.Visible)
            return;

        Tween.InterpolateProperty(control, "modulate:a", null, 0, duration, transitionType, easeType, delay);
        Tween.Start();

        if (!Tween.IsConnected("tween_completed", this, nameof(HideControlOnFadeOutComplete)) && hideOnFinished)
        {
            Tween.Connect("tween_completed", this, nameof(HideControlOnFadeOutComplete),
                new Array { control }, (int)ConnectFlags.Oneshot);
        }
    }

    /// <summary>
    ///   Creates an icon for the given compound name.
    /// </summary>
    public TextureRect CreateCompoundIcon(string compoundName, float sizeX = 20.0f, float sizeY = 20.0f)
    {
        return CreateIcon(SimulationParameters.Instance.GetCompound(compoundName).LoadedIcon!, sizeX, sizeY);
    }

    /// <summary>
    ///   Creates an icon from the given texture.
    /// </summary>
    public TextureRect CreateIcon(Texture texture, float sizeX = 20.0f, float sizeY = 20.0f)
    {
        var element = new TextureRect
        {
            Expand = true,
            RectMinSize = new Vector2(sizeX, sizeY),
            SizeFlagsVertical = (int)Control.SizeFlags.ShrinkCenter,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = texture,
        };

        return element;
    }

    /// <summary>
    ///   This method exists because Godot signals always need an object so this is here as it makes some sense for
    ///   <see cref="ControlHelpers"/> to rely on this class
    /// </summary>
    /// <param name="target">The target control that should forward focus</param>
    internal void ProxyFocusForward(Control target)
    {
        target.ForwardFocusToNext();
    }

    /// <summary>
    ///   Proxies requests to
    /// </summary>
    internal void ProxyDrawFocus(Control target)
    {
        target.DrawCustomFocusBorderIfFocused();
    }

    private void HideControlOnFadeOutComplete(Object obj, NodePath key, Control control)
    {
        _ = obj;
        _ = key;

        control.Hide();
    }
}
