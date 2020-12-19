using System;
using Godot;

/// <summary>
///   Handles key input in the microbe stage
/// </summary>
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
            var movement = new Vector3(leftRightMovement, 0, forwardMovement);

            stage.Player.MovementDirection = autoMove ? new Vector3(0, 0, -1) : movement.Normalized();

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

        stage.Player.EngulfMode = !stage.Player.EngulfMode;

        if (stage.Player.EngulfMode)
            stage.Player.BindingMode = false;
    }

    [RunOnKeyDown("g_toggle_binding")]
    public void ToggleBinding()
    {
        if (stage.Player == null)
            return;

        stage.Player.AnyInBindingMode = !stage.Player.AnyInBindingMode;

        if (stage.Player.AnyInBindingMode)
            stage.Player.EngulfMode = false;
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

    private void SpawnCheatCloud(string name, float delta)
    {
        stage.Clouds.AddCloud(SimulationParameters.Instance.GetCompound(name),
            8000.0f * delta, stage.Camera.CursorWorldPos);
    }
}
