namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles updating the state of <see cref="ColourAnimation"/> based on animations triggered elsewhere
    /// </summary>
    [With(typeof(ColourAnimation))]
    public sealed class ColourAnimationSystem : AEntitySetSystem<float>
    {
        public ColourAnimationSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var colourAnimation = ref entity.Get<ColourAnimation>();

            if (!colourAnimation.Animating)
                return;

            if (colourAnimation.AnimationDuration <= 0)
            {
                GD.PrintErr("Animation duration for ColourAnimation not set properly");
                colourAnimation.AnimationDuration = 0.001f;
            }

            colourAnimation.AnimationElapsed += delta;

            if (colourAnimation.AnimationElapsed >= colourAnimation.AnimationDuration)
            {
                // Finished animation

                if (colourAnimation.AutoReverseAnimation)
                {
                    // Play in reverse
                    colourAnimation.AutoReverseAnimation = false;

                    // Swap direction
                    (colourAnimation.AnimationTargetColour, colourAnimation.AnimationStartColour) = (
                        colourAnimation.AnimationStartColour, colourAnimation.AnimationTargetColour);
                    colourAnimation.AnimationElapsed -= colourAnimation.AnimationDuration;

                    if (colourAnimation.AnimationElapsed < 0)
                        colourAnimation.AnimationElapsed = 0;
                }
                else
                {
                    // No new animation to run, stop processing this entity
                    colourAnimation.Animating = false;
                }
            }

            colourAnimation.ColourApplied = false;
        }
    }
}
