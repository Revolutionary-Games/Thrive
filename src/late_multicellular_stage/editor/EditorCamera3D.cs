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
    public float RotateSpeed = 0.8f;

    [Export]
    public float MouseRotateSpeed = 0.03f;

    [Export]
    public Transform DefaultPosition =
        new(new Basis(new Quat(new Vector3(1, 0, 0), MathUtils.DEGREES_TO_RADIANS * -20)),
            new Vector3(0, 2, 2.5f));

    [Export]
    public Vector3 RotateAroundPoint = Vector3.Zero;

    private const float VERTICAL_DIRECTION_THRESHOLD = (float)MathUtils.FULL_CIRCLE * 0.05f;

    private bool reversedXControl;

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
            // TODO: check if multiplying by delta is a good idea here
            RotateCamera(RotateSpeed * upDown * delta, RotateSpeed * leftRight * delta);
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
            var mouseDirection = (viewPort.GetMousePosition() - mousePanningStart.Value) * delta * MouseRotateSpeed;
            RotateCamera(-mouseDirection.y, mouseDirection.x);
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

    // TODO: should be instead make this zoom in and our towards the target point and have new keys for up/down?
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
        // TODO: rotate the pan vector with the current camera orientation, check this math is right
        var basis = Transform.basis;
        var rotation = basis.Quat();

        panAmount = rotation.Xform(panAmount);
        Transform = new Transform(basis, Transform.origin + panAmount);
        EmitSignal(nameof(OnPositionChanged), Transform);
    }

    private void RotateCamera(float xAngle, float yAngle)
    {
        var currentTransform = Transform;

        var up = new Vector3(0, 1, 0);
        var down = new Vector3(0, -1, 0);
        var left = new Vector3(1, 0, 0);
        var right = new Vector3(-1, 0, 0);
        var forward = new Vector3(0, 0, 1);
        var backwards = new Vector3(0, 0, -1);

        // If we have passed the straight up view, we need to invert xAngle
        if (reversedXControl)
            xAngle = -xAngle;

        // // If we are basically looking top down, we need to set forward as the up direction
        // if (currentTransform.basis.Xform(forward).AngleTo(up) < STRAIGHT_DOWN_ANGLE_THRESHOLD)
        var angle = currentTransform.basis.Xform(forward).SignedAngleTo(up, right);
        // var angle = currentTransform.basis.Xform(forward).AngleTo(up);

        /*if (angle is <= VERTICAL_DIRECTION_THRESHOLD or >= (float)MathUtils.FULL_CIRCLE - VERTICAL_DIRECTION_THRESHOLD)
        {
            // reversedXControl = !reversedXControl;


            // up = forward;
            up = backwards;

            xAngle = -xAngle;
            GD.Print("Angle: ", angle, " (if passed)");
        }
        else
        {
            GD.Print("Angle: ", angle);
        }*/

        if (reversedXControl)
        {
            up = backwards;
            xAngle = -xAngle;
        }

        // The up/down direction needs to be a cross vector with the towards direction to the object we are looking at
        var lookDirection = (RotateAroundPoint - currentTransform.origin).Normalized();
        var xAxis = lookDirection.Cross(up).Normalized();

        var newTranslation = (currentTransform.origin - RotateAroundPoint).Rotated(xAxis, xAngle)
            .Rotated(new Vector3(0, 1, 0), yAngle) +
            RotateAroundPoint;
        Translation = newTranslation;
        LookAt(RotateAroundPoint, up);

        // If we passed the vertical (or if we are flipped, the new "up" direction) we need to invert the controls
        var compareDirection = reversedXControl ? left : right;

        if (reversedXControl && Transform.basis.Xform(forward).AngleTo(up) < VERTICAL_DIRECTION_THRESHOLD)
        {
            GD.Print("horizontal passed");
            reversedXControl = false;
        } else

        if (!reversedXControl && Math.Sign(angle) != Math.Sign(Transform.basis.Xform(forward).SignedAngleTo(up, right)))
        // if (Math.Abs(angle - Transform.basis.Xform(forward).SignedAngleTo(up, right)) > VERTICAL_DIRECTION_THRESHOLD)
        // GD.Print("Angle: ", angle, " new: ", Transform.basis.Xform(forward).SignedAngleTo(up, right));
        // if (Math.Sign(angle) != Math.Sign(Transform.basis.Xform(forward).SignedAngleTo(up, right)))
        // if (angle < Transform.basis.Xform(forward).AngleTo(up))
        {
            GD.Print("Vertical passed");
            reversedXControl = true;
        }

        EmitSignal(nameof(OnPositionChanged), Transform);
    }
}
