using System;
using Godot;

/// <summary>
///   Script for the floating chunks (cell parts, rocks, hazards)
/// </summary>
public class FloatingChunk : RigidBody, ISpawned
{
    [Export]
    public PackedScene GraphicsScene;

    public int DespawnRadiusSqr { get; set; }

    public Node SpawnedNode
    {
        get
        {
            return this;
        }
    }

    public override void _Ready()
    {
        if (GraphicsScene == null)
        {
            GD.PrintErr("FloatingChunk doesn't have GraphicsScene set");
            return;
        }

        AddChild(GraphicsScene.Instance());
    }

    public override void _Process(float delta)
    {
    }
}
