using System.Collections.Generic;
using Godot;

/// <summary>
///   A system that manages detecting what the player is hovering over with the cursor.
/// </summary>
public class PlayerHoverInfo : Node
{
    private readonly Dictionary<Compound, float> currentHoveredCompounds = new Dictionary<Compound, float>();
    private readonly Dictionary<Compound, float> compoundDelayTimer = new Dictionary<Compound, float>();
    private MicrobeCamera camera;
    private CompoundCloudSystem cloudSystem;
    private Vector3 lastCursorWorldPos = Vector3.Inf;

    /// <summary>
    ///   All compounds the user is hovering over.
    /// </summary>
    public Dictionary<Compound, float> HoveredCompounds { get; } = new Dictionary<Compound, float>();

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
        cloudSystem.GetAllAvailableAt(camera.CursorWorldPos, currentHoveredCompounds);

        if (camera.CursorWorldPos != lastCursorWorldPos)
        {
            HoveredCompounds.Clear();
            lastCursorWorldPos = camera.CursorWorldPos;
        }

        foreach (var compound in currentHoveredCompounds)
        {
            HoveredCompounds.TryGetValue(compound.Key, out float oldAmount);

            // Delay removing of label to reduce flickering.
            if (compound.Value == 0f && oldAmount > 0f)
            {
                compoundDelayTimer.TryGetValue(compound.Key, out float delayDelta);
                delayDelta += delta;
                if (delayDelta > Constants.COMPOUND_HOVER_INFO_REMOVE_DELAY)
                {
                    compoundDelayTimer.Remove(compound.Key);
                    HoveredCompounds[compound.Key] = 0f;
                    continue;
                }

                compoundDelayTimer[compound.Key] = delayDelta;
                continue;
            }

            // Ignore small changes to reduce flickering.
            if (Mathf.Abs(compound.Value - oldAmount) >= Constants.COMPOUND_HOVER_INFO_THRESHOLD)
            {
                HoveredCompounds[compound.Key] = compound.Value;
            }
        }

        currentHoveredCompounds.Clear();

        var allMicrobes = GetTree().GetNodesInGroup(Constants.AI_TAG_MICROBE);

        foreach (var hoveredMicrobe in HoveredMicrobes)
            hoveredMicrobe.IsHoveredOver = false;

        HoveredMicrobes.Clear();

        foreach (Microbe microbe in allMicrobes)
        {
            var distanceSquared = (microbe.GlobalTransform.origin - camera.CursorWorldPos).LengthSquared();

            // Find only cells that have the mouse
            // position within their membrane
            if (distanceSquared > microbe.RadiusSquared + Constants.MICROBE_HOVER_DETECTION_EXTRA_RADIUS_SQUARED)
                continue;

            microbe.IsHoveredOver = true;
            HoveredMicrobes.Add(microbe);
        }
    }
}
