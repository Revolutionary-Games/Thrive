using System;
using System.Linq;
using Components;
using DefaultEcs;
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

        if (stage.HasPlayer)
        {
            var player = stage.Player;

            ref var position = ref player.Get<WorldPosition>();
            ref var control = ref player.Get<MicrobeControl>();

            if (control.State == MicrobeState.Unbinding)
            {
                // It's probably fine to not update the tutorial state here with events as this state doesn't last
                // that long and the player needs a pretty long time to get so far in the game as to get here
                control.MovementDirection = Vector3.Zero;
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
                control.LookAtPoint = position.Position + new Vector3(0, 0, -10);
            }
            else
            {
                control.LookAtPoint = stage.Camera.CursorWorldPos;
            }

            // Rotate the inputs when we want to use screen relative movement to make it happen
            if (screenRelative)
            {
                // Rotate the opposite of the player orientation to get back to screen (as when applying movement
                // vector the normal rotation is used to rotate the movement direction so these two operations cancel
                // out)
                movement = position.Rotation.Inverse().Xform(movement);
            }

            if (autoMove)
            {
                control.MovementDirection = new Vector3(0, 0, -1);
            }
            else
            {
                // We only normalize when the length is over to make moving slowly with a controller work
                control.MovementDirection = movement.Length() > 1 ? movement.Normalized() : movement;
            }

            stage.TutorialState.SendEvent(TutorialEventType.MicrobePlayerMovement,
                new MicrobeMovementEventArgs(screenRelative, control.MovementDirection,
                    control.LookAtPoint - position.Position), this);
        }
    }

    [RunOnKeyDown("g_fire_toxin")]
    public void EmitToxin()
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();
        ref var compoundStorage = ref stage.Player.Get<CompoundStorage>();

        control.EmitToxin(ref stage.Player.Get<OrganelleContainer>(), compoundStorage.Compounds, stage.Player);
    }

    [RunOnKey("g_secrete_slime")]
    public void SecreteSlime(float delta)
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();

        control.QueueSecreteSlime(ref stage.Player.Get<OrganelleContainer>(), stage.Player, delta);
    }

    [RunOnKeyDown("g_toggle_engulf")]
    public void ToggleEngulf()
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();
        ref var cellProperties = ref stage.Player.Get<CellProperties>();

        if (control.State == MicrobeState.Engulf)
        {
            control.State = MicrobeState.Normal;
        }
        else if (cellProperties.CanEngulfInColony(stage.Player))
        {
            control.State = MicrobeState.Engulf;
        }
    }

    [RunOnKeyDown("g_toggle_binding")]
    public void ToggleBinding()
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();
        ref var organelles = ref stage.Player.Get<OrganelleContainer>();

        if (control.State == MicrobeState.Binding)
        {
            control.State = MicrobeState.Normal;
        }
        else if (organelles.HasBindingAgent)
        {
            control.State = MicrobeState.Binding;
        }
    }

    [RunOnKeyDown("g_toggle_unbinding")]
    public void ToggleUnbinding()
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();

        if (control.State == MicrobeState.Unbinding)
        {
            stage.HUD.HintText = string.Empty;
            control.State = MicrobeState.Normal;
        }
        else if (stage.Player.Has<Components.MicrobeColony>() && stage.GameWorld.PlayerSpecies is MicrobeSpecies)
        {
            stage.HUD.HintText = TranslationServer.Translate("UNBIND_HELP_TEXT");
            control.State = MicrobeState.Unbinding;
        }
    }

    [RunOnKeyDown("g_unbind_all")]
    public void UnbindAll()
    {
        if (!stage.HasPlayer)
            return;

        if (stage.Player.Has<Components.MicrobeColony>())
        {
            throw new NotImplementedException();

            // stage.Player?.UnbindAll();
        }
    }

    [RunOnKeyDown("g_perform_unbinding", Priority = 1)]
    public bool AcceptUnbind()
    {
        if (!stage.HasPlayer)
            return false;

        ref var control = ref stage.Player.Get<MicrobeControl>();

        if (control.State != MicrobeState.Unbinding)
            return false;

        var target = stage.HoverInfo.Entities.FirstOrDefault();
        if (target == default || !target.IsAlive)
            return false;

        // This checks for the microbe species member as all cell colonies are merged to have a single physics body
        // so this always hits the colony lead cell
        if (!target.IsAlive || !target.Has<MicrobeSpeciesMember>())
            return false;

        // If didn't hit a cell colony, can't do anything
        if (!target.Has<Components.MicrobeColony>())
            return false;

        if (!stage.HoverInfo.GetRaycastData(target, out var raycastData))
            return false;

        throw new NotImplementedException();

        // var actualMicrobe = microbe.GetMicrobeFromShape(raycastData.Value.Shape);
        // if (actualMicrobe == null)
        //     return false;
        //
        // RemoveCellFromColony(actualMicrobe);

        stage.HUD.HintText = string.Empty;
        return true;
    }

    [RunOnKeyDown("g_pack_commands")]
    public bool ShowSignalingCommandsMenu()
    {
        if (!stage.HasPlayer)
            return false;

        ref var organelles = ref stage.Player.Get<OrganelleContainer>();

        if (!organelles.HasSignalingAgent)
            return false;

        stage.HUD.ShowSignalingCommandsMenu(stage.Player);

        // We need to not consume the input, otherwise the key up for this will not run
        return false;
    }

    [RunOnKeyUp("g_pack_commands")]
    public void CloseSignalingCommandsMenu()
    {
        var command = stage.HUD.SelectSignalCommandIfOpen();

        if (stage.HasPlayer)
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

    private void RemoveCellFromColony(Entity target)
    {
        throw new NotImplementedException();

        // if (target.Colony == null)
        // {
        //     GD.PrintErr("Target microbe is not a part of colony");
        //     return;
        // }
        //
        // target.Colony.RemoveFromColony(target);
    }

    private void SpawnCheatCloud(string name, float delta)
    {
        float multiplier = 1.0f;

        // To make cheating easier in multicellular with large cell layouts
        if (stage.GameWorld.PlayerSpecies is not MicrobeSpecies)
            multiplier = 4;

        stage.Clouds.AddCloud(SimulationParameters.Instance.GetCompound(name),
            Constants.CLOUD_CHEAT_DENSITY * delta * multiplier, stage.Camera.CursorWorldPos);
    }
}
