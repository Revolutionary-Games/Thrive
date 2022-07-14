using System;

/// <summary>
///   Handles input for the (late) multicellular stage
/// </summary>
public class PlayerMulticellularInput : NodeWithInput
{
    private bool autoMove;

    private MulticellularStage stage = null!;

    public override void _Ready()
    {
        // Not the cleanest that the parent has to be MulticellularStage type...
        stage = (MulticellularStage)GetParent();
    }

    [RunOnKeyDown("g_hold_forward")]
    public void ToggleAutoMove()
    {
        autoMove = !autoMove;
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

        // TODO: implement
        /*if (stage.Player != null)
        {
            if (stage.Player.State == Microbe.MicrobeState.Unbinding)
            {
                stage.Player.MovementDirection = Vector3.Zero;
                return;
            }

            var movement = new Vector3(leftRightMovement, 0, forwardMovement);

            stage.Player.MovementDirection = autoMove ? new Vector3(0, 0, -1) : movement.Normalized();

            stage.Player.LookAtPoint = stage.Camera.CursorWorldPos;
        }*/
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
