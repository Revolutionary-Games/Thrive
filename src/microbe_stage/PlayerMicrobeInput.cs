using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles key input in the microbe stage
/// </summary>
public class PlayerMicrobeInput : Node
{
    public PlayerMicrobeInput()
    {
        RunOnInputAttribute.InputClasses.Add(this);
    }

    /*private readonly InputAxis forwardBackAxis;
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

        Inputs = new InputGroup(new List<IInputReceiver>
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

    protected override InputGroup Inputs { get; }

    public override void _Process(float delta)
    {
        base._Process(delta);

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

        if (Environment.Player != null)
        {
            Environment.Player.MovementDirection = movement.Normalized();
            Environment.Player.LookAtPoint = Environment.Camera.CursorWorldPos;
        }

        if (fireToxin.Pressed)
        {
            Environment.Player?.EmitToxin();
        }

        if (toggleEngulf.ReadTrigger())
        {
            if (Environment.Player != null)
            {
                Environment.Player.EngulfMode = !Environment.Player.EngulfMode;
            }
        }

        if (settings.CheatsEnabled && cheatEditor.ReadTrigger())
        {
            Environment.HUD.ShowReproductionDialog();
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
        Environment.Clouds.AddCloud(SimulationParameters.Instance.GetCompound(name),
            8000.0f * delta, Environment.Camera.CursorWorldPos);
    }*/
}
