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

        public AttachedToEntity(in Entity parentEntity, Vector3 relativePosition, Quat relativeRotation)
        {
            AttachedTo = parentEntity;
            RelativePosition = relativePosition;
            RelativeRotation = relativeRotation;
        }
    }

    public static class AttachedToEntityHelpers
    {
        /// <summary>
        ///   Hold this lock whenever entity attach relationships are modified. This will hopefully ensure that we
        ///   don't end up with many very complex state bugs that trigger if some entity is tried to be attached to
        ///   multiple different places on exactly the same frame.
        /// </summary>
        public static readonly object EntityAttachRelationshipModifyLock = new();
    }
}
