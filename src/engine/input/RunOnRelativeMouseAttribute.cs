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
public class RunOnRelativeMouseAttribute : RunOnInputWithStrengthAttribute
{
    private readonly CapturedMouseAxis axis;

    public RunOnRelativeMouseAttribute(CapturedMouseAxis axis) : base(CAPTURED_MOUSE_AS_AXIS_PREFIX)
    {
        this.axis = axis;
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
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
            return false;

        var relative = mouseMotion.Relative;

        switch (axis)
        {
            case CapturedMouseAxis.Left:
                if (relative.x < 0)
                {
                    Strength = -relative.x;
                    HeldDown = true;
                }

                break;
            case CapturedMouseAxis.Right:
                if (relative.x > 0)
                {
                    Strength = relative.x;
                    HeldDown = true;
                }

                break;
            case CapturedMouseAxis.Up:
                if (relative.y < 0)
                {
                    Strength = -relative.y;
                    HeldDown = true;
                }

                break;
            case CapturedMouseAxis.Down:
                if (relative.y > 0)
                {
                    Strength = relative.y;
                    HeldDown = true;
                }

                break;
            default:
                throw new InvalidOperationException("unhandled mouse axis");
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
        if (HeldDown)
        {
            CallMethod(delta, Strength);

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
