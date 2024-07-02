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
public partial class PlayerMicrobeInput : NodeWithInput
{
    private readonly MicrobeMovementEventArgs cachedEventArgs = new(true, Vector3.Zero);

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
    public void OnMovement(double delta, float forwardMovement, float leftRightMovement, ActiveInputMethod inputMethod)
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
                // Unbinding mode movement is canceled by the binding system now
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
                movement = position.Rotation.Inverse() * movement;
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

            // A cached event args is used as otherwise this generates quite a large fraction of allocated objects
            // by the game
            cachedEventArgs.ReuseEvent(screenRelative, control.MovementDirection);
            stage.TutorialState.SendEvent(TutorialEventType.MicrobePlayerMovement, cachedEventArgs, this);
        }
    }

    [RunOnKeyDown("g_fire_siderophore")]
    public void EmitIron()
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();

        control.EmitIron(ref stage.Player.Get<OrganelleContainer>(), stage.Player);
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
    public void SecreteSlime(double delta)
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();

        control.QueueSecreteSlime(ref stage.Player.Get<OrganelleContainer>(), stage.Player, (float)delta);
    }

    [RunOnKeyDown("g_toggle_engulf")]
    public void ToggleEngulf()
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();
        ref var cellProperties = ref stage.Player.Get<CellProperties>();

        var currentState = control.State;

        if (stage.Player.Has<MicrobeColony>())
        {
            ref var colony = ref stage.Player.Get<MicrobeColony>();

            currentState = colony.ColonyState;
        }

        if (currentState == MicrobeState.Engulf)
        {
            control.SetStateColonyAware(stage.Player, MicrobeState.Normal);
        }
        else if (cellProperties.CanEngulfInColony(stage.Player))
        {
            control.SetStateColonyAware(stage.Player, MicrobeState.Engulf);
        }
    }

    [RunOnKeyDown("g_eject_engulfed")]
    public void EjectAllEngulfed()
    {
        if (!stage.HasAlivePlayer)
            return;

        ref var engulfer = ref stage.Player.Get<Engulfer>();

        if (engulfer.EngulfedObjects is { Count: > 0 })
        {
            foreach (var engulfedObject in engulfer.EngulfedObjects)
            {
                engulfer.EjectEngulfable(ref engulfedObject.Get<Engulfable>());
            }
        }
    }

    [RunOnKeyDown("g_toggle_binding")]
    public void ToggleBinding()
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();
        ref var organelles = ref stage.Player.Get<OrganelleContainer>();

        // This doesn't check colony data as the player cell is always able to bind when in a colony so the state
        // should not be able to be out of sync

        if (control.State is MicrobeState.Binding or MicrobeState.Unbinding)
        {
            control.SetStateColonyAware(stage.Player, MicrobeState.Normal);
        }
        else if (organelles.HasBindingAgent && stage.GameWorld.PlayerSpecies is MicrobeSpecies)
        {
            // Only microbe species can bind new members, multicellular ones cannot

            control.SetStateColonyAware(stage.Player, MicrobeState.Binding);
        }
    }

    [RunOnKeyDown("g_toggle_unbinding")]
    public void ToggleUnbinding()
    {
        if (!stage.HasPlayer)
            return;

        ref var control = ref stage.Player.Get<MicrobeControl>();

        if (control.State is MicrobeState.Unbinding or MicrobeState.Binding)
        {
            stage.HUD.HintText = string.Empty;
            control.SetStateColonyAware(stage.Player, MicrobeState.Normal);
        }
        else if (stage.Player.Has<MicrobeColony>() && stage.GameWorld.PlayerSpecies is MicrobeSpecies)
        {
            stage.HUD.HintText = Localization.Translate("UNBIND_HELP_TEXT");
            control.SetStateColonyAware(stage.Player, MicrobeState.Unbinding);

            ref var callbacks = ref stage.Player.Get<MicrobeEventCallbacks>();

            callbacks.OnUnbindEnabled?.Invoke(stage.Player);
        }
    }

    [RunOnKeyDown("g_unbind_all")]
    public void UnbindAll()
    {
        if (!stage.HasPlayer)
            return;

        if (stage.Player.Has<MicrobeColony>())
        {
            if (!MicrobeColonyHelpers.UnbindAllOutsideGameUpdate(stage.Player, stage.WorldSimulation))
            {
                GD.PrintErr("Failed to unbind the player");
            }
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

        if (!target.IsAlive || !target.Has<MicrobeSpeciesMember>())
            return false;

        // If didn't hit a cell colony, can't do anything
        if (!target.Has<MicrobeColony>() && !target.Has<MicrobeColonyMember>())
            return false;

        RemoveCellFromColony(target);

        // Removing a colony member should reset the microbe mode so this text should be hidden anyway soon, but
        // apparently we wanted extra guarantee here
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

        if (command != null && stage.HasPlayer)
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
    public void CheatGlucose(double delta)
    {
        if (Settings.Instance.CheatsEnabled)
        {
            SpawnCheatCloud("glucose", delta);
        }
    }

    [RunOnKey("g_cheat_ammonia")]
    public void CheatAmmonia(double delta)
    {
        if (Settings.Instance.CheatsEnabled)
        {
            SpawnCheatCloud("ammonia", delta);
        }
    }

    [RunOnKey("g_cheat_phosphates")]
    public void CheatPhosphates(double delta)
    {
        if (Settings.Instance.CheatsEnabled)
        {
            SpawnCheatCloud("phosphates", delta);
        }
    }

    private void RemoveCellFromColony(Entity target)
    {
        if (!MicrobeColonyHelpers.UnbindAllOutsideGameUpdate(target, stage.WorldSimulation))
        {
            GD.PrintErr("Target microbe failed to unbind");
        }
    }

    private void SpawnCheatCloud(string name, double delta)
    {
        float multiplier = 1.0f;

        // To make cheating easier in multicellular with large cell layouts
        if (stage.GameWorld.PlayerSpecies is not MicrobeSpecies)
            multiplier = 4;

        stage.Clouds.AddCloud(SimulationParameters.Instance.GetCompound(name),
            (float)(Constants.CLOUD_CHEAT_DENSITY * delta * multiplier), stage.Camera.CursorWorldPos);
    }
}
