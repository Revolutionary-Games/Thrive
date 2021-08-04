﻿using System;
using Godot;
using Array = Godot.Collections.Array;
using Object = Godot.Object;

/// <summary>
///   Common helpers for the GUI to work with. This is autoloaded.
/// </summary>
public class GUICommon : NodeWithInput
{
    private static GUICommon instance;

    private AudioStream buttonPressSound;

    private GUICommon()
    {
        instance = this;

        AudioSource = new AudioStreamPlayer();
        AudioSource.Bus = "GUI";
        Tween = new Tween();

        AddChild(AudioSource);
        AddChild(Tween);

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
    ///   General purpose Tween node for use in various places.
    /// </summary>
    public Tween Tween { get; }

    /// <summary>
    ///   Checks whether the top-most modal on stack is an exclusive popup.
    /// </summary>
    public bool IsAnyExclusivePopupVisible => GetCurrentlyActiveExclusivePopup() != null;

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
    ///   Closes any currently active exclusive modal popup.
    /// </summary>
    [RunOnKeyDown("ui_cancel", Priority = Constants.POPUP_CANCEL_PRIORITY)]
    public bool HideCurrentlyActiveExclusivePopup()
    {
        var popup = GetCurrentlyActiveExclusivePopup();
        var customPopup = popup as ICustomPopup;

        if (!IsAnyExclusivePopupVisible || (customPopup != null &&
            !customPopup.ExclusiveAllowCloseOnEscape))
        {
            return false;
        }

        if (customPopup != null)
        {
            customPopup.ClosePopup();
        }
        else
        {
            popup.Hide();
        }

        return true;
    }

    /// <summary>
    ///   Returns the top-most exclusive popup in the current Viewport's modal stack. Null if there is none.
    /// </summary>

    public Popup GetCurrentlyActiveExclusivePopup()
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
