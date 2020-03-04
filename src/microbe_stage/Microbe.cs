using System;
using Godot;

/// <summary>
///   Main script on each cell in the game
/// </summary>
public class Microbe : RigidBody
{
    /// <summary>
    ///   The point towards which the microbe will move to point to
    /// </summary>
    public Vector3 LookAtPoint = new Vector3(0, 0, -1);

    /// <summary>
    ///   The direction the microbe wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection = new Vector3(0, 0, 0);

    public override void _Ready()
    {
    }

    public override void _Process(float delta)
    {
    }
}
