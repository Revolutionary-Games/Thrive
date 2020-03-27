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

    public override void _Process(float delta)
    {
    }
}
