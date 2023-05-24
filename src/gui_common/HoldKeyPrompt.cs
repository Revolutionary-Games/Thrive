using System;
using Godot;

/// <summary>
///   Variant of the <see cref="KeyPrompt"/> that shows a "hold" progress on the button. For use with actions that take
///   some time to trigger.
/// </summary>
public class HoldKeyPrompt : KeyPrompt
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
    private float holdProgress;

    [Signal]
    public delegate void OnPressedLongEnough();

    public override void _Process(float delta)
    {
        if (!ShowPress)
        {
            if (holdProgress != 0)
            {
                holdProgress = 0;
                Update();
            }

            return;
        }

        base._Process(delta);

        if (!string.IsNullOrEmpty(ActionName))
        {
            var pressed = Input.IsActionPressed(ActionName);
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
                Update();
            }
        }
        else
        {
            if (holdProgress != 0)
            {
                holdProgress = 0;
                Update();
            }
        }
    }

    public override void _Draw()
    {
        base._Draw();

        if (holdProgress > 0.001f)
        {
            var ourSize = RectSize;

            var drawHeight = (int)Mathf.Round(ourSize.y * holdProgress);

            // Ensure at least one pixel drawn if there's some progress
            if (drawHeight < 1)
                drawHeight = 1;

            DrawRect(new Rect2(0, ourSize.y - drawHeight, ourSize.x, drawHeight),
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

        var size = RectSize;

        // Position the icons to leave some size for rendering the hold indicator

        var doubleIndicatorSize = HoldIndicatorMarginSize * 2;
        size = new Vector2(size.x - doubleIndicatorSize, size.y - doubleIndicatorSize);

        primaryIcon!.RectMinSize = size;
        secondaryIcon.RectMinSize = size;

        var offset = new Vector2(HoldIndicatorMarginSize, HoldIndicatorMarginSize);

        primaryIcon.RectPosition = offset;
        secondaryIcon.RectPosition = offset;
    }

    private void OnFullyPressed()
    {
        wasFullyPressed = true;
        EmitSignal(nameof(OnPressedLongEnough));
    }
}
