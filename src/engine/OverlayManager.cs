using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages loading and ordering game overlays
/// </summary>
public class OverlayManager : Node
{
    /// <summary>
    ///   Controls the order the filters are positioned after the main scene
    /// </summary>
    private readonly List<Node> orderedFilters = new List<Node>();

    public override void _Ready()
    {
        var root = GetTree().Root;

        // Get references to the overlays, which must be autoloads that are specified before this one in the load order
        var overlays = new string[]
        {
            "LoadingScreen",
            "SaveStatusOverlay",
            "ColourblindScreenFilter",
            "FPSCounter",
        };

        foreach (var name in overlays)
        {
            var overlay = root.GetNode(name);

            if (overlay == null)
                throw new NullReferenceException("specified overlay was not autoloaded before this");

            orderedFilters.Add(overlay);
        }

        PauseMode = PauseModeEnum.Process;
    }

    public override void _Process(float delta)
    {
        var root = GetTree().Root;
        var count = root.GetChildCount();

        // Move all the overlays to their right indexes
        for (int i = 0; i < orderedFilters.Count; ++i)
        {
            var targetPos = count - (orderedFilters.Count - i);
            if (root.GetChild(targetPos) != orderedFilters[i])
            {
                root.MoveChild(orderedFilters[i], targetPos);
            }
        }
    }
}
