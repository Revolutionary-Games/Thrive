namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   System that handles <see cref="AnimationControl"/>
    /// </summary>
    [With(typeof(AnimationControl))]
    [With(typeof(SpatialInstance))]
    [RunsOnMainThread]
    public sealed class AnimationControlSystem : AEntitySetSystem<float>
    {
        public AnimationControlSystem(World world) : base(world, null)
        {
        }

        protected override void Update(float state, in Entity entity)
        {
            ref var animation = ref entity.Get<AnimationControl>();

            if (animation.AnimationApplied)
                return;

            ref var spatial = ref entity.Get<SpatialInstance>();

            // Wait until graphics instance is initialized
            if (spatial.GraphicalInstance == null)
                return;

            var player = GetPlayer(spatial.GraphicalInstance, animation.AnimationPlayerPath);

            if (player == null)
            {
                GD.PrintErr($"{nameof(AnimationControl)} component couldn't find animation player from node: ",
                    spatial.GraphicalInstance, " with relative path: ", animation.AnimationPlayerPath);

                // Set the animation as applied to not spam this error message over and over
                animation.AnimationApplied = true;
                return;
            }

            if (animation.StopPlaying)
            {
                // TODO: parameter in the component to allow passing reset: false?
                player.Stop();
            }

            animation.AnimationApplied = true;
        }

        private AnimationPlayer? GetPlayer(Spatial spatial, string? playerPath)
        {
            // TODO: cache for animation players to allow fast per-update data access
            // For now a cache is not implemented as this is just for stopping playing an animation once and then not
            // doing anything

            // When no path provided, assume default name. This is needed as the AnimationPlayer doesn't inherit
            // Spatial so we can't try to even cast that here
            if (string.IsNullOrEmpty(playerPath))
                return spatial.GetNode<AnimationPlayer>(nameof(AnimationPlayer));

            return spatial.GetNode<AnimationPlayer>(playerPath);
        }
    }
}
