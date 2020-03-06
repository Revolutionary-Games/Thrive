using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages spawning and processing compound clouds
/// </summary>
public class CompoundCloudSystem : Node
{
    private List<CompoundCloudPlane> clouds = new List<CompoundCloudPlane>();

    public override void _Ready()
    {
        foreach (var child in GetChildren())
        {
            clouds.Add((CompoundCloudPlane)child);
        }

        if (clouds.Count != 9)
            GD.PrintErr("CompoundCloudSystem doesn't have 9 child cloud objects");
    }

    public override void _Process(float delta)
    {
    }

    /// <summary>
    ///   Resets the cloud contents and positions
    /// </summary>
    public void Init()
    {
    }
}
