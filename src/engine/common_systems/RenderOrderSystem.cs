namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Applies <see cref="RenderOrderOverride"/>
    /// </summary>
    [With(typeof(RenderOrderOverride))]
    [With(typeof(EntityMaterial))]
    public sealed class RenderOrderSystem : AEntitySetSystem<float>
    {
        public RenderOrderSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var renderOrder = ref entity.Get<RenderOrderOverride>();

            if (renderOrder.RenderPriorityApplied)
                return;

            ref var material = ref entity.Get<EntityMaterial>();

            // Wait until material becomes available
            if (material.Material == null)
                return;

            material.Material.RenderPriority = renderOrder.RenderPriority;

            renderOrder.RenderPriorityApplied = true;
        }
    }
}
