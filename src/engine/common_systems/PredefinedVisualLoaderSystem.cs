﻿namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Loads predefined visual instances for entities.
/// </summary>
/// <remarks>
///   <para>
///     On average this doesn't take a lot of time, but due to potential load time spikes when this does load
///     something this has runtime cost of 1 even though 0.25-0.5 would be more suitable based on raw numbers.
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     TODO: could pool some visuals to decrease the performance hit spawning a bunch of stuff causes
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     TODO: merge the loading logic of this to leverage <see cref="ResourceManager"/>
///   </para>
/// </remarks>
[RuntimeCost(0.75f)]
[RunsOnMainThread]
public partial class PredefinedVisualLoaderSystem : BaseSystem<World, float>
{
    /// <summary>
    ///   This stores all the scenes seen in this world. This is done with the assumption that any once used scene
    ///   will get used again in this world at some point.
    /// </summary>
    private readonly Dictionary<VisualResourceIdentifier, PackedScene?> usedScenes = new();

    private PackedScene? errorScene;

    // External resource that should not be disposed
#pragma warning disable CA2213
    private SimulationParameters simulationParameters = null!;
#pragma warning restore CA2213

    public PredefinedVisualLoaderSystem(World world) : base(world)
    {
        // TODO: will we be able to at some point load Godot scenes in parallel without issues?
        // Also a proper resource manager would basically remove the need for that
    }

    public override void BeforeUpdate(in float delta)
    {
        simulationParameters = SimulationParameters.Instance;
    }

    // TODO: this will need a callback for when graphics visual level is updated and this needs to redo all of the
    // loaded graphics (if we add a quality level graphics option)

    public override void Dispose()
    {
        Dispose(true);

        // This doesn't have a destructor
        // GC.SuppressFinalize(this);
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref PredefinedVisuals visuals, ref SpatialInstance spatial)
    {
        // Skip update if nothing to do
        if (visuals.VisualIdentifier == visuals.LoadedInstance)
            return;

        visuals.LoadedInstance = visuals.VisualIdentifier;

        if (!usedScenes.TryGetValue(visuals.VisualIdentifier, out var scene))
        {
            scene = LoadVisual(simulationParameters.GetVisualResource(visuals.LoadedInstance));

            if (scene == null)
            {
                // Try to fall back to an error scene
                errorScene ??= LoadVisual(simulationParameters.GetErrorVisual());
                scene = errorScene;
            }

            usedScenes.Add(visuals.VisualIdentifier, scene);
        }

        if (scene == null)
        {
            // Even the error scene failed
            return;
        }

        // SpatialAttachSystem will handle deleting the graphics instance if not used

        // TODO: could add a debug-only leak detector system that checks no leaks persist

        try
        {
            spatial.GraphicalInstance = scene.Instantiate<Node3D>();
        }
        catch (Exception e)
        {
            GD.PrintErr("Predefined visual is not convertible to Node3D: ", e);
        }
    }

    private PackedScene? LoadVisual(VisualResourceData visualResourceData)
    {
        // TODO: visual quality (/ LOD level?)
        return GD.Load<PackedScene>(visualResourceData.NormalQualityPath);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            usedScenes.Clear();
        }
    }
}
