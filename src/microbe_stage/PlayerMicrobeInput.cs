using Godot;

/// <summary>
///   Handles key input in the microbe stage
/// </summary>
public class PlayerMicrobeInput : Node
{
    /// <summary>
    ///   A reference to the stage is kept to get to the player object
    ///   and also the cloud spawning.
    /// </summary>
    private MicrobeStage stage;

    /// <summary>
    ///  Whether or not the player is allowed to use auto-move
    /// </summary>
    private bool autoMoveAllowed = true;

    // // All the input actions
    private bool forward;
    private bool backwards;
    private bool left;
    private bool right;

    /// <summary>
    ///  Whether or not player is stationary. Used to adjust cloud spawn rate
    /// </summary>
    private bool isPlayerStationary = true;

    private bool cheatGlucose;
    private bool cheatAmmonia;
    private bool cheatPhosphates;

    public override void _Ready()
    {
        stage = (MicrobeStage)GetParent();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        var settings = Settings.Instance;

        if (@event.IsActionPressed("g_hold_forward") && autoMoveAllowed)
        {
            forward = !forward;
        }

        if (@event.IsActionPressed("g_move_forward"))
        {
            forward = true;
            autoMoveAllowed = false;
            isPlayerStationary = false;
        }
        else if (@event.IsActionReleased("g_move_forward"))
        {
            forward = false;
            autoMoveAllowed = true;
            isPlayerStationary = true;
        }

        if (@event.IsActionPressed("g_move_backwards"))
        {
            backwards = true;
            autoMoveAllowed = false;
            isPlayerStationary = false;
        }
        else if (@event.IsActionReleased("g_move_backwards"))
        {
            backwards = false;
            autoMoveAllowed = true;
            isPlayerStationary = true;
        }

        if (@event.IsActionPressed("g_move_left"))
        {
            left = true;
            autoMoveAllowed = false;
            isPlayerStationary = false;
        }
        else if (@event.IsActionReleased("g_move_left"))
        {
            left = false;
            autoMoveAllowed = true;
            isPlayerStationary = true;
        }

        if (@event.IsActionPressed("g_move_right"))
        {
            right = true;
            autoMoveAllowed = false;
            isPlayerStationary = false;
        }
        else if (@event.IsActionReleased("g_move_right"))
        {
            right = false;
            autoMoveAllowed = true;
            isPlayerStationary = true;
        }

        if (settings.CheatsEnabled && @event.IsActionPressed("g_cheat_editor"))
        {
            stage.HUD.ShowReproductionDialog();
        }

        if (settings.CheatsEnabled && @event.IsActionPressed("g_cheat_glucose"))
        {
            cheatGlucose = true;
        }
        else if (@event.IsActionReleased("g_cheat_glucose"))
        {
            cheatGlucose = false;
        }

        if (settings.CheatsEnabled && @event.IsActionPressed("g_cheat_ammonia"))
        {
            cheatAmmonia = true;
        }
        else if (@event.IsActionReleased("g_cheat_ammonia"))
        {
            cheatAmmonia = false;
        }

        if (settings.CheatsEnabled && @event.IsActionPressed("g_cheat_phosphates"))
        {
            cheatPhosphates = true;
        }
        else if (@event.IsActionReleased("g_cheat_phosphates"))
        {
            cheatPhosphates = false;
        }

        if (@event.IsActionPressed("g_toggle_engulf"))
        {
            if (stage.Player != null)
            {
                stage.Player.EngulfMode = !stage.Player.EngulfMode;
            }
        }

        if (@event.IsActionPressed("g_fire_toxin", true))
        {
            stage.Player?.EmitToxin();
        }
    }

    public override void _Notification(int focus)
    {
        // If the window goes out of focus, we don't receive the key released events
        // We reset our held down keys if the player tabs out while pressing a key
        if (focus == MainLoop.NotificationWmFocusOut)
        {
            if (!autoMoveAllowed)
            {
                forward = false;
                backwards = false;
                left = false;
                right = false;
            }
        }
    }

    public override void _Process(float delta)
    {
        var movement = new Vector3(0, 0, 0);

        if (forward)
        {
            movement.z -= 1;
        }

        if (backwards)
        {
            movement.z += 1;
        }

        if (left)
        {
            movement.x -= 1;
        }

        if (right)
        {
            movement.x += 1;
        }

        if (stage.Player != null)
        {
            stage.Player.MovementDirection = movement.Normalized();
            stage.Player.LookAtPoint = stage.Camera.CursorWorldPos;
            stage.isPlayerStationary = isPlayerStationary;
        }

        if (cheatAmmonia)
        {
            SpawnCheatCloud("ammonia", delta);
        }

        if (cheatGlucose)
        {
            SpawnCheatCloud("glucose", delta);
        }

        if (cheatPhosphates)
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
