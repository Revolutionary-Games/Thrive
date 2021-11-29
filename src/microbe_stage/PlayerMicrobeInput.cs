using System;
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

    /// <summary>
    ///   A reference to the stage is kept to get to the player object
    ///   and also the cloud spawning.
    /// </summary>
    private MicrobeStage stage;

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
            if (stage.Player.State == Microbe.MicrobeState.Unbinding)
            {
                stage.Player.MovementDirection = Vector2.Zero;
                return;
            }

            var movement = new Vector2(leftRightMovement, forwardMovement);

            stage.Player.MovementDirection = autoMove ? new Vector2(0, -1) : movement.Normalized();

            stage.Player.LookAtPoint = stage.Camera.CursorWorldPos;
        }
    }

    [RunOnKeyDown("g_fire_toxin")]
    public void EmitToxin()
    {
        stage.Player?.EmitToxin();
    }

    [RunOnKeyDown("g_toggle_engulf")]
    public void ToggleEngulf()
    {
        if (stage.Player == null)
            return;

        if (stage.Player.State == Microbe.MicrobeState.Engulf)
        {
            stage.Player.State = Microbe.MicrobeState.Normal;
        }
        else if (!stage.Player.Membrane.Type.CellWall)
        {
            stage.Player.State = Microbe.MicrobeState.Engulf;
        }
    }

    [RunOnKeyDown("g_toggle_binding")]
    public void ToggleBinding()
    {
        if (stage.Player == null)
            return;

        if (stage.Player.State == Microbe.MicrobeState.Binding)
        {
            stage.Player.State = Microbe.MicrobeState.Normal;
        }
        else if (stage.Player.CanBind)
        {
            stage.Player.State = Microbe.MicrobeState.Binding;
        }
    }

    [RunOnKeyDown("g_toggle_unbinding")]
    public void ToggleUnbinding()
    {
        if (stage.Player == null)
            return;

        if (stage.Player.State == Microbe.MicrobeState.Unbinding)
        {
            stage.HUD.HintText = string.Empty;
            stage.Player.State = Microbe.MicrobeState.Normal;
        }
        else if (stage.Player.Colony != null)
        {
            stage.HUD.HintText = TranslationServer.Translate("UNBIND_HELP_TEXT");
            stage.Player.State = Microbe.MicrobeState.Unbinding;
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
        if (stage.Player?.State != Microbe.MicrobeState.Unbinding)
            return false;

        if (stage.HoverInfo.HoveredMicrobes.Count == 0)
            return false;

        var target = stage.HoverInfo.HoveredMicrobes[0];
        RemoveCellFromColony(target);

        stage.HUD.HintText = string.Empty;
        return true;
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
        target.Colony.RemoveFromColony(target);
    }

    private void SpawnCheatCloud(string name, float delta)
    {
        stage.Clouds.AddCloud(SimulationParameters.Instance.GetCompound(name),
            Constants.CLOUD_CHEAT_DENSITY * delta, stage.Camera.CursorWorldPos);
    }
}
