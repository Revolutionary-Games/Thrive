namespace Systems;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Attaches <see cref="SpatialInstance"/> to the Godot scene and handles freeing unused spatial instances.
///   Must run before <see cref="SpatialPositionSystem"/>.
/// </summary>
/// <remarks>
///   <para>
///     This explicitly marks the <see cref="SpatialInstance"/> as read component as the threading system doesn't
///     handle AComponentSystem automatically. And as this *just* attaches the created instances to the scene tree,
///     this doesn't *really* modify the component data.
///   </para>
/// </remarks>
[RunsBefore(typeof(SpatialPositionSystem))]
[ReadsComponent(typeof(SpatialInstance))]
[RuntimeCost(5)]
[RunsOnMainThread]
public partial class SpatialAttachSystem : BaseSystem<World, float>
{
    private readonly Node godotWorldRoot;

    private readonly Dictionary<Node3D, AttachedInfo> attachedSpatialInstances = new();
    private readonly List<Node3D> instancesToDelete = new();

    public SpatialAttachSystem(Node godotWorldRoot, World world) : base(world)
    {
        this.godotWorldRoot = godotWorldRoot;
    }

    public void FreeNodeResources()
    {
        foreach (var entry in attachedSpatialInstances)
        {
            entry.Key.QueueFree();
        }

        attachedSpatialInstances.Clear();
    }

    public override void BeforeUpdate(in float delta)
    {
        // Unmark all
        foreach (var info in attachedSpatialInstances.Values)
        {
            info.Marked = false;
        }
    }

    public override void AfterUpdate(in float delta)
    {
        // Delete unmarked
        foreach (var pair in attachedSpatialInstances)
        {
            if (!pair.Value.Marked)
                instancesToDelete.Add(pair.Key);
        }

        foreach (var spatial in instancesToDelete)
        {
            attachedSpatialInstances.Remove(spatial);
            spatial.QueueFree();
        }

        instancesToDelete.Clear();
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref SpatialInstance spatial)
    {
        var graphicalInstance = spatial.GraphicalInstance;
        if (graphicalInstance == null)
            return;

        if (!attachedSpatialInstances.TryGetValue(graphicalInstance, out var info))
        {
            // New spatial to attach
            godotWorldRoot.AddChild(graphicalInstance);

            info = new AttachedInfo();
            attachedSpatialInstances.Add(graphicalInstance, info);
        }
        else
        {
            info.Marked = true;
        }
    }

    /// <summary>
    ///   Info (really just a marked status) for spatial instances. This breaks the use of only value types by
    ///   systems, so there might be some more efficient way to implement this (for example, with two hash sets)
    /// </summary>
    private class AttachedInfo
    {
        public bool Marked = true;
    }
}
