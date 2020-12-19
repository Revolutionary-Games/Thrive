using System;
using Godot;

/// <summary>
///   Handles key input in the microbe stage
/// </summary>
public class PlayerMicrobeInput : NodeWithInput
{
<<<<<<< HEAD
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
    private readonly InputTrigger toggleBinding = new InputTrigger("g_toggle_binding");

    private readonly InputBool fireToxin = new InputBool("g_fire_toxin");
    private readonly InputToggle autoMove = new InputToggle("g_hold_forward");
=======
    private bool autoMove;
>>>>>>> master

    /// <summary>
    ///   A reference to the stage is kept to get to the player object
    ///   and also the cloud spawning.
    /// </summary>
    private MicrobeStage stage;

<<<<<<< HEAD
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
            toggleBinding,
            fireToxin,
            cheatEditor,
            cheatGlucose,
            cheatAmmonia,
            cheatPhosphates,
        });
    }

=======
>>>>>>> master
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

<<<<<<< HEAD
        if (toggleEngulf.ReadTrigger())
        {
            if (stage.Player != null)
            {
                if (stage.Player.EngulfMode)
                {
                    stage.Player.EngulfMode = false;
                }
                else
                {
                    stage.Player.BindingMode = false;
                    stage.Player.EngulfMode = true;
                }
            }
        }

        if (toggleBinding.ReadTrigger())
        {
            if (stage.Player != null)
            {
                if (stage.Player.AnyInBindingMode)
                {
                    stage.Player.AnyInBindingMode = false;
                }
                else
                {
                    stage.Player.EngulfMode = false;
                    stage.Player.AnyInBindingMode = true;
                }
            }
        }
=======
    [RunOnKeyDown("g_toggle_engulf")]
    public void ToggleEngulf()
    {
        if (stage.Player == null)
            return;
>>>>>>> master

        stage.Player.EngulfMode = !stage.Player.EngulfMode;
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
