﻿using System;
using Godot;

/// <summary>
///   Variant of the <see cref="KeyPrompt"/> that shows a "hold" progress on the button. For use with actions that take
///   some time to trigger.
/// </summary>
public partial class HoldKeyPrompt : KeyPrompt
{
    [Export]
    public float RequiredHoldTime = 1;

    /// <summary>
    ///   If over 0 then adds extra margin around the prompt icon for letting the hold fill colour be visible
    /// </summary>
    [Export]
    public int HoldIndicatorMarginSize;

    /// <summary>
    ///   How much to fill the indicator with a single press to make it more certain the user notices they need to keep
    ///   the button held down.
    /// </summary>
    [Export]
    public float SinglePressHoldProgress = 0.1f;

    /// <summary>
    ///   How quickly the filled indicator empties
    /// </summary>
    [Export]
    public float DecayRate = 4;

    /// <summary>
    ///   When fully pressed and then released, this is the decay rate used in that situation
    /// </summary>
    [Export]
    public float DecayRateAfterPress = 10;

    /// <summary>
    ///   Colour modulation of the press progress indicator
    /// </summary>
    [Export]
    public Color HoldIndicatorColour = new(0.0f, 0.7490f, 0.7137f, 1);

    /// <summary>
    ///   Colour modulation of the press progress indicator when the press has been registered
    /// </summary>
    [Export]
    public Color HoldIndicatorFilledColour = new(0.0f, 1.0f, 0.8353f, 1);

    private bool pressedPreviously;
    private bool wasFullyPressed;
    private double holdProgress;

    [Signal]
    public delegate void OnPressedLongEnoughEventHandler();

    /// <summary>
    ///   Value between 0-1 indicating how far along the press is currently
    /// </summary>
    public double HoldProgress => holdProgress;

    public override void _Process(double delta)
    {
        if (!ShowPress)
        {
            if (holdProgress != 0)
            {
                holdProgress = 0;
                QueueRedraw();
            }

            return;
        }

        base._Process(delta);

        if (!string.IsNullOrEmpty(ActionName))
        {
            var pressed = ResolvedAction != null && Input.IsActionPressed(ResolvedAction);
            var oldProgress = holdProgress;

            if (pressed)
            {
                // Give a bump of initial progress
                if (!pressedPreviously)
                {
                    pressedPreviously = true;
                    holdProgress = SinglePressHoldProgress;
                }
                else
                {
                    holdProgress += delta / RequiredHoldTime;
                }
            }
            else
            {
                if (wasFullyPressed)
                {
                    // Decay faster from a full press
                    holdProgress -= delta * DecayRateAfterPress;
                }
                else
                {
                    holdProgress -= delta * DecayRate;
                }

                // Reset pressed flag when this has been fully unpressed
                if (holdProgress <= 0)
                {
                    wasFullyPressed = false;
                    pressedPreviously = false;
                }
            }

            if (holdProgress > 1)
            {
                holdProgress = 1;

                if (!wasFullyPressed)
                    OnFullyPressed();
            }
            else if (holdProgress < 0)
            {
                holdProgress = 0;
            }

            if (Math.Abs(holdProgress - oldProgress) > 0.0001f)
            {
                QueueRedraw();
            }
        }
        else
        {
            if (holdProgress != 0)
            {
                holdProgress = 0;
                QueueRedraw();
            }
        }
    }

    public override void _Draw()
    {
        base._Draw();

        if (holdProgress > 0.001f)
        {
            var ourSize = Size;

            var drawHeight = ourSize.Y * (float)holdProgress;

            // Ensure at least one p.Xel drawn if there's some progress
            if (drawHeight < 1)
                drawHeight = 1;

            DrawRect(new Rect2(0, ourSize.Y - drawHeight, ourSize.X, drawHeight),
                holdProgress >= 1 ? HoldIndicatorFilledColour : HoldIndicatorColour, true);
        }
    }

    protected override void ApplySize()
    {
        if (HoldIndicatorMarginSize <= 0)
        {
            base.ApplySize();
            return;
        }

        var size = Size;

        // Position the icons to leave some size for rendering the hold indicator

        var doubleIndicatorSize = HoldIndicatorMarginSize * 2;
        size = new Vector2(size.X - doubleIndicatorSize, size.Y - doubleIndicatorSize);

        primaryIcon!.CustomMinimumSize = size;
        secondaryIcon.CustomMinimumSize = size;

        var offset = new Vector2(HoldIndicatorMarginSize, HoldIndicatorMarginSize);

        primaryIcon.Position = offset;
        secondaryIcon.Position = offset;
    }

    private void OnFullyPressed()
    {
        wasFullyPressed = true;
        EmitSignal(SignalName.OnPressedLongEnough);
    }
}
