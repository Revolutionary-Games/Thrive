namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles things related to <see cref="MicrobeShaderParameters"/>. This should run each frame and pause when
    ///   the game is paused.
    /// </summary>
    [With(typeof(MicrobeShaderParameters))]
    [With(typeof(EntityMaterial))]
    [RunsOnFrame]
    public sealed class MicrobeShaderSystem : AEntitySetSystem<float>
    {
        // private readonly Lazy<Texture> noiseTexture = GD.Load<Texture>("res://assets/textures/dissolve_noise.tres");

        public MicrobeShaderSystem(World world) : base(world, null)
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
            if (entityMaterial.Materials == null)
                return;

            foreach (var material in entityMaterial.Materials)
            {
                material.SetShaderParam("dissolveValue", shaderParameters.DissolveValue);
            }

            // TODO: remove this and the lazy value if unnecessary (if necessary this should be applied just once and
            // not each frame)
            // entityMaterial.Material.SetShaderParam("dissolveTexture", noiseTexture);

            shaderParameters.ParametersApplied = true;
        }
    }
}
