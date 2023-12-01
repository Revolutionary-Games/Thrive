namespace Components
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using DefaultEcs;
    using Newtonsoft.Json;

    /// <summary>
    ///   Physics object that detects objects inside it (similar to Godot <see cref="Godot.Area"/>)
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct PhysicsSensor
    {
        /// <summary>
        ///   The shape this sensor has. Must be non-null to activate the sensor functionality. Changing this after
        ///   the sensor is created only applies when <see cref="ApplyNewShape"/> is set.
        /// </summary>
        [JsonIgnore]
        public PhysicsShape? ActiveArea;

        /// <summary>
        ///   Set to a valid body by the sensor system while a shape is set and this is not disabled
        /// </summary>
        [JsonIgnore]
        public NativePhysicsBody? SensorBody;

        /// <summary>
        ///   Collisions detected by this sensor. Count of valid entries is in <see cref="ActiveCollisionCountPtr"/>.
        ///   Use the special helper
        /// </summary>
        [JsonIgnore]
        public PhysicsCollision[]? ActiveCollisions;

        /// <summary>
        ///   Pointer to the detected bodies count variable
        /// </summary>
        [JsonIgnore]
        public IntPtr ActiveCollisionCountPtr;

        /// <summary>
        ///   Sets the maximum number of simultaneous contacts this sensor can detect. Note changing this after the
        ///   sensor has been created has no effect.
        /// </summary>
        public int MaxActiveContacts;

        /// <summary>
        ///   If set to true then this sensor will not detect anything (gets disabled)
        /// </summary>
        public bool Disabled;

        /// <summary>
        ///   When set to true the sensor body uses kinematic movement type and detects sleeping bodies. If this is
        ///   false this only detects active bodies within the sensor. Must be set before the sensor is created,
        ///   doesn't apply retroactively.
        /// </summary>
        public bool DetectSleepingBodies;

        /// <summary>
        ///   If true then this detects static bodies. Should be left off unless explicitly needed as static body
        ///   detection has quite many caveats (and performance concerns). Doesn't apply retroactively after sensor
        ///   creation.
        /// </summary>
        public bool DetectStaticBodies;

        // TODO: for purely sensor type entities implement a bool here for automatically retrieving the shape from
        // PhysicsShapeHolder component

        /// <summary>
        ///   Set to true to re-apply <see cref="ActiveArea"/>
        /// </summary>
        public bool ApplyNewShape;

        /// <summary>
        ///   Internal variable, don't modify
        /// </summary>
        [JsonIgnore]
        public bool InternalDisabledState;

        public PhysicsSensor(int maxActiveContacts = Constants.MAX_SIMULTANEOUS_COLLISIONS_SENSOR)
        {
            MaxActiveContacts = maxActiveContacts;

            ActiveArea = null;
            SensorBody = null;
            Disabled = false;
            DetectSleepingBodies = false;
            DetectStaticBodies = false;

            ActiveCollisions = null;
            ActiveCollisionCountPtr = IntPtr.Zero;
            ApplyNewShape = false;
            InternalDisabledState = false;
        }
    }

    public static class PhysicsSensorHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetActiveCollisions(this ref PhysicsSensor physicsSensor, out PhysicsCollision[]? collisions)
        {
            // If state is not correct for reading
            collisions = physicsSensor.ActiveCollisions;
            if (collisions == null || physicsSensor.ActiveCollisionCountPtr.ToInt64() == 0)
            {
                return 0;
            }

            return Marshal.ReadInt32(physicsSensor.ActiveCollisionCountPtr);
        }

        public static void GetDetectedBodies(this ref PhysicsSensor physicsSensor, HashSet<Entity> resultEntities)
        {
            var count = physicsSensor.GetActiveCollisions(out var collisions);

            for (int i = 0; i < count; ++i)
            {
                ref var collision = ref collisions![i];

                resultEntities.Add(collision.SecondEntity);
            }
        }
    }
}
