namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   Applies <see cref="RenderPriorityOverride"/>
/// </summary>
/// <remarks>
///   <para>
///     This is marked as just reading the materials as this simply just assigns a single Godot property to the
///     materials, so this doesn't really conflict with any other potential "writes" to the same component.
///   </para>
/// </remarks>
[ReadsComponent(typeof(EntityMaterial))]
[RuntimeCost(0.5f)]
[RunsOnMainThread]
public partial class RenderPrioritySystem : BaseSystem<World, float>
{
    public RenderPrioritySystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref RenderPriorityOverride renderOrder, ref EntityMaterial material)
    {
        if (renderOrder.RenderPriorityApplied)
            return;

        // Wait until material becomes available
        if (material.Materials == null)
            return;

        foreach (var shaderMaterial in material.Materials)
        {
            shaderMaterial.RenderPriority = renderOrder.RenderPriority;
        }

        renderOrder.RenderPriorityApplied = true;
    }
}
