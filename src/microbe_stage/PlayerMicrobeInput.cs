using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles key input in the microbe stage
/// </summary>
public class PlayerMicrobeInput : Node
{
    // Inputs all grouped for easy focus loss and input passing
    private readonly InputGroup inputs;

    private readonly InputAxis forwardBackAxis;
    private readonly InputAxis leftRightAxis;

    // Access to individual inputs

    private readonly InputTrigger forward = new InputTrigger("g_move_forward");
    private readonly InputTrigger backwards = new InputTrigger("g_move_backwards");
    private readonly InputTrigger left = new InputTrigger("g_move_left");
    private readonly InputTrigger right = new InputTrigger("g_move_right");

    // Other inputs

    private readonly InputTrigger cheatEditor = new InputTrigger("g_cheat_editor");
    private readonly InputBool cheatGlucose = new InputBool("g_cheat_glucose");
    private readonly InputBool cheatAmmonia = new InputBool("g_cheat_ammonia");
    private readonly InputBool cheatPhosphates = new InputBool("g_cheat_phosphates");

    private readonly InputTrigger toggleEngulf = new InputTrigger("g_toggle_engulf");

    private readonly InputBool fireToxin = new InputBool("g_fire_toxin");
    private readonly InputToggle autoMove = new InputToggle("g_hold_forward");

    /// <summary>
    ///   A reference to the stage is kept to get to the player object
    ///   and also the cloud spawning.
    /// </summary>
    private MicrobeStage stage;

    public PlayerMicrobeInput()
    {
        forwardBackAxis = new InputAxis(new List<(InputBool input, int associatedValue)>
        {
            (forward, -1),
            (backwards, 1),
        });

        leftRightAxis = new InputAxis(new List<(InputBool input, int associatedValue)>
        {
            (left, -1),
            (right, 1),
        });

        inputs = new InputGroup(new List<IInputReceiver>
        {
            forwardBackAxis,
            leftRightAxis,
            autoMove,
            toggleEngulf,
            fireToxin,
            cheatEditor,
            cheatGlucose,
            cheatAmmonia,
            cheatPhosphates,
        });
    }

    public override void _Ready()
    {
        // Not the cleanest that the parent has to be MicrobeState type...
        stage = (MicrobeStage)GetParent();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (inputs.CheckInput(@event))
        {
            GetTree().SetInputAsHandled();
        }
    }

    public override void _Notification(int focus)
    {
        // If the window goes out of focus, we don't receive the key released events
        // We reset our held down keys if the player tabs out while pressing a key
        if (focus == MainLoop.NotificationWmFocusOut)
        {
            inputs.FocusLost();
        }
    }

    public override void _Process(float delta)
    {
        inputs.OnFrameChanged();

        var settings = Settings.Instance;

        float forwardMovement = forwardBackAxis.CurrentValue;

        if (autoMove.ToggledOn)
        {
            forwardMovement = -1;
        }

        var movement = new Vector3(leftRightAxis.CurrentValue, 0, forwardMovement);

        // Reset auto move if a key was pressed
        if (forward.ReadTrigger() || backwards.ReadTrigger() || left.ReadTrigger() || right.ReadTrigger())
        {
            autoMove.ToggledOn = false;
        }

        if (stage.Player != null)
        {
            stage.Player.MovementDirection = movement.Normalized();
            stage.Player.LookAtPoint = stage.Camera.CursorWorldPos;
        }

        if (fireToxin.Pressed)
        {
            stage.Player?.EmitToxin();
        }

        if (toggleEngulf.ReadTrigger())
        {
            if (stage.Player != null)
            {
                stage.Player.EngulfMode = !stage.Player.EngulfMode;
            }
        }

        if (settings.CheatsEnabled && cheatEditor.ReadTrigger())
        {
            stage.HUD.ShowReproductionDialog();
        }

        if (settings.CheatsEnabled && cheatAmmonia.Pressed)
        {
            SpawnCheatCloud("ammonia", delta);
        }

        if (settings.CheatsEnabled && cheatGlucose.Pressed)
        {
            SpawnCheatCloud("glucose", delta);
        }

        if (settings.CheatsEnabled && cheatPhosphates.Pressed)
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
