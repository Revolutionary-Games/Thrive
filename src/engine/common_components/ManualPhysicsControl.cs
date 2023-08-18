namespace Components
{
    using Godot;

    /// <summary>
    ///   Allows manual physics control over physical entities
    /// </summary>
    public struct ManualPhysicsControl
    {
        // Note: to allow multiple places in the code to use this this should have values added with += instead of
        // assigning to not remove the previous value.
        public Vector3 ImpulseToGive;
        public Vector3 AngularImpulseToGive;

        public bool RemoveVelocity;
        public bool RemoveAngularVelocity;

        /// <summary>
        ///   Needs to be set false whenever anything is changed here, otherwise the physics state is not applied
        /// </summary>
        public bool PhysicsApplied;
    }
}
