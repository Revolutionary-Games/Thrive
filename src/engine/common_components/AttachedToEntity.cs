namespace Components
{
    using DefaultEcs;
    using Godot;

    /// <summary>
    ///   Entity data regarding being attached to another entity
    /// </summary>
    [JSONDynamicTypeAllowed]
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
        public Quaternion RelativeRotation;

        /// <summary>
        ///   If true then this entity is deleted if <see cref="AttachedTo"/> is deleted (handled by the attached
        ///   position system)
        /// </summary>
        public bool DeleteIfTargetIsDeleted;

        public AttachedToEntity(in Entity parentEntity, Vector3 relativePosition, Quaternion relativeRotation)
        {
            AttachedTo = parentEntity;
            RelativePosition = relativePosition;
            RelativeRotation = relativeRotation;
            DeleteIfTargetIsDeleted = false;
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

        /// <summary>
        ///   Creates the position in a cell colony for multicellular body plan part. Note that unlike before this uses
        ///   the hard position set in the body plan to avoid some attach weirdness bugs, though now this is much more
        ///   dependent on intercellular matrix graphics to look good (which at the time of writing is not implemented)
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     See the TODO on <see cref="Constants.MULTICELLULAR_CELL_DISTANCE_MULTIPLIER"/> about how this doesn't
        ///     exactly work perfectly, but at least removes one different bug.
        ///   </para>
        /// </remarks>
        public static void CreateMulticellularAttachPosition(this ref AttachedToEntity attachedToEntity,
            Hex cellTemplatePosition, int cellTemplateOrientation)
        {
            attachedToEntity.RelativePosition = Hex.AxialToCartesian(cellTemplatePosition) *
                Constants.MULTICELLULAR_CELL_DISTANCE_MULTIPLIER;

            attachedToEntity.RelativeRotation = MathUtils.CreateRotationForOrganelle(cellTemplateOrientation);
        }
    }
}
