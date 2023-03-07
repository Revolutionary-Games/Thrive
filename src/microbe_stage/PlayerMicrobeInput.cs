using System;
using System.Linq;
using Godot;

/// <summary>
///   Handles key input in the microbe stage
/// </summary>
/// <remarks>
///   <para>
///     Note that callbacks from other places directly call some methods in this class, so
///     an extra care should be taken while modifying the methods as otherwise some stuff
///     may no longer work.
///   </para>
/// </remarks>
public class PlayerMicrobeInput : NodeWithInput
{
    private bool autoMove;

#pragma warning disable CA2213 // this is our parent object

    /// <summary>
    ///   A reference to the stage is kept to get to the player object and also the cloud spawning.
    /// </summary>
    private MicrobeStage stage = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        // Not the cleanest that the parent has to be MicrobeState type...
        stage = (MicrobeStage)GetParent();
    }

    [RunOnKeyDown("g_hold_forward")]
    public void ToggleAutoMove()
    {
        autoMove = !autoMove;
    }

    [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
    [RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
    [RunOnAxisGroup(InvokeAlsoWithNoInput = true, TrackInputMethod = true)]
    public void OnMovement(float delta, float forwardMovement, float leftRightMovement, ActiveInputMethod inputMethod)
    {
        _ = delta;
        const float epsilon = 0.01f;

        // Reset auto move if a key was pressed
        if (Math.Abs(forwardMovement) + Math.Abs(leftRightMovement) > epsilon)
        {
            autoMove = false;
        }

        var player = stage.Player;
        if (player != null)
        {
            if (player.State == MicrobeState.Unbinding)
            {
                // It's probably fine to not update the tutorial state here with events as this state doesn't last
                // that long and the player needs a pretty long time to get so far in the game as to get here
                player.MovementDirection = Vector3.Zero;
                return;
            }

            bool screenRelative = false;
            var settingValue = Settings.Instance.TwoDimensionalMovement.Value;

            if (settingValue == TwoDimensionalMovementMode.ScreenRelative ||
                (settingValue == TwoDimensionalMovementMode.Automatic && inputMethod == ActiveInputMethod.Controller))
            {
                screenRelative = true;
            }

            var movement = new Vector3(leftRightMovement, 0, forwardMovement);

            if (inputMethod == ActiveInputMethod.Controller)
            {
                // TODO: look direction for controller input  https://github.com/Revolutionary-Games/Thrive/issues/4034
                player.LookAtPoint = player.GlobalTranslation + new Vector3(0, 0, -10);
            }
            else
            {
                player.LookAtPoint = stage.Camera.CursorWorldPos;
            }

            // Rotate the inputs when we want to use screen relative movement to make it happen
            if (screenRelative)
            {
                // Rotate the opposite of the player orientation to get back to screen
                movement = player.GlobalTransform.basis.Quat().Inverse().Xform(movement);
            }

            if (autoMove)
            {
                player.MovementDirection = new Vector3(0, 0, -1);
            }
            else
            {
                // We only normalize when the length is over to make moving slowly with a controller work
                player.MovementDirection = movement.Length() > 1 ? movement.Normalized() : movement;
            }

            stage.TutorialState.SendEvent(TutorialEventType.MicrobePlayerMovement,
                new MicrobeMovementEventArgs(screenRelative, player.MovementDirection,
                    player.LookAtPoint - player.GlobalTranslation), this);
        }
    }

    [RunOnKeyDown("g_fire_toxin")]
    public void EmitToxin()
    {
        stage.Player?.EmitToxin();
    }

    [RunOnKey("g_secrete_slime")]
    public void SecreteSlime(float delta)
    {
        stage.Player?.QueueSecreteSlime(delta);
    }

    [RunOnKeyDown("g_toggle_engulf")]
    public void ToggleEngulf()
    {
        if (stage.Player == null)
            return;

        if (stage.Player.State == MicrobeState.Engulf)
        {
            stage.Player.State = MicrobeState.Normal;
        }
        else if (stage.Player.CanEngulfInColony())
        {
            stage.Player.State = MicrobeState.Engulf;
        }
    }

    [RunOnKeyDown("g_toggle_binding")]
    public void ToggleBinding()
    {
        if (stage.Player == null)
            return;

        if (stage.Player.State == MicrobeState.Binding)
        {
            stage.Player.State = MicrobeState.Normal;
        }
        else if (stage.Player.CanBind)
        {
            stage.Player.State = MicrobeState.Binding;
        }
    }

    [RunOnKeyDown("g_toggle_unbinding")]
    public void ToggleUnbinding()
    {
        if (stage.Player == null)
            return;

        if (stage.Player.State == MicrobeState.Unbinding)
        {
            stage.HUD.HintText = string.Empty;
            stage.Player.State = MicrobeState.Normal;
        }
        else if (stage.Player.Colony != null && !stage.Player.IsMulticellular)
        {
            stage.HUD.HintText = TranslationServer.Translate("UNBIND_HELP_TEXT");
            stage.Player.State = MicrobeState.Unbinding;
        }
    }

    [RunOnKeyDown("g_unbind_all")]
    public void UnbindAll()
    {
        stage.Player?.UnbindAll();
    }

    [RunOnKeyDown("g_perform_unbinding", Priority = 1)]
    public bool AcceptUnbind()
    {
        if (stage.Player?.State != MicrobeState.Unbinding)
            return false;

        var inspectables = stage.HoverInfo.InspectableEntities.ToList();
        if (inspectables.Count == 0)
            return false;

        var target = inspectables[0];
        if (target is not Microbe microbe)
            return false;

        var raycastData = stage.HoverInfo.GetRaycastData(target);
        if (raycastData == null)
            return false;

        var actualMicrobe = microbe.GetMicrobeFromShape(raycastData.Value.Shape);
        if (actualMicrobe == null)
            return false;

        RemoveCellFromColony(actualMicrobe);

        stage.HUD.HintText = string.Empty;
        return true;
    }

    [RunOnKeyDown("g_pack_commands")]
    public bool ShowSignalingCommandsMenu()
    {
        if (stage.Player?.HasSignalingAgent != true)
            return false;

        stage.HUD.ShowSignalingCommandsMenu(stage.Player);

        // We need to not consume the input, otherwise the key up for this will not run
        return false;
    }

    [RunOnKeyUp("g_pack_commands")]
    public void CloseSignalingCommandsMenu()
    {
        var command = stage.HUD.SelectSignalCommandIfOpen();

        if (stage.Player != null)
            stage.HUD.ApplySignalCommand(command, stage.Player);
    }

    [RunOnKeyDown("g_cheat_editor")]
    public void CheatEditor()
    {
        if (Settings.Instance.CheatsEnabled)
        {
            stage.HUD.ShowReproductionDialog();
        }
    }

    [RunOnKey("g_cheat_glucose")]
    public void CheatGlucose(float delta)
    {
        if (Settings.Instance.CheatsEnabled)
        {
            SpawnCheatCloud("glucose", delta);
        }
    }

    [RunOnKey("g_cheat_ammonia")]
    public void CheatAmmonia(float delta)
    {
        if (Settings.Instance.CheatsEnabled)
        {
            SpawnCheatCloud("ammonia", delta);
        }
    }

    [RunOnKey("g_cheat_phosphates")]
    public void CheatPhosphates(float delta)
    {
        if (Settings.Instance.CheatsEnabled)
        {
            SpawnCheatCloud("phosphates", delta);
        }
    }

    private void RemoveCellFromColony(Microbe target)
    {
        if (target.Colony == null)
        {
            GD.PrintErr("Target microbe is not a part of colony");
            return;
        }

        target.Colony.RemoveFromColony(target);
    }

    private void SpawnCheatCloud(string name, float delta)
    {
        float multiplier = 1.0f;

        // To make cheating easier in multicellular with large cell layouts
        if (stage.Player?.IsMulticellular == true)
            multiplier = 4;

        stage.Clouds.AddCloud(SimulationParameters.Instance.GetCompound(name),
            Constants.CLOUD_CHEAT_DENSITY * delta * multiplier, stage.Camera.CursorWorldPos);
    }
}
