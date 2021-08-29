using System.Collections.Generic;
using Godot;

/// <summary>
///   A system that manages reading what the player is hovering over.
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
    public List<Microbe> HoveredMicrobes { get; private set; } = new List<Microbe>();

    public bool IsHoveringAnyEntity => HoveredCompounds.Count > 0 || HoveredMicrobes.Count > 0;

    public void Init(MicrobeCamera camera, CompoundCloudSystem cloudSystem)
    {
        this.camera = camera;
        this.cloudSystem = cloudSystem;
    }

    public override void _Process(float delta)
    {
        HoveredCompounds = cloudSystem.GetAllAvailableAt(camera.CursorWorldPos);

        var microbes = GetTree().GetNodesInGroup(Constants.AI_TAG_MICROBE);

        foreach (var microbe in HoveredMicrobes)
            microbe.IsHoveredOver = false;

        HoveredMicrobes.Clear();

        foreach (Microbe entry in microbes)
        {
            var distance = (entry.GlobalTransform.origin - camera.CursorWorldPos).Length();

            // Find only cells that have the mouse
            // position within their membrane
            if (distance > entry.Radius + Constants.MICROBE_HOVER_DETECTION_EXTRA_RADIUS)
                continue;

            entry.IsHoveredOver = true;
            HoveredMicrobes.Add(entry);
        }
    }
}
