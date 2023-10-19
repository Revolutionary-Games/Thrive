namespace Components
{
    using Newtonsoft.Json;

    /// <summary>
    ///   Allows controlling <see cref="Godot.AnimationPlayer"/> in a <see cref="SpatialInstance"/> (note that if
    ///   spatial is recreated <see cref="AnimationApplied"/> needs to be set to false for the animation to reapply)
    /// </summary>
    public struct AnimationControl
    {
        // TODO: add speed / animation to play fields to make this generally useful

        /// <summary>
        ///   If not null will try to find the animation player to control based on this path starting from the
        ///   graphics instance of this entity
        /// </summary>
        public string? AnimationPlayerPath;

        /// <summary>
        ///   If set to true, all animations are stopped
        /// </summary>
        public bool StopPlaying;

        /// <summary>
        ///   Set to false when any properties change in this component to re-apply them
        /// </summary>
        [JsonIgnore]
        public bool AnimationApplied;
    }
}
