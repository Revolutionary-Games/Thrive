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

    // TODO: readd cheats

    public override void _Ready()
    {
        stage = (MicrobeStage)GetParent();
    }

    public override void _Input(InputEvent @event)
    {
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
            stage.Player.MovementDirection = movement;
            stage.Player.LookAtPoint = stage.Camera.CursorWorldPos;
        }
    }
}
