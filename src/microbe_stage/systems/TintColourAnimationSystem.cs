namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Handles applying the shader "tint" parameter based on <see cref="ColourAnimation"/> to an
    ///   <see cref="EntityMaterial"/> that has microbe stage compatible shader parameter names
    /// </summary>
    [With(typeof(ColourAnimation))]
    [With(typeof(EntityMaterial))]
    public sealed class TintColourAnimationSystem : AEntitySetSystem<float>
    {
        public TintColourAnimationSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var animation = ref entity.Get<ColourAnimation>();

            if (animation.ColourApplied)
                return;

            ref var entityMaterial = ref entity.Get<EntityMaterial>();

            if (entityMaterial.Material == null)
                return;

            entityMaterial.Material.SetShaderParam("tint", animation.CurrentColour);

            animation.ColourApplied = true;
        }
    }
}
