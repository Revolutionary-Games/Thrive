using System;
using Godot;

/// <summary>
///   Main class of the microbe editor
/// </summary>
public class MicrobeEditor : Node
{
    private MicrobeCamera camera;

    public override void _Ready()
    {
        camera = GetNode<MicrobeCamera>("PrimaryCamera");

        camera.ObjectToFollow = GetNode<Spatial>("CameraLookAt");
        OnEnterEditor();
    }

    /// <summary>
    ///   Sets up the editor when entering
    /// </summary>
    public void OnEnterEditor()
    {
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("e_rotate_right"))
        {
            GD.Print("Editor: Rotate right");
        }

        if (@event.IsActionPressed("e_rotate_left"))
        {
            GD.Print("Editor: Rotate left");
        }

        if (@event.IsActionPressed("e_redo"))
        {
            GD.Print("Editor: redo");
        }
        else if (@event.IsActionPressed("e_undo"))
        {
            GD.Print("Editor: undo");
        }
    }

    public override void _Process(float delta)
    {
    }
}
