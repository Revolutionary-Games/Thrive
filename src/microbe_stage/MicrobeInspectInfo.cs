using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Inspection info for the microbe stage.
/// </summary>
public partial class MicrobeInspectInfo : PlayerInspectInfo
{
    protected readonly Dictionary<Compound, float> hoveredCompounds = new();

    /// <summary>
    ///   Used to query the real hovered compound values.
    ///   This is a member variable to reduce GC pressure.
    /// </summary>
    private readonly Dictionary<Compound, float> currentHoveredCompounds = new();

    private readonly Dictionary<Compound, double> compoundDelayTimer = new();

    private Vector3? lastCursorWorldPos;

#pragma warning disable CA2213
    private CompoundCloudSystem? clouds;
    private MicrobeCamera? camera;
#pragma warning restore CA2213

    /// <summary>
    ///   All compounds the user is hovering over with delay to reduce flickering.
    /// </summary>
    public IReadOnlyDictionary<Compound, float> HoveredCompounds => hoveredCompounds;

    public void Init(CompoundCloudSystem clouds, MicrobeCamera camera)
    {
        this.clouds = clouds;
        this.camera = camera;
    }

    public override void Process(double delta)
    {
        if (camera == null || clouds == null)
            throw new InvalidOperationException($"{nameof(MicrobeInspectInfo)} was not initialized");

        base.Process(delta);

        clouds.GetAllAvailableAt(camera.CursorVisualWorldPos, currentHoveredCompounds, false);

        if (camera.CursorVisualWorldPos != lastCursorWorldPos)
        {
            hoveredCompounds.Clear();
            lastCursorWorldPos = camera.CursorVisualWorldPos;
        }

        foreach (var compound in SimulationParameters.Instance.GetCloudCompounds())
        {
            hoveredCompounds.TryGetValue(compound, out float oldAmount);
            currentHoveredCompounds.TryGetValue(compound, out float newAmount);

            // Delay removing of label to reduce flickering.
            if (newAmount == 0.0f && oldAmount > 0.0f)
            {
                compoundDelayTimer.TryGetValue(compound, out var delayDelta);
                delayDelta += delta;
                if (delayDelta > Constants.COMPOUND_HOVER_INFO_REMOVE_DELAY)
                {
                    compoundDelayTimer.Remove(compound);
                    hoveredCompounds[compound] = 0.0f;
                    continue;
                }

                compoundDelayTimer[compound] = delayDelta;
                continue;
            }

            // Ignore small changes to reduce flickering.
            if (Mathf.Abs(newAmount - oldAmount) >= Constants.COMPOUND_HOVER_INFO_THRESHOLD)
            {
                hoveredCompounds[compound] = newAmount;
            }
        }

        currentHoveredCompounds.Clear();
    }

    /// <inheritdoc/>
    protected override Vector2 ApplyScreenEffects(Vector2 mousePos, Vector2 viewportSize)
    {
        if (Settings.Instance.ChromaticEnabled)
        {
            float distortion = Settings.Instance.ChromaticAmount;
            mousePos = ScreenUtils.BarrelDistortion(mousePos, distortion, viewportSize);
        }

        return mousePos;
    }
}
