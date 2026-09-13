using System;
using Godot;

/// <summary>
///   A free-flying camera for looking around a 3D scene, behaving like freelook mode in the Godot editor. Hold the
///   right mouse button to look around and move with WASD, and the cursor is left alone otherwise.
/// </summary>
[GlobalClass]
public partial class FreeViewCamera : Camera3D
{
    /// <summary>
    ///   Units moved per second. The mouse wheel adjusts this while looking around.
    /// </summary>
    [Export]
    public float MoveSpeed = 10.0f;

    /// <summary>
    ///   What the move speed is multiplied by while shift is held.
    /// </summary>
    [Export]
    public float SprintMultiplier = 3.0f;

    [Export]
    public float MouseSensitivity = 0.003f;

    [Export]
    public bool InvertY;

    [Export]
    public float MinMoveSpeed = 0.05f;

    [Export]
    public float MaxMoveSpeed = 1000.0f;

    /// <summary>
    ///   What one notch of the mouse wheel multiplies or divides the move speed by.
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

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && looking)
        {
            yaw -= motion.Relative.X * MouseSensitivity;
            pitch -= motion.Relative.Y * MouseSensitivity * (InvertY ? -1.0f : 1.0f);
            pitch = Math.Clamp(pitch, -MaxPitch, MaxPitch);

            ApplyRotation();
            GetViewport().SetInputAsHandled();
        }
    }

    [RunOnKeyChange("e_secondary", OnlyUnhandled = false)]
    public void OnLookInput(bool pressed)
    {
        SetLooking(pressed);
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

    [RunOnAxis(["g_zoom_out", "g_zoom_in"], [-1.0f, 1.0f], UseDiscreteKeyInputs = true)]
    public void ChangeMoveSpeed(double delta, float value)
    {
        _ = delta;

        if (!looking)
            return;

        AdjustMoveSpeed(MathF.Pow(SpeedAdjustFactor, value));
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
