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
        public TintColourAnimationSystem(World world) : base(world, null)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var animation = ref entity.Get<ColourAnimation>();

            if (animation.ColourApplied)
                return;

            ref var entityMaterial = ref entity.Get<EntityMaterial>();

            if (entityMaterial.Materials == null)
                return;

            var materials = entityMaterial.Materials;

            if (animation.AnimateOnlyFirstMaterial)
            {
                if (materials.Length > 0)
                {
                    materials[0].SetShaderParam("tint", animation.CurrentColour);
                }
            }
            else
            {
                foreach (var material in materials)
                {
                    material.SetShaderParam("tint", animation.CurrentColour);
                }
            }

            animation.ColourApplied = true;
        }
    }
}
