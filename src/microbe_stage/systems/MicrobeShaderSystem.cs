namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles things related to <see cref="MicrobeShaderParameters"/>. This should run each frame and pause when
    ///   the game is paused.
    /// </summary>
    [With(typeof(MicrobeShaderParameters))]
    [With(typeof(EntityMaterial))]
    public sealed class MicrobeShaderSystem : AEntitySetSystem<float>
    {
        public MicrobeShaderSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var shaderParameters = ref entity.Get<MicrobeShaderParameters>();

            if (shaderParameters.ParametersApplied && !shaderParameters.PlayAnimations)
                return;

            if (shaderParameters.PlayAnimations)
            {
                if (shaderParameters.DissolveAnimationSpeed != 0)
                {
                    if (float.IsNaN(shaderParameters.DissolveValue))
                    {
                        GD.PrintErr("Correcting NaN as dissolve shader parameter");
                        shaderParameters.DissolveValue = 0;
                    }
                    else if (shaderParameters.DissolveValue < 1)
                    {
                        shaderParameters.DissolveValue += shaderParameters.DissolveAnimationSpeed * delta;

                        if (shaderParameters.DissolveValue > 1)
                            shaderParameters.DissolveValue = 1;
                    }
                }
                else
                {
                    GD.PrintErr("Entity has incorrectly enabled animations playing but no animation flag " +
                        "is turned on");
                    shaderParameters.PlayAnimations = false;
                }
            }

            ref var entityMaterial = ref entity.Get<EntityMaterial>();

            // Wait for the material to be defined
            if (entityMaterial.Material == null)
                return;

            entityMaterial.Material.SetShaderParam("dissolveValue", shaderParameters.DissolveValue);

            shaderParameters.ParametersApplied = true;
        }
    }
}
