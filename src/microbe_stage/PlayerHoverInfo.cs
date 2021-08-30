﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   A system that manages detecting what the player is hovering over with the cursor.
/// </summary>
public class PlayerHoverInfo : Node
{
    private MicrobeCamera camera;
    private CompoundCloudSystem cloudSystem;

    /// <summary>
    ///   All compounds the user is hovering over.
    /// </summary>
    public Dictionary<Compound, float> HoveredCompounds { get; private set; } = new Dictionary<Compound, float>();

    /// <summary>
    ///   All microbes the user is hovering over.
    /// </summary>
    public List<Microbe> HoveredMicrobes { get; } = new List<Microbe>();

    public bool IsHoveringOverAnything => HoveredCompounds.Count > 0 || HoveredMicrobes.Count > 0;

    public void Init(MicrobeCamera camera, CompoundCloudSystem cloudSystem)
    {
        this.camera = camera;
        this.cloudSystem = cloudSystem;
    }

    public override void _Process(float delta)
    {
        HoveredCompounds = cloudSystem.GetAllAvailableAt(camera.CursorWorldPos);

        var allMicrobes = GetTree().GetNodesInGroup(Constants.AI_TAG_MICROBE);

        foreach (var hoveredMicrobe in HoveredMicrobes)
            hoveredMicrobe.IsHoveredOver = false;

        HoveredMicrobes.Clear();

        foreach (Microbe microbe in allMicrobes)
        {
            var distance = (microbe.GlobalTransform.origin - camera.CursorWorldPos).LengthSquared();

            // Find only cells that have the mouse
            // position within their membrane
            if (distance > microbe.RadiusSquared + Constants.MICROBE_HOVER_DETECTION_EXTRA_RADIUS)
                continue;

            microbe.IsHoveredOver = true;
            HoveredMicrobes.Add(microbe);
        }
    }
}
