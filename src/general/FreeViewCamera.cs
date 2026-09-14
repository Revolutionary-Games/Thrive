using System;
using Godot;

/// <summary>
///   A free-flying camera for looking around a 3D scene, behaving like freelook mode in the Godot editor. Hold the
///   <c>e_secondary</c> (e.g. RMB) to look around and move with the directional controls (e.g. WASD or any other input
///   scheme), and the cursor is left alone otherwise.
/// </summary>
[GlobalClass]
public partial class FreeViewCamera : Camera3D
{
    /// <summary>
    ///   Units moved per second. The zoom key adjusts this while looking around.
    /// </summary>
    [Export]
    public float MoveSpeed = 10.0f;

    /// <summary>
    ///   What the move speed is multiplied by while <c>g_sprint</c> is held.
    /// </summary>
    [Export]
    public float SprintMultiplier = 3.0f;

    [Export]
    public float MinMoveSpeed = 0.05f;

    [Export]
    public float MaxMoveSpeed = 1000.0f;

    /// <summary>
    ///   What one notch of the zoom key multiplies or divides the move speed by.
    /// </summary>
    [Export(PropertyHint.Range, "1.01,4.0,0.01")]
    public float SpeedAdjustFactor = 1.1f;

    private const float MaxPitch = MathF.PI * 0.5f - 0.01f;

    private readonly StringName sprintAction = new("g_sprint");

    private float pitch;
    private float yaw;

    private bool looking;

    public override void _Ready()
    {
        var euler = GlobalBasis.GetEuler(EulerOrder.Yxz);

        pitch = Math.Clamp(euler.X, -MaxPitch, MaxPitch);
        yaw = euler.Y;

        ApplyRotation();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputManager.UnregisterReceiver(this);

        StopLooking();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut)
            StopLooking();
    }

    [RunOnKeyChange("e_secondary", OnlyUnhandled = false)]
    public void OnLookInput(bool pressed)
    {
        SetLooking(pressed);
    }

    [RunOnAxis([
            RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX +
            nameof(RunOnRelativeMouseAttribute.CapturedMouseAxis.Right),
            "g_look_yaw_negative",
            RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX +
            nameof(RunOnRelativeMouseAttribute.CapturedMouseAxis.Left),
            "g_look_yaw_positive",
        ], [-1.0f, 1.0f],
        Look = RunOnAxisAttribute.LookMode.Yaw)]
    [RunOnAxis([
            RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX +
            nameof(RunOnRelativeMouseAttribute.CapturedMouseAxis.Down),
            "g_look_pitch_negative",
            RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX +
            nameof(RunOnRelativeMouseAttribute.CapturedMouseAxis.Up),
            "g_look_pitch_positive",
        ], [-1.0f, 1.0f],
        Look = RunOnAxisAttribute.LookMode.Pitch)]
    [RunOnAxisGroup(InvokeAlsoWithNoInput = false, InvokeWithDelta = false)]
    public void OnLook(float yawMovement, float pitchMovement)
    {
        if (!looking)
            return;

        yaw += yawMovement;
        pitch = Math.Clamp(pitch + pitchMovement, -MaxPitch, MaxPitch);

        ApplyRotation();
    }

    [RunOnAxis(["g_move_forward", "g_move_backwards"], [-1.0f, 1.0f])]
    [RunOnAxis(["g_move_left", "g_move_right"], [-1.0f, 1.0f])]
    [RunOnAxis(["g_move_down", "g_move_up"], [-1.0f, 1.0f])]
    [RunOnAxisGroup]
    public void Move(double delta, float forwardBackward, float leftRight, float downUp)
    {
        if (!looking)
            return;

        var basis = GlobalBasis;
        var direction = basis.Z * forwardBackward + basis.X * leftRight + Vector3.Up * downUp;

        if (direction.IsZeroApprox())
            return;

        float speed = MoveSpeed;

        if (Input.IsActionPressed(sprintAction))
            speed *= SprintMultiplier;

        GlobalPosition += direction.Normalized() * (speed * (float)delta);
    }

    [RunOnKeyDownWithRepeat("g_zoom_in")]
    public bool IncreaseMoveSpeed()
    {
        if (!looking)
            return false;

        AdjustMoveSpeed(SpeedAdjustFactor);
        return true;
    }

    [RunOnKeyDownWithRepeat("g_zoom_out")]
    public bool DecreaseMoveSpeed()
    {
        if (!looking)
            return false;

        AdjustMoveSpeed(1.0f / SpeedAdjustFactor);
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            sprintAction.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ApplyRotation()
    {
        GlobalBasis = Basis.FromEuler(new Vector3(pitch, yaw, 0.0f), EulerOrder.Yxz);
    }

    private void AdjustMoveSpeed(float factor)
    {
        float minimum = MathF.Min(MinMoveSpeed, MaxMoveSpeed);
        float maximum = MathF.Max(MinMoveSpeed, MaxMoveSpeed);

        MoveSpeed = Math.Clamp(MoveSpeed * factor, minimum, maximum);
    }

    private void StopLooking()
    {
        SetLooking(false);
    }

    private void SetLooking(bool wanted)
    {
        if (looking == wanted)
            return;

        looking = wanted;

        MouseCaptureManager.SetGameStateWantedCaptureState(wanted);
    }
}
