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

        if (axis == CapturedMouseAxis.Left && relative.x < 0)
        {
            Strength = -relative.x;
            HeldDown = true;
        }
        else if (axis == CapturedMouseAxis.Right && relative.x > 0)
        {
            Strength = relative.x;
            HeldDown = true;
        }
        else if (axis == CapturedMouseAxis.Up && relative.y < 0)
        {
            Strength = -relative.y;
            HeldDown = true;
        }
        else if (axis == CapturedMouseAxis.Down && relative.y > 0)
        {
            Strength = relative.y;
            HeldDown = true;
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
