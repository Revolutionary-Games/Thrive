﻿namespace Components
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
        /// <remarks>
        ///   <para>
        ///     This is actually saved (unlike many applied state variables) as the velocities applied to the physics
        ///     object are persistent state as they have already affected the physics object properties.
        ///   </para>
        /// </remarks>
        public bool PhysicsApplied;
    }
}
