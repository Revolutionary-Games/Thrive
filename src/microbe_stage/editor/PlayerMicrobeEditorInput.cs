using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles key input in the microbe editor
/// </summary>
public class PlayerMicrobeEditorInput : InputEnvironment<MicrobeEditor>
{
    // Camera axis

    private readonly InputAxis panUpDownAxis;
    private readonly InputAxis panLeftRightAxis;

    // Individual camera inputs

    private readonly InputTrigger up = new InputTrigger("e_pan_up");
    private readonly InputTrigger down = new InputTrigger("e_pan_down");
    private readonly InputTrigger left = new InputTrigger("e_pan_left");
    private readonly InputTrigger right = new InputTrigger("e_pan_right");
    private readonly InputTrigger startMousePan = new InputTrigger("e_pan_mouse");
    private readonly InputTrigger reset = new InputTrigger("e_reset_cam");

    // Rotation

    private readonly InputTrigger rotateLeft = new InputTrigger("e_rotate_left");
    private readonly InputTrigger rotateRight = new InputTrigger("e_rotate_right");

    // Undo/Redo

    private readonly InputTrigger undo = new InputTrigger("e_undo");
    private readonly InputTrigger redo = new InputTrigger("e_redo");

    // Other

    private readonly InputTrigger primary = new InputTrigger("e_primary");
    private readonly InputTrigger secondary = new InputTrigger("e_secondary");

    /// <summary>
    ///   Where the user started panning with the mouse
    ///   Null if the user is not panning with the mouse
    /// </summary>
    private Vector3? mousePanningStart;

    public PlayerMicrobeEditorInput()
    {
        panUpDownAxis = new InputAxis(new List<(InputBool input, int associatedValue)>
        {
            (up, -1),
            (down, 1),
        });
        panLeftRightAxis = new InputAxis(new List<(InputBool input, int associatedValue)>
        {
            (left, -1),
            (right, 1),
        });

        Inputs = new InputGroup(new List<IInputReceiver>
        {
            panUpDownAxis,
            panLeftRightAxis,
            startMousePan,
            reset,
            rotateLeft,
            rotateRight,
            undo,
            redo,
            primary,
            secondary,
        });
    }

    protected override InputGroup Inputs { get; }

    public override void _Process(float delta)
    {
        base._Process(delta);
        ProcessCamPan(delta);
        ProcessRotation();
        ProcessRedoUndo();
        ProcessMainEditorControls();
    }

    private void ProcessCamPan(float delta)
    {
        if (mousePanningStart == null)
        {
            var movement = new Vector3(panLeftRightAxis.CurrentValue, 0, panUpDownAxis.CurrentValue);

            // Apply camera movement
            if (movement != Vector3.Zero) // Check to save performance
                Environment.MoveObjectToFollow(movement.Normalized() * delta * Environment.Camera.CameraHeight);
        }
        else
        {
            // Pan the camera with the mouse
            var mousePanDirection = mousePanningStart.Value - Environment.Camera.CursorWorldPos;
            Environment.MoveObjectToFollow(mousePanDirection);
        }

        if (startMousePan.ReadTrigger() && mousePanningStart == null)
        {
            mousePanningStart = Environment.Camera.CursorWorldPos;
            GD.Print("set mousePanningStart");
        }

        if (!startMousePan.Pressed)
        {
            mousePanningStart = null;
        }

        if (reset.ReadTrigger())
            Environment.ResetCamera();
    }

    private void ProcessRotation()
    {
        if (rotateLeft.ReadTrigger())
            Environment.RotateLeft();
        if (rotateRight.ReadTrigger())
            Environment.RotateRight();
    }

    private void ProcessRedoUndo()
    {
        if (undo.ReadTrigger())
            Environment.Undo();
        if (redo.ReadTrigger())
            Environment.Redo();
    }

    private void ProcessMainEditorControls()
    {
        if (primary.ReadTrigger())
            Environment.PlaceOrganelle();
        if (secondary.ReadTrigger())
            Environment.RemoveOrganelle();
    }
}
