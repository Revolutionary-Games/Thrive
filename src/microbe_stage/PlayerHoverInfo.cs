using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   A system that manages detecting what the player is hovering over with the cursor.
/// </summary>
public class PlayerHoverInfo : Node
{
    /// <summary>
    ///   Used to query the real hovered compound values.
    ///   This is a member variable to reduce GC pressure.
    /// </summary>
    private readonly Dictionary<Compound, float> currentHoveredCompounds = new();

    private readonly Dictionary<Compound, float> compoundDelayTimer = new();
    private MicrobeCamera? camera;
    private CompoundCloudSystem? cloudSystem;

    private Vector3? lastCursorWorldPos;

    /// <summary>
    ///   List off all cloud compounds to iterate.
    /// </summary>
    private List<Compound> cloudCompounds = null!;

    /// <summary>
    ///   All compounds the user is hovering over with delay to reduce flickering.
    /// </summary>
    public Dictionary<Compound, float> HoveredCompounds { get; } = new();

    /// <summary>
    ///   All microbes the user is hovering over.
    /// </summary>
    public List<Microbe> HoveredMicrobes { get; } = new();

    public bool IsHoveringOverAnything => HoveredCompounds.Count > 0 || HoveredMicrobes.Count > 0;

    public override void _Ready()
    {
        cloudCompounds = SimulationParameters.Instance.GetCloudCompounds();

        // This needs to be processed after Microbe, otherwise this can cause double membrane initialization
        ProcessPriority = 10;
    }

    public void Init(MicrobeCamera camera, CompoundCloudSystem cloudSystem)
    {
        this.camera = camera;
        this.cloudSystem = cloudSystem;
    }

    public override void _Process(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        if (camera == null || cloudSystem == null)
            throw new InvalidOperationException($"{nameof(PlayerHoverInfo)} was not initialized");

        cloudSystem.GetAllAvailableAt(camera.CursorWorldPos, currentHoveredCompounds);

        if (camera.CursorWorldPos != lastCursorWorldPos)
        {
            HoveredCompounds.Clear();
            lastCursorWorldPos = camera.CursorWorldPos;
        }

        foreach (var compound in cloudCompounds)
        {
            HoveredCompounds.TryGetValue(compound, out float oldAmount);
            currentHoveredCompounds.TryGetValue(compound, out float newAmount);

            // Delay removing of label to reduce flickering.
            if (newAmount == 0f && oldAmount > 0f)
            {
                compoundDelayTimer.TryGetValue(compound, out float delayDelta);
                delayDelta += delta;
                if (delayDelta > Constants.COMPOUND_HOVER_INFO_REMOVE_DELAY)
                {
                    compoundDelayTimer.Remove(compound);
                    HoveredCompounds[compound] = 0f;
                    continue;
                }

                compoundDelayTimer[compound] = delayDelta;
                continue;
            }

            // Ignore small changes to reduce flickering.
            if (Mathf.Abs(newAmount - oldAmount) >= Constants.COMPOUND_HOVER_INFO_THRESHOLD)
            {
                HoveredCompounds[compound] = newAmount;
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

            // Find only cells that have the mouse position within their membrane
            if (distanceSquared > microbe.RadiusSquared + Constants.MICROBE_HOVER_DETECTION_EXTRA_RADIUS_SQUARED)
                continue;

            microbe.IsHoveredOver = true;
            HoveredMicrobes.Add(microbe);
        }
    }
}
