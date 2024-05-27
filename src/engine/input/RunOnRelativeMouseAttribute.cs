using System;
using Godot;

/// <summary>
///   Handles captured mouse inputs for a single axis and direction
/// </summary>
/// <remarks>
///   <para>
///     This might not work when used by itself, currently only used through <see cref="RunOnAxisAttribute"/>
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     This ignores <see cref="InputAttribute.TrackInputMethod"/> as this is purely for mouse input, but as used in
///     groups, this still allows setting that to true in case we ever propagate that property down in an object
///     hierarchy. So this just ignores that being set to true instead of throwing anything.
///   </para>
/// </remarks>
public class RunOnRelativeMouseAttribute : RunOnInputWithStrengthAttribute
{
    private readonly CapturedMouseAxis axis;

    public RunOnRelativeMouseAttribute(CapturedMouseAxis axis) : base(CAPTURED_MOUSE_AS_AXIS_PREFIX)
    {
        this.axis = axis;

        // See the remark on this class
        LastUsedInputMethod = ActiveInputMethod.Keyboard;
    }

    public enum CapturedMouseAxis
    {
        Left,
        Right,
        Up,
        Down,
    }

    public override bool OnInput(InputEvent input)
    {
        if (input is not InputEventMouseMotion mouseMotion)
            return false;

        // Ignore if mouse is not captured
        if (!MouseCaptureManager.Captured)
            return false;

        var relative = mouseMotion.Relative;

        switch (axis)
        {
            case CapturedMouseAxis.Left:
                if (relative.X < 0)
                {
                    Strength = -relative.X;
                    HeldDown = true;
                }

                break;
            case CapturedMouseAxis.Right:
                if (relative.X > 0)
                {
                    Strength = relative.X;
                    HeldDown = true;
                }

                break;
            case CapturedMouseAxis.Up:
                if (relative.Y < 0)
                {
                    Strength = -relative.Y;
                    HeldDown = true;
                }

                break;
            case CapturedMouseAxis.Down:
                if (relative.Y > 0)
                {
                    Strength = relative.Y;
                    HeldDown = true;
                }

                break;
            default:
                throw new InvalidOperationException("unhandled mouse axis");
        }

        return false;
    }

    public override void OnProcess(double delta)
    {
        if (HeldDown)
        {
            PrepareMethodParameters(ref cachedMethodCallParameters, 2, delta);
            cachedMethodCallParameters![1] = Strength;

            CallMethod(cachedMethodCallParameters);

            MarkMouseMotionRead();
        }
    }

    public void MarkMouseMotionRead()
    {
        // This may not be the best overall design but this was at least a convenient variable to use for this
        // purpose
        HeldDown = false;
        Strength = 0;
    }
}
