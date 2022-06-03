using System;
using Godot;

public class EditorCamera3D : Camera
{
    /// <summary>
    ///   Minimum distance the camera can be at from the rotate point
    /// </summary>
    [Export]
    public float MinDistance = 0.01f;

    [Export]
    public float MaxDistance = 50.0f;

    [Export]
    public float SidePresetViewDistances = 5.0f;

    [Export]
    public float PanSpeed = 5.0f;

    [Export]
    public float MousePanSpeed = 0.05f;

    [Export]
    public float UpDownMoveSpeed = 0.15f;

    [Export]
    public float RotateSpeed = 5.0f;

    [Export]
    public Transform DefaultPosition =
        new(new Basis(new Quat(new Vector3(1, 0, 0), MathUtils.DEGREES_TO_RADIANS * -20)),
            new Vector3(0, 2, 2.5f));

    [Export]
    public Vector3 RotateAroundPoint = Vector3.Zero;

    /// <summary>
    ///   Where the user started rotating (or panning) with the mouse. Null if the user is not rotating with the mouse
    /// </summary>
    private Vector2? mousePanningStart;

    private bool panning;

    /// <summary>
    ///   Triggered when the user uses the input actions to move this camera, used for editor tabs to be able to save
    ///   where their camera was
    /// </summary>
    [Signal]
    public delegate void OnPositionChanged(Transform newPosition);

    public override void _EnterTree()
    {
        base._EnterTree();
        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputManager.UnregisterReceiver(this);
    }

    [RunOnKeyDown("e_camera_front", Priority = -1)]
    public bool ToFrontView()
    {
        if (!Current || !Visible)
            return false;

        Translation = new Vector3(0, 0, SidePresetViewDistances);
        LookAt(RotateAroundPoint, Vector3.Up);
        EmitSignal(nameof(OnPositionChanged), Transform);
        return true;
    }

    [RunOnKeyDown("e_camera_right", Priority = -1)]
    public bool ToRightView()
    {
        if (!Current || !Visible)
            return false;

        Translation = new Vector3(SidePresetViewDistances, 0, 0);
        LookAt(RotateAroundPoint, Vector3.Up);
        EmitSignal(nameof(OnPositionChanged), Transform);
        return true;
    }

    [RunOnKeyDown("e_camera_top", Priority = -1)]
    public bool ToTopView()
    {
        if (!Current || !Visible)
            return false;

        Translation = new Vector3(0, SidePresetViewDistances, 0);
        LookAt(RotateAroundPoint, Vector3.Forward);
        EmitSignal(nameof(OnPositionChanged), Transform);
        return true;
    }

    [RunOnAxisGroup(Priority = -1)]
    [RunOnAxis(new[] { "e_pan_up", "e_pan_down" }, new[] { -1.0f, 1.0f })]
    [RunOnAxis(new[] { "e_pan_left", "e_pan_right" }, new[] { -1.0f, 1.0f })]
    public bool RotateOrPanCameraWithKeys(float delta, float upDown, float leftRight)
    {
        if (!Current || !Visible)
            return false;

        if (mousePanningStart != null)
            return true;

        var movement = new Vector3(leftRight, 0, upDown);

        if (IsPanModeWanted())
        {
            PanCamera(movement.Normalized() * delta * PanSpeed);
        }
        else
        {
            // TODO: rotate
        }

        return true;
    }

    [RunOnKey("e_pan_mouse", CallbackRequiresElapsedTime = false)]
    public bool RotateOrPanCameraWithMouse(float delta)
    {
        if (!Current || !Visible)
            return false;

        if (mousePanningStart == null)
            return false;

        var viewPort = GetViewport();

        if (viewPort == null)
            throw new InvalidOperationException("No viewport");

        if (panning)
        {
            var mousePanDirection = (viewPort.GetMousePosition() - mousePanningStart.Value) * delta * MousePanSpeed;
            PanCamera(new Vector3(mousePanDirection.x, 0, mousePanDirection.y));
        }
        else
        {
            // TODO: rotating
        }

        return true;
    }

    [RunOnKeyDown("e_pan_mouse", Priority = -1)]
    public bool StartRotateOrPanCameraWithMouse()
    {
        if (!Current || !Visible)
            return false;

        var viewPort = GetViewport();

        if (viewPort == null)
            throw new InvalidOperationException("No viewport");

        mousePanningStart = viewPort.GetMousePosition();
        panning = IsPanModeWanted();
        return true;
    }

    [RunOnKeyUp("e_pan_mouse", OnlyUnhandled = false)]
    public bool ReleaseRotateOrPanCameraWithMouse()
    {
        if (!Current || !Visible)
            return false;

        mousePanningStart = null;
        return false;
    }

    [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f }, UseDiscreteKeyInputs = true, Priority = -1)]
    public void PanUpDown(float delta, float value)
    {
        _ = delta;

        var height = Translation.y;

        var old = height;

        height += UpDownMoveSpeed * value;

        height = height.Clamp(0, MaxDistance);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (height != old)
        {
            Translation = new Vector3(Translation.x, height, Translation.z);
            EmitSignal(nameof(OnPositionChanged), Transform);
        }
    }

    [RunOnKeyDown("e_reset_camera", Priority = -1)]
    public bool ResetCamera()
    {
        if (!Current || !Visible)
            return false;

        Transform = new Transform(DefaultPosition.basis, DefaultPosition.origin + RotateAroundPoint);
        EmitSignal(nameof(OnPositionChanged), Transform);
        return true;
    }

    private bool IsPanModeWanted()
    {
        return Input.IsActionPressed("e_pan_mode");
    }

    private void PanCamera(Vector3 panAmount)
    {
        // TODO: rotate the pan vector with the current camera orientation
        Transform = new Transform(Transform.basis, Transform.origin + panAmount);
        EmitSignal(nameof(OnPositionChanged), Transform);
    }
}
