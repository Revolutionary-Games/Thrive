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
                // Reset this to make sure the animation doesn't start again behind our backs
                player.Autoplay = null;

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

            int childCount = spatial.GetChildCount();

            // When no path provided, find the first animation player
            if (string.IsNullOrEmpty(playerPath))
            {
                for (int i = 0; i < childCount; ++i)
                {
                    var child = spatial.GetChild(i);

                    if (child is AnimationPlayer casted)
                        return casted;
                }

                return null;
            }

            if (childCount == 1)
            {
                // There might be one level of indirection
                if (!playerPath!.StartsWith("Spatial"))
                {
                    // TODO: how to suppress errors if this is wrong
                    var attempt = spatial.GetChild(0).GetNode<AnimationPlayer>(playerPath);

                    if (attempt != null)
                        return attempt;
                }
            }

            return spatial.GetNode<AnimationPlayer>(playerPath);
        }
    }
}
