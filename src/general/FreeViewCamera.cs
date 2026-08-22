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

    public override void _ExitTree()
    {
        base._ExitTree();

        StopLooking();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut)
            StopLooking();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            HandleMouseButton(button);
            return;
        }

        if (@event is InputEventMouseMotion motion && looking)
        {
            yaw -= motion.Relative.X * MouseSensitivity;
            pitch -= motion.Relative.Y * MouseSensitivity * (InvertY ? -1.0f : 1.0f);
            pitch = Math.Clamp(pitch, -MaxPitch, MaxPitch);

            ApplyRotation();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!looking)
            return;

        var basis = GlobalBasis;
        var direction = Vector3.Zero;

        if (Input.IsPhysicalKeyPressed(Key.W))
            direction -= basis.Z;

        if (Input.IsPhysicalKeyPressed(Key.S))
            direction += basis.Z;

        if (Input.IsPhysicalKeyPressed(Key.A))
            direction -= basis.X;

        if (Input.IsPhysicalKeyPressed(Key.D))
            direction += basis.X;

        if (Input.IsPhysicalKeyPressed(Key.E))
            direction += Vector3.Up;

        if (Input.IsPhysicalKeyPressed(Key.Q))
            direction -= Vector3.Up;

        if (direction.IsZeroApprox())
            return;

        float speed = MoveSpeed;

        if (Input.IsPhysicalKeyPressed(Key.Shift))
            speed *= SprintMultiplier;

        GlobalPosition += direction.Normalized() * (speed * (float)delta);
    }

    private void HandleMouseButton(InputEventMouseButton button)
    {
        if (button.ButtonIndex == MouseButton.Right)
        {
            SetLooking(button.Pressed);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!looking || !button.Pressed)
            return;

        if (button.ButtonIndex == MouseButton.WheelUp)
        {
            AdjustMoveSpeed(SpeedAdjustFactor);
            GetViewport().SetInputAsHandled();
        }
        else if (button.ButtonIndex == MouseButton.WheelDown)
        {
            AdjustMoveSpeed(1.0f / SpeedAdjustFactor);
            GetViewport().SetInputAsHandled();
        }
    }

    private void ApplyRotation()
    {
        GlobalBasis = Basis.FromEuler(new Vector3(pitch, yaw, 0.0f), EulerOrder.Yxz);
    }

    private void AdjustMoveSpeed(float factor)
    {
        MoveSpeed = Math.Clamp(MoveSpeed * factor, MinMoveSpeed, MaxMoveSpeed);
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
