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

    // // All the input actions
    private bool forward = false;
    private bool backwards = false;
    private bool left = false;
    private bool right = false;

    private bool cheatGlucose = false;
    private bool cheatAmmonia = false;
    private bool cheatPhosphates = false;

    public override void _Ready()
    {
        stage = (MicrobeStage)GetParent();
    }

    public override void _Input(InputEvent @event)
    {
        var settings = Settings.Instance;

        if (@event.IsActionPressed("g_move_forward"))
        {
            forward = true;
        }
        else if (@event.IsActionReleased("g_move_forward"))
        {
            forward = false;
        }

        if (@event.IsActionPressed("g_move_backwards"))
        {
            backwards = true;
        }
        else if (@event.IsActionReleased("g_move_backwards"))
        {
            backwards = false;
        }

        if (@event.IsActionPressed("g_move_left"))
        {
            left = true;
        }
        else if (@event.IsActionReleased("g_move_left"))
        {
            left = false;
        }

        if (@event.IsActionPressed("g_move_right"))
        {
            right = true;
        }
        else if (@event.IsActionReleased("g_move_right"))
        {
            right = false;
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
            if (stage.Player != null)
            {
                stage.Player.EmitToxin();
            }
        }
    }

    public override void _Notification(int focus)
    {
        // If the window goes out of focus, we don't receive the key released events
        // We reset our held down keys if the player tabs out while pressing a key
        if (focus == MainLoop.NotificationWmFocusOut)
        {
            forward = false;
            backwards = false;
            left = false;
            right = false;
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
