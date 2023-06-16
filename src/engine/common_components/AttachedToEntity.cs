namespace Components
{
    using DefaultEcs;
    using Godot;

    /// <summary>
    ///   Entity data regarding being attached to another entity
    /// </summary>
    public struct AttachedToEntity
    {
        /// <summary>
        ///   Entity this is attached to. Should be valid whenever this component exists
        /// </summary>
        public Entity AttachedTo;

        /// <summary>
        ///   Position relative to the parent entity
        /// </summary>
        public Vector3 RelativePosition;

        /// <summary>
        ///   Rotation relative to the parent entity
        /// </summary>
        public Quat RelativeRotation;
    }
}
