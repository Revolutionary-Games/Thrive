namespace Components;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Physics body for an entity
/// </summary>
[JSONDynamicTypeAllowed]
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
    ///   Sets the axis lock type applied when the body is created (for example constraining to the Y-axis).
    ///   This limitation exists because there's currently no need to allow physics bodies to add / remove the
    ///   axis lock dynamically, so if this value is changed then the body needs to be forcefully recreated.
    /// </summary>
    public AxisLockType AxisLock;

    /// <summary>
    ///   When set to <see cref="CollisionState.DisableCollisions"/>, this disables all *further*
    ///   collisions for the object. This doesn't stop any existing collisions. To do that the physics body needs
    ///   to be removed entirely from the world with <see cref="Physics.BodyDisabled"/>.
    /// </summary>
    public CollisionState DisableCollisionState;

    // TODO: flags for teleporting the physics body to current WorldPosition

    /// <summary>
    ///   When the body is disabled the body state is no longer read into the position variables allowing custom
    ///   control. And it is removed from the physics system to not interact with anything. Note that if the
    ///   <see cref="Systems.PhysicsBodyDisablingSystem"/> has not run yet the actual state might not match.
    ///   So use
    /// </summary>
    public bool BodyDisabled;

    /// <summary>
    ///   Internal variable for the disable system, don't touch elsewhere
    /// </summary>
    [JsonIgnore]
    public bool InternalDisableState;

    [JsonIgnore]
    public bool InternalDisableCollisionState;

    [Flags]
    public enum AxisLockType : byte
    {
        None = 0,
        YAxis = 1,
        AlsoLockRotation = 2,
        YAxisWithRotation = 3,
    }

    public enum CollisionState : byte
    {
        DoNotChange = 0,
        EnableCollisions = 1,
        DisableCollisions = 2,
    }
}

public static class PhysicsHelpers
{
    public static void SetCollisionDisableState(this ref Physics physics, bool disableCollisions)
    {
        physics.DisableCollisionState = disableCollisions ?
            Physics.CollisionState.DisableCollisions :
            Physics.CollisionState.EnableCollisions;
    }

    /// <summary>
    ///   Returns true only if the body is created and not currently disabled (or waiting to be re-enabled)
    /// </summary>
    /// <returns>True when body is fully usable</returns>
    public static bool IsBodyEffectivelyEnabled(this ref Physics physics)
    {
        return physics.Body != null && !physics.BodyDisabled && !physics.InternalDisableState;
    }

    public static Physics CreatePhysicsForMicrobe(bool disabledInitially = false)
    {
        return new Physics
        {
            AxisLock = Physics.AxisLockType.YAxisWithRotation,
            LinearDamping = Constants.MICROBE_PHYSICS_DAMPING,
            AngularDamping = Constants.MICROBE_PHYSICS_DAMPING_ANGULAR,
            TrackVelocity = true,
            BodyDisabled = disabledInitially,
        };
    }
}
