namespace Components
{
    using DefaultEcs;

    /// <summary>
    ///   Allows control over the few (animation) shader parameters available in the microbe stage for some entities.
    ///   Requires <see cref="EntityMaterial"/> to apply.
    /// </summary>
    public struct MicrobeShaderParameters
    {
        /// <summary>
        ///   Dissolve effect value, range [0, 1]. 0 is default not dissolved state
        /// </summary>
        public float DissolveValue;

        /// <summary>
        ///   Automatically animate the <see cref="DissolveValue"/> when this is not 0 and <see cref="PlayAnimations"/>
        ///   is true. 1 is default speed.
        /// </summary>
        public float DissolveAnimationSpeed;

        /// <summary>
        ///   Set to true to enable playing any of the separate animations. If this is false none of the animations
        ///   play at all.
        /// </summary>
        public bool PlayAnimations;

        /// <summary>
        ///   Always reset this to false after changing something to have the changes apply
        /// </summary>
        public bool ParametersApplied;
    }

    public static class MicrobeShaderParametersHelpers
    {
        public static void StartDissolveAnimation(this Entity entity, bool useChunkSpeed)
        {
            float speed = 1;

            if (useChunkSpeed)
                speed = Constants.FLOATING_CHUNKS_DISSOLVE_SPEED;

            if (entity.Has<MicrobeShaderParameters>())
            {
                ref var shaderParameters = ref entity.Get<MicrobeShaderParameters>();

                shaderParameters.DissolveAnimationSpeed = speed;
                shaderParameters.PlayAnimations = true;
            }
            else
            {
                entity.Set(new MicrobeShaderParameters
                {
                    DissolveAnimationSpeed = speed,
                    PlayAnimations = true,
                });
            }
        }
    }
}
