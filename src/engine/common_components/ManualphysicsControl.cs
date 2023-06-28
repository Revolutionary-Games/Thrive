namespace Components
{
    using Godot;

    /// <summary>
    ///   Allows manual physics control over physical entities
    /// </summary>
    public struct ManualPhysicsControl
    {
        public Vector3 ImpulseToGive;
        public Vector3 AngularImpulseToGive;

        public Vector3? SetVelocity;
        public Vector3? SetAngularVelocity;

        public bool? DisableCollisions;

        public bool RemoveVelocity;
        public bool RemoveAngularVelocity;

        /// <summary>
        ///   Needs to be set false whenever anything is changed here, otherwise the physics state is not applied
        /// </summary>
        public bool PhysicsApplied;
    }
}
