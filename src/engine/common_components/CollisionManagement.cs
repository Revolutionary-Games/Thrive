namespace Components
{
    using System.Collections.Generic;
    using DefaultEcs;
    using Newtonsoft.Json;

    /// <summary>
    ///   Allows modifying <see cref="Physics"/> collisions of this entity
    /// </summary>
    public struct CollisionManagement
    {
        /// <summary>
        ///   Collisions experienced by this entity note that <see cref="RecordActiveCollisions"/> needs to be 1 or
        ///   more for this list to the populated. Don't reassign this list as otherwise it will stop being updated
        ///   by the underlying physics body.
        /// </summary>
        [JsonIgnore]
        public PhysicsCollision[]? ActiveCollisions;

        public List<Entity>? IgnoredCollisionsWith;

        /// <summary>
        ///   When specified this callback is called before any physics collisions are allowed to happen. Returning
        ///   false will prevent that collision. Note that no state should be modified (that is not completely
        ///   thread-safe and entity order safe) by this. Also this will increase the physics processing expensiveness
        ///   of an entity so if at all possible other approaches should be used to filter out unwanted collisions.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     TODO: plan if this should be saved (in which case some objects don't want their callbacks to save,
        ///     for example the toxin collision system) or if all systems will need to reapply their filters after load
        ///   </para>
        /// </remarks>
        [JsonIgnore]
        public OnCollided? CollisionFilter;

        /// <summary>
        ///   When set above 0 up to this many collisions are recorded in <see cref="ActiveCollisions"/>
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Note that increasing or lowering this value after recording has been enabled has no effect. All
        ///     entities should just initially figure out how many max collisions they should handle.
        ///   </para>
        /// </remarks>
        public int RecordActiveCollisions;

        public bool AllCollisionsDisabled;

        /// <summary>
        ///   Must be set to false after changing any properties to have them apply (after the initial creation)
        /// </summary>
        public bool StateApplied;

        // The following variables are internal for the collision management system and should not be modified
        [JsonIgnore]
        public bool CurrentCollisionState;

        [JsonIgnore]
        public bool CollisionFilterCallbackRegistered;

        public delegate bool OnCollided(ref PhysicsCollision collision);
    }
}
