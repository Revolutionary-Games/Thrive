using System;
using Godot;

/// <summary>
///   Handles key input in a multiplayer microbe stage.
/// </summary>
public class PlayerMicrobeArenaInput : MultiplayerInputBase
{
    private Random random = new();

    private NetworkInputVars cachedInput;
    private Vector2 lastMousePosition;

    protected MicrobeArena Stage => MultiplayerStage as MicrobeArena ??
        throw new InvalidOperationException("Stage hasn't been set");

    // TODO: when using controller movement this should be screen relative movement by default
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

        if (Stage.Player != null)
        {
            if (Stage.Player.State == MicrobeState.Unbinding)
            {
                Stage.Player.MovementDirection = Vector3.Zero;
                return;
            }

            var movement = new Vector3(leftRightMovement, 0, forwardMovement);

            // TODO: change this line to only normalize when length exceeds 1 to make slowly moving with a controller
            // work
            var direction = autoMove ? new Vector3(0, 0, -1) : movement.Normalized();

            cachedInput.EncodeMovementDirection(direction);
            cachedInput.WorldLookAtPoint = Stage.Camera.CursorWorldPos;
            lastMousePosition = GetViewport().GetMousePosition();
        }
    }

    [RunOnKeyDown("g_fire_toxin")]
    public bool EmitToxin()
    {
        cachedInput.Bools |= (byte)Microbe.InputFlag.EmitToxin;
        return false;
    }

    [RunOnKeyUp("g_fire_toxin")]
    public void StopEmittingToxin()
    {
        cachedInput.Bools &= (byte)~Microbe.InputFlag.EmitToxin;
    }

    [RunOnKeyDown("g_secrete_slime")]
    public bool SecreteSlime()
    {
        cachedInput.Bools |= (byte)Microbe.InputFlag.SecreteSlime;
        return false;
    }

    [RunOnKeyUp("g_secrete_slime")]
    public void StopSecretingSlime()
    {
        cachedInput.Bools &= (byte)~Microbe.InputFlag.SecreteSlime;
    }

    [RunOnKeyDown("g_toggle_engulf")]
    public void ToggleEngulf()
    {
        cachedInput.Bools ^= (byte)Microbe.InputFlag.Engulf;
    }

    [RunOnKeyChange("g_toggle_scoreboard")]
    public void ShowInfoScreen(bool heldDown)
    {
        Stage.HUD.ToggleInfoScreen();
    }

    [RunOnKeyChange("g_toggle_map")]
    public void ShowMap(bool heldDown)
    {
        Stage.HUD.ToggleMap();
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

    [RunOnKeyDown("g_cheat_editor")]
    public void CheatEditor()
    {
        if (Settings.Instance.CheatsEnabled)
        {
            Stage.HUD.ShowReproductionDialog();
        }
    }

    protected override bool ShouldApplyInput(NetworkInputVars sampled)
    {
        if (base.ShouldApplyInput(sampled))
        {
            if (Stage.LocalPlayerVars.GetVar<bool>("editor"))
                return false;

            if (GetViewport().GetMousePosition() != lastMousePosition)
                return true;

            if (sampled.MovementDirection == 0 && PreviousInput.MovementDirection != 0)
                return true;

            if (sampled.Bools == 0 && PreviousInput.Bools != 0)
                return true;

            return sampled.MovementDirection != 0 || sampled.Bools != 0;
        }

        if ((sampled.Bools & (byte)Microbe.InputFlag.SecreteSlime) != 0)
            return true;

        return false;
    }

    protected override NetworkInputVars SampleInput()
    {
        return cachedInput;
    }

    private void SpawnCheatCloud(string name, float delta)
    {
        SpawnHelpers.SpawnCloud(Stage.Clouds, Stage.Camera.CursorWorldPos,
            SimulationParameters.Instance.GetCompound(name), Constants.CLOUD_CHEAT_DENSITY * delta, random);
    }
}
