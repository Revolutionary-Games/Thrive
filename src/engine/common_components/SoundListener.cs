namespace Components
{
    /// <summary>
    ///   Places a <see cref="Godot.Listener"/> at this entity. Requires a <see cref="WorldPosition"/> to function.
    /// </summary>
    public struct SoundListener
    {
        /// <summary>
        ///   When set to true sound is set to come from the side of the screen relative to the
        ///   camera rather than using the entity's rotation.
        /// </summary>
        public bool UseTopDownRotation;

        public bool Disabled;
    }
}
