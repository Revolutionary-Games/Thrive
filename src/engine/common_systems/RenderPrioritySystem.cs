namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Applies <see cref="RenderPriorityOverride"/>
    /// </summary>
    [With(typeof(RenderPriorityOverride))]
    [With(typeof(EntityMaterial))]
    public sealed class RenderPrioritySystem : AEntitySetSystem<float>
    {
        public RenderPrioritySystem(World world, IParallelRunner runner) : base(world, runner)
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
}
