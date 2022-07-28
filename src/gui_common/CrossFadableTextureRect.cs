﻿using Godot;

/// <summary>
///   Displays image that can be changed smoothly with fade.
/// </summary>
public class CrossFadableTextureRect : TextureRect
{
    private Texture? image;
    private Tween tween = null!;

    [Signal]
    public delegate void Faded();

    /// <summary>
    ///   Image to be displayed. This fades the texture rect. To change the image without fading use
    ///   <see cref="TextureRect.Texture"/>.
    /// </summary>
    public Texture? Image
    {
        get => image;
        set
        {
            image = value;
            UpdateImage();
        }
    }

    [Export]
    public float FadeDuration { get; set; } = 0.5f;

    public override void _Ready()
    {
        tween = GetNode<Tween>("Tween");
    }

    private void UpdateImage()
    {
        // Initial image display shouldn't fade
        if (Texture == null)
        {
            Texture = Image;
            return;
        }

        tween.InterpolateProperty(this, "modulate", null, Colors.Black, FadeDuration);
        tween.Start();

        tween.CheckAndConnect(
            "tween_completed", this, nameof(OnFaded), null, (uint)ConnectFlags.Oneshot);
    }

    private void OnFaded(Object @object, NodePath key)
    {
        _ = @object;
        _ = key;

        Texture = Image;
        EmitSignal(nameof(Faded));

        tween.InterpolateProperty(this, "modulate", null, Colors.White, FadeDuration);
        tween.Start();
    }
}
