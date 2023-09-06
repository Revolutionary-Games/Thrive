namespace Components
{
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Specifies simple colour changing animations
    /// </summary>
    public struct ColourAnimation
    {
        /// <summary>
        ///   The default colour that can be returned to. For example stores the base microbe colour to reset to after
        ///   animating.
        /// </summary>
        public Color DefaultColour;

        public Color AnimationTargetColour;
        public Color AnimationStartColour;

        public float AnimationDuration;

        /// <summary>
        ///   The code triggering animations may store whatever info it wants about the animations here. For example
        ///   how important the current animation is to know if some other animation is allowed to overwrite this.
        /// </summary>
        public int AnimationUserInfo;

        /// <summary>
        ///   This shouldn't be changed manually outside the colour animation system
        /// </summary>
        public float AnimationElapsed;

        /// <summary>
        ///   If true the animation is played in reverse after it completes once. Used for example for colour flashes.
        /// </summary>
        public bool AutoReverseAnimation;

        /// <summary>
        ///   Needs to be set to true to trigger the animation to happen
        /// </summary>
        public bool Animating;

        /// <summary>
        ///   if true only the first material is animated on an entity and the other ones are left untouched
        /// </summary>
        public bool AnimateOnlyFirstMaterial;

        /// <summary>
        ///   True when whatever entity / stage specific system that handles applying the colour is
        /// </summary>
        [JsonIgnore]
        public bool ColourApplied;

        public ColourAnimation(Color defaultColour)
        {
            DefaultColour = defaultColour;
            AnimationTargetColour = defaultColour;
            AnimationStartColour = default;

            AnimationDuration = 0;
            AnimationUserInfo = 0;
            AnimationElapsed = 0;

            AutoReverseAnimation = false;
            Animating = false;
            AnimateOnlyFirstMaterial = false;

            ColourApplied = false;
        }

        /// <summary>
        ///   The current colour value that should be displayed. Note that this component by itself is not enough to
        ///   get this to display anywhere.
        /// </summary>
        [JsonIgnore]
        public Color CurrentColour
        {
            get
            {
                if (!Animating || AnimationElapsed >= AnimationDuration)
                    return AnimationTargetColour;

                return AnimationStartColour.LinearInterpolate(AnimationTargetColour,
                    AnimationElapsed / AnimationDuration);
            }
        }
    }

    public static class ColourAnimationHelpers
    {
        /// <summary>
        ///   Plays a flashing animation
        /// </summary>
        /// <param name="animation">Where to put the animation</param>
        /// <param name="targetColour">The colour to flash as</param>
        /// <param name="duration">How long the change to the target colour takes</param>
        /// <param name="priority">
        ///   Used to skip previous animations, if this is higher than current
        ///   <see cref="ColourAnimation.AnimationUserInfo"/> then this replaces the current animation. Otherwise this
        ///   is silently ignored.
        /// </param>
        public static void Flash(this ref ColourAnimation animation, Color targetColour, float duration,
            int priority = 1)
        {
            if (animation.Animating && animation.AnimationUserInfo >= priority)
                return;

            animation.AnimationStartColour = animation.CurrentColour;
            animation.AnimationTargetColour = targetColour;

            animation.AnimationDuration = duration;
            animation.AutoReverseAnimation = true;
            animation.AnimationElapsed = 0;
            animation.AnimationUserInfo = priority;

            animation.Animating = true;
        }

        /// <summary>
        ///   Stops animations and resets to default colour
        /// </summary>
        public static void ResetColour(this ref ColourAnimation animation)
        {
            animation.Animating = false;
            animation.AnimationTargetColour = animation.DefaultColour;
        }
    }
}
