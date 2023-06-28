namespace Components
{
    using System;
    using System.Collections.Generic;
    using DefaultEcs;
    using Newtonsoft.Json;

    /// <summary>
    ///   Allows modifying <see cref="Physics"/> collisions of this entity
    /// </summary>
    public struct CollisionManagement
    {
        // TODO: some kind of physics callback registration

        public List<Entity>? IgnoredCollisionsWith;

        public bool AllCollisionsDisabled;

        /// <summary>
        ///   Must be set to false after changing any properties to have them apply (after the initial creation)
        /// </summary>
        public bool StateApplied;

        // The following variables are internal for the collision management system and should not be modified
        [JsonIgnore]
        public bool CurrentCollisionState;

        public delegate void OnCollidedWith(NativePhysicsBody body, int collidedSubShapeDataOurs,
            int collidedSubShapeDataTheirs);
    }

    public static class CollisionManagementExtensions
    {
        public static void RegisterCollisionCallback(this CollisionManagement entity,
            CollisionManagement.OnCollidedWith onCollided)
        {
            throw new NotImplementedException();
        }
    }
}
