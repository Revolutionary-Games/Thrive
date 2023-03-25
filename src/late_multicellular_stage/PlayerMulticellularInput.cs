using System;
using Godot;

/// <summary>
///   Handles input for the (late) multicellular stage
/// </summary>
public class PlayerMulticellularInput : NodeWithInput
{
    private bool autoMove;
    private bool mouseUnCapturePressed;

#pragma warning disable CA2213 // this is our parent object
    private MulticellularStage stage = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        // Not the cleanest that the parent has to be MulticellularStage type...
        stage = (MulticellularStage)GetParent();

        PauseMode = PauseModeEnum.Process;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Release our mouse capture, _Process shouldn't get called again after this exited the tree
        MouseCaptureManager.SetGameStateWantedCaptureState(false);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // Handle the GUI mouse capture
        MouseCaptureManager.SetGameStateWantedCaptureState(!mouseUnCapturePressed && !PauseManager.Instance.Paused);
    }

    [RunOnKeyDown("g_hold_forward")]
    public void ToggleAutoMove()
    {
        autoMove = !autoMove;
    }

    [RunOnKeyDown("g_free_cursor")]
    public bool StartShowingCursor()
    {
        mouseUnCapturePressed = true;
        return false;
    }

    [RunOnKeyUp("g_free_cursor")]
    public void StopShowingCursor()
    {
        mouseUnCapturePressed = false;
    }

    [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
    [RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
    [RunOnAxisGroup(InvokeAlsoWithNoInput = true)]
    public void OnMovement(float delta, float forwardMovement, float leftRightMovement)
    {
        _ = delta;
        const float epsilon = 0.01f;

        // Reset auto move if a key was pressed
        if (Math.Abs(forwardMovement) + Math.Abs(leftRightMovement) > epsilon)
        {
            autoMove = false;
        }

        if (stage.Player != null)
        {
            Vector3 movement;

            if (autoMove)
            {
                movement = new Vector3(0, 0, -1);
            }
            else
            {
                movement = new Vector3(leftRightMovement, 0, forwardMovement);

                // To allow slow movement with a controller
                if (movement.Length() > 1)
                    movement = movement.Normalized();
            }

            if (autoMove || Settings.Instance.ThreeDimensionalMovement.Value !=
                ThreeDimensionalMovementMode.WorldRelative)
            {
                // Rotate movement direction by the 2D rotation of the camera
                var rotation = new Quat(new Vector3(0, 1, 0), stage.PlayerCamera.YRotation);

                movement = rotation.Xform(movement);
            }

            stage.Player.MovementDirection = movement;
        }
    }

    [RunOnKey("g_move_up")]
    public void SwimUpOrJump(float delta)
    {
        stage.Player?.SwimUpOrJump(delta);
    }

    [RunOnKey("g_move_down")]
    public void SwimDownOrCrouch(float delta)
    {
        stage.Player?.SwimDownOrCrouch(delta);
    }

    [RunOnAxis(
        new[]
        {
            RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX +
            nameof(RunOnRelativeMouseAttribute.CapturedMouseAxis.Right),
            "g_look_yaw_negative",
            RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX +
            nameof(RunOnRelativeMouseAttribute.CapturedMouseAxis.Left),
            "g_look_yaw_positive",
        }, new[] { -1.0f, 1.0f },
        Look = RunOnAxisAttribute.LookMode.Yaw)]
    [RunOnAxis(new[]
        {
            RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX +
            nameof(RunOnRelativeMouseAttribute.CapturedMouseAxis.Down),
            "g_look_pitch_negative",
            RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX +
            nameof(RunOnRelativeMouseAttribute.CapturedMouseAxis.Up),
            "g_look_pitch_positive",
        }, new[] { -1.0f, 1.0f },
        Look = RunOnAxisAttribute.LookMode.Pitch)]
    [RunOnAxisGroup(InvokeAlsoWithNoInput = false, InvokeWithDelta = false)]
    public void OnLook(float yawMovement, float pitchMovement)
    {
        stage.RotateCamera(yawMovement, pitchMovement);
    }

    [RunOnKeyDown("g_interact")]
    public void InteractWithEnvironment()
    {
        stage.AttemptPlayerWorldInteraction();
    }

    [RunOnKeyDown("g_inventory")]
    public void OpenInventory()
    {
        stage.TogglePlayerInventory();
    }

    [RunOnKeyDown("g_build_structure")]
    public void OpenBuildMenu()
    {
        stage.PerformBuildOrOpenMenu();
    }

    [RunOnKeyDown("ui_cancel")]
    public bool CancelBuild()
    {
        return stage.CancelBuildingPlaceIfInProgress();
    }

    [RunOnKeyDown("g_fire_toxin")]
    public void EmitToxin()
    {
        // TODO: implement

        // stage.Player?.EmitToxin();
    }

    [RunOnKeyDown("g_toggle_engulf")]
    public void ToggleEngulf()
    {
        // TODO: implement

        // if (stage.Player == null)
        //     return;

        // if (stage.Player.State == Microbe.MicrobeState.Engulf)
        // {
        //     stage.Player.State = Microbe.MicrobeState.Normal;
        // }
        // else if (!stage.Player.Membrane.Type.CellWall)
        // {
        //     stage.Player.State = Microbe.MicrobeState.Engulf;
        // }
    }

    [RunOnKeyDown("g_pack_commands")]
    public bool ShowSignalingCommandsMenu()
    {
        // TODO: implement the communication technology that unlocks when using the commands

        // TODO: implement

        // if (stage.Player?.HasSignalingAgent != true)
        //     return false;
        //
        // stage.HUD.ShowSignalingCommandsMenu(stage.Player);

        // We need to not consume the input, otherwise the key up for this will not run
        return false;
    }

    [RunOnKeyUp("g_pack_commands")]
    public void CloseSignalingCommandsMenu()
    {
        // TODO: implement

        // var command = stage.HUD.SelectSignalCommandIfOpen();
        //
        // if (stage.Player != null)
        //     stage.HUD.ApplySignalCommand(command, stage.Player);
    }

    [RunOnKeyDown("g_cheat_editor")]
    public void CheatEditor()
    {
        if (Settings.Instance.CheatsEnabled)
        {
            stage.HUD.ShowReproductionDialog();
        }
    }
}
