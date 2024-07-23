using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Camera for the 3D editor scenes
/// </summary>
public partial class EditorCamera3D : Camera3D
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
    public float MousePanHorizontalMultiplier = 0.6f;

    [Export]
    public float MousePanVerticalMultiplier = 0.8f;

    [Export]
    public float UpDownMoveSpeed = 1.0f;

    [Export]
    public float ZoomSpeed = 0.20f;

    [Export]
    public float RotateSpeed = 0.85f;

    [Export]
    public float MouseRotateMultiplier = 0.5f;

    [Export]
    public bool InvertedMouseRotation;

    [Export]
    public bool InvertedMousePanning;

    /// <summary>
    ///   The current rotation around the X-axis
    /// </summary>
    [Export]
    [JsonProperty]
    public float XRotation = -(float)MathUtils.FULL_CIRCLE * 0.1f;

    [Export]
    [JsonProperty]
    public float YRotation;

    [Export]
    public float DefaultXRotation = -(float)MathUtils.FULL_CIRCLE * 0.1f;

    [Export]
    public float DefaultYRotation;

    [Export]
    [JsonProperty]
    public float ViewDistance = 3.0f;

    [Export]
    public float DefaultViewDistance = 3.0f;

    [Export]
    public Vector3 RotateAroundPoint = Vector3.Zero;

    private readonly StringName panModeAction = new("e_pan_mode");

    /// <summary>
    ///   To make the 3D math reasonable to implement here, we use the x,y and distance values to rotate the camera
    ///   around the target point and panning is just an offset on top of that.
    /// </summary>
    [JsonProperty]
    private Vector3 panOffset;

    /// <summary>
    ///   Where the user started rotating (or panning) with the mouse. Null if the user is not rotating with the mouse.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The mouse panning is not infinite in the 3D camera as that feels weird, so we store the last position
    ///     in this variable where we panned with the mouse to be able to just add the delta of the current movement.
    ///   </para>
    /// </remarks>
    private Vector2? mousePanningStart;

    private bool panning;

    /// <summary>
    ///   Triggered when the user uses the input actions to move this camera, used for editor tabs to be able to save
    ///   where their camera was
    /// </summary>
    [Signal]
    public delegate void OnPositionChangedEventHandler(Transform3D newPosition);

    public override void _Ready()
    {
        base._Ready();
        ApplyTransform();
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
    }

    [RunOnKeyDown("e_camera_front", Priority = -1)]
    public bool ToFrontView()
    {
        if (!Current || !Visible)
            return false;

        XRotation = 0;
        YRotation = DefaultYRotation;
        panOffset = Vector3.Zero;
        ApplyTransform();

        return true;
    }

    [RunOnKeyDown("e_camera_right", Priority = -1)]
    public bool ToRightView()
    {
        if (!Current || !Visible)
            return false;

        XRotation = 0;
        YRotation = (float)MathUtils.FULL_CIRCLE * 0.25f;
        panOffset = Vector3.Zero;
        ApplyTransform();

        return true;
    }

    [RunOnKeyDown("e_camera_top", Priority = -1)]
    public bool ToTopView()
    {
        if (!Current || !Visible)
            return false;

        XRotation = -(float)MathUtils.FULL_CIRCLE * 0.25f;
        YRotation = DefaultYRotation;
        panOffset = Vector3.Zero;
        ApplyTransform();

        return true;
    }

    [RunOnAxis(new[] { "e_pan_up", "e_pan_down" }, new[] { -1.0f, 1.0f })]
    [RunOnAxis(new[] { "e_pan_left", "e_pan_right" }, new[] { -1.0f, 1.0f })]
    [RunOnAxisGroup(Priority = -1)]
    public bool RotateOrPanCameraWithKeys(double delta, float upDown, float leftRight)
    {
        if (!Current || !Visible)
            return false;

        if (mousePanningStart != null)
            return true;

        var movement = new Vector3(leftRight, 0, upDown);

        if (IsPanModeWanted())
        {
            PanCamera(movement.Normalized() * (float)delta * PanSpeed);
        }
        else
        {
            // TODO: check if multiplying by delta is a good idea here
            RotateCamera(RotateSpeed * upDown * (float)delta, RotateSpeed * leftRight * (float)delta);
        }

        return true;
    }

    [RunOnKey("e_pan_mouse", Priority = -2, OnlyUnhandled = false)]
    public void RotateOrPanCameraWithMouse(double delta)
    {
        if (!Current || !Visible)
            return;

        if (mousePanningStart == null)
            return;

        var viewPort = GetViewport();

        if (viewPort == null)
            throw new InvalidOperationException("No viewport");

        var newPosition = viewPort.GetMousePosition();

        if (mousePanningStart == newPosition)
            return;

        if (panning)
        {
            // TODO: could maybe take the aspect ratio of the viewport into account rather than having two explicit
            // variables
            var mousePanDirection = (newPosition - mousePanningStart.Value) * (float)delta *
                new Vector2(MousePanHorizontalMultiplier, -MousePanVerticalMultiplier);

            if (!InvertedMousePanning)
                mousePanDirection = -mousePanDirection;

            PanCamera(new Vector3(mousePanDirection.X, mousePanDirection.Y, 0));
        }
        else
        {
            var mouseDirection = (newPosition - mousePanningStart.Value) * (float)delta * MouseRotateMultiplier;

            if (!InvertedMouseRotation)
                mouseDirection = -mouseDirection;

            RotateCamera(mouseDirection.Y, mouseDirection.X);
        }

        mousePanningStart = newPosition;
    }

    [RunOnKeyDown("e_pan_mouse", Priority = -1, OnlyUnhandled = false)]
    public bool StartRotateOrPanCameraWithMouse()
    {
        if (!Current || !Visible || mousePanningStart != null)
            return false;

        var viewPort = GetViewport();

        if (viewPort == null)
            throw new InvalidOperationException("No viewport");

        mousePanningStart = viewPort.GetMousePosition();
        panning = IsPanModeWanted();
        return false;
    }

    [RunOnKeyUp("e_pan_mouse", OnlyUnhandled = false)]
    public bool ReleaseRotateOrPanCameraWithMouse()
    {
        mousePanningStart = null;
        return false;
    }

    [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f }, UseDiscreteKeyInputs = true, Priority = -1)]
    public void ZoomInOrOut(double delta, float value)
    {
        _ = delta;

        ViewDistance = (ViewDistance + ZoomSpeed * value).Clamp(MinDistance, MaxDistance);
        ApplyTransform();
    }

    [RunOnAxis(new[] { "g_move_down", "g_move_up" }, new[] { -1.0f, 1.0f })]
    public void PanUpDown(double delta, float value)
    {
        PanCamera(new Vector3(0, UpDownMoveSpeed * value * (float)delta, 0));
    }

    [RunOnKeyDown("e_reset_camera", Priority = -1)]
    public bool ResetCamera()
    {
        if (!Current || !Visible)
            return false;

        XRotation = DefaultXRotation;
        YRotation = DefaultYRotation;
        ViewDistance = DefaultViewDistance;
        panOffset = Vector3.Zero;

        ApplyTransform();

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            panModeAction.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool IsPanModeWanted()
    {
        return Input.IsActionPressed(panModeAction);
    }

    private void PanCamera(Vector3 panAmount)
    {
        var yAmount = panAmount.Y;
        panAmount.Y = 0;

        // Only left and right look rotation is taken into account for movement, so that it feels better
        var rotation = new Quaternion(new Vector3(0, 1, 0), YRotation).Normalized();

        panAmount = rotation * panAmount;
        panOffset += panAmount;

        // Y-axis panning *does* take the full rotation of the camera into account
        if (yAmount != 0)
        {
            rotation = Transform.Basis.GetRotationQuaternion().Normalized();

            panAmount = rotation * new Vector3(0, yAmount, 0);
            panOffset += panAmount;
        }

        ApplyTransform();
    }

    private void RotateCamera(float xAngle, float yAngle)
    {
        XRotation = (XRotation + xAngle) % (float)MathUtils.FULL_CIRCLE;
        YRotation = (YRotation + yAngle) % (float)MathUtils.FULL_CIRCLE;
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        var currentTransform = Transform;

        var right = new Vector3(1, 0, 0);
        var up = new Vector3(0, 1, 0);

        var rotatedPosition = new Vector3(0, 0, ViewDistance).Rotated(right, XRotation).Rotated(up, YRotation) +
            RotateAroundPoint;

        Position = rotatedPosition;

        // "Up" being always up here makes the visuals a bit weird looking when going fully around
        // TODO: could add a mode that clamps the XRotation to ]-0.5 * FULL_CIRCLE, 0.5 * FULL_CIRCLE[
        LookAt(RotateAroundPoint, up);

        var newTransform = new Transform3D(Transform.Basis, rotatedPosition + panOffset);

        if (newTransform != currentTransform)
        {
            Transform = newTransform;
            EmitSignal(SignalName.OnPositionChanged, Transform);
        }
    }
}
