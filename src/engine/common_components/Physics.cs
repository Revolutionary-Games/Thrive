namespace Components
{
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Physics body for an entity
    /// </summary>
    public struct Physics
    {
        /// <summary>
        ///   Allows direct physics state control. <see cref="VelocitiesApplied"/> need to be false for this to apply.
        ///   Only applies on body creation unless also <see cref="ManualPhysicsControl"/> component exists on the
        ///   current entity.
        /// </summary>
        public Vector3 Velocity;

        public Vector3 AngularVelocity;

        [JsonIgnore]
        public NativePhysicsBody? Body;

        public float? LinearDamping;

        /// <summary>
        ///   Angular damping. Note that this only applies if <see cref="LinearDamping"/> is also not null.
        /// </summary>
        public float? AngularDamping;

        /// <summary>
        ///   Set to false if the new velocities should apply to the entity
        /// </summary>
        [JsonIgnore]
        public bool VelocitiesApplied;

        /// <summary>
        ///   Set to false if new damping values are set
        /// </summary>
        [JsonIgnore]
        public bool DampingApplied;

        /// <summary>
        ///   When true <see cref="Velocity"/> is updated from the physics system each update
        /// </summary>
        public bool TrackVelocity;

        /// <summary>
        ///   If true when the body is created, this body is constrained to the Y-axis. This limitation exists because
        ///   there's currently no need to allow physics bodies to add / remove the axis lock dynamically
        /// </summary>
        public bool LockToYAxis;

        /// <summary>
        ///   When <see cref="LockToYAxis"/> is true and this is true then rotation is also locked to the axis
        /// </summary>
        public bool LockRotationWithAxisLock;

        // TODO: flags for teleporting the physics body to current WorldPosition and also overriding velocity + angular

        /// <summary>
        ///   When the body is disabled the body state is no longer read into the position variables allowing custom
        ///   control. And it is removed from the physics system to not interact with anything.
        /// </summary>
        public bool BodyDisabled;

        /// <summary>
        ///   Internal variable for the disable system, don't touch elsewhere
        /// </summary>
        [JsonIgnore]
        public bool InternalDisableState;
    }
}
