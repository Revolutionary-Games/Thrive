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
    public Vector3 RotateAroundPoint = Vector3.Zero;

    /// <summary>
    ///   Where the user started rotating with the mouse. Null if the user is not rotating with the mouse
    /// </summary>
    private Vector3? mousePanningStart;

    /// <summary>
    ///   Triggered when the user uses the input actions to move this camera, used for editor tabs to be able to save
    ///   where their camera was
    /// </summary>
    [Signal]
    public delegate void OnPositionChanged(Transform newPosition);

    public override void _Ready()
    {
        base._Ready();
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

    /*[RunOnKeyDown("e_3d_camera_front")]
    public bool ToFrontView()
    {
        if(!Current || !Visible)
            return false;
    }

    [RunOnAxisGroup]
    [RunOnAxis(new[] { "e_pan_up", "e_pan_down" }, new[] { -1.0f, 1.0f })]
    [RunOnAxis(new[] { "e_pan_left", "e_pan_right" }, new[] { -1.0f, 1.0f })]
    public bool PanCameraWithKeys(float delta, float upDown, float leftRight)
    {
        if (!Visible)
            return false;

        if (mousePanningStart != null)
            return true;

        var movement = new Vector3(leftRight, 0, upDown);
        MoveCamera(movement.Normalized() * delta * CameraHeight);
        return true;
    }

    [RunOnKey("e_pan_mouse", CallbackRequiresElapsedTime = false)]
    public bool PanCameraWithMouse(float delta)
    {
        if (!Visible)
            return false;

        if (mousePanningStart == null)
        {
            mousePanningStart = camera!.CursorWorldPos;
        }
        else
        {
            var mousePanDirection = mousePanningStart.Value - camera!.CursorWorldPos;
            MoveCamera(mousePanDirection * delta * 10);
        }

        return false;
    }

    [RunOnKeyUp("e_pan_mouse")]
    public bool ReleasePanCameraWithMouse()
    {
        if (!Visible)
            return false;

        mousePanningStart = null;
        return true;
    }

    [RunOnKeyDown("e_reset_camera")]
    public bool ResetCamera()
    {
        if (!Visible)
            return false;

        if (camera == null)
        {
            GD.PrintErr("Editor camera isn't set");
            return false;
        }

        CameraPosition = new Vector3(0, 0, 0);
        UpdateCamera();

        camera.ResetHeight();
        return true;
    }
    */
}
