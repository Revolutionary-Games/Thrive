namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;

/// <summary>
///   Applies <see cref="RenderPriorityOverride"/>
/// </summary>
/// <remarks>
///   <para>
///     This is marked as just reading the materials as this simply just assigns a single Godot property to the
///     materials, so this doesn't really conflict with any other potential "writes" to the same component.
///   </para>
/// </remarks>
[With(typeof(RenderPriorityOverride))]
[With(typeof(EntityMaterial))]
[ReadsComponent(typeof(EntityMaterial))]
[RuntimeCost(0.5f)]
[RunsOnMainThread]
public sealed class RenderPrioritySystem : AEntitySetSystem<float>
{
    public RenderPrioritySystem(World world) : base(world, null)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var renderOrder = ref entity.Get<RenderPriorityOverride>();

        if (renderOrder.RenderPriorityApplied)
            return;

        ref var material = ref entity.Get<EntityMaterial>();

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
