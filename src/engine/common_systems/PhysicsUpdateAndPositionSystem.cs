namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Reads the physics state into position and also applies a few physics component state things
/// </summary>
[With(typeof(Physics))]
[With(typeof(WorldPosition))]
[RunsBefore(typeof(SpatialPositionSystem))]
[RuntimeCost(10)]
public sealed class PhysicsUpdateAndPositionSystem : AEntitySetSystem<float>
{
    private readonly PhysicalWorld physicalWorld;

    public PhysicsUpdateAndPositionSystem(PhysicalWorld physicalWorld, World world, IParallelRunner runner) : base(
        world, runner)
    {
        this.physicalWorld = physicalWorld;
    }

    /// <summary>
    ///   If true Y-axis fixed bodies are ensured they don't get too far away from Y=0. Should be unnecessary now
    ///   with
    /// </summary>
    public bool EnforceYPosition { get; set; }

    protected override void Update(float delta, in Entity entity)
    {
        ref var physics = ref entity.Get<Physics>();

        if (!physics.IsBodyEffectivelyEnabled())
            return;

        var body = physics.Body!;

        ref var position = ref entity.Get<WorldPosition>();

        // TODO: implement this operation
        // if (physics.TeleportBodyPosition || physics.TeleportBodyRotationAlso)
        // {
        //     if (physics.TeleportBodyRotationAlso)
        //     {
        //     }
        //     else
        //     {
        //         physics.BodyCreatedInWorld!.SetBodyPosition(body, position.Position);
        //     }
        // }

        (position.Position, position.Rotation) = physicalWorld.ReadBodyPosition(body);

        // TODO: it might be a good slight performance improvement to have a single native method to get both
        // the velocities and positions with a single native method call
        if (physics.TrackVelocity)
        {
            (physics.Velocity, physics.AngularVelocity) = physicalWorld.ReadBodyVelocity(body);
        }

        if (EnforceYPosition && (physics.AxisLock & Physics.AxisLockType.YAxis) != 0)
        {
            // Apply fixing to Y-position if drifted too far
            var driftAmount = Mathf.Abs(position.Position.Y);

            if (driftAmount > Constants.PHYSICS_ALLOWED_Y_AXIS_DRIFT)
            {
                physicalWorld.FixBodyYCoordinateToZero(body);
            }
        }

        // Apply updated damping values (physics body creation applies the initial value)
        if (!physics.DampingApplied)
        {
            physics.DampingApplied = true;

            if (physics.LinearDamping != null)
            {
                physicalWorld.SetDamping(body, physics.LinearDamping.Value, physics.AngularDamping);
            }
        }

        if (physics.DisableCollisionState != Physics.CollisionState.DoNotChange)
        {
            // Because the struct default data is 0 (false) we need to use a reversed value for the flag here
            bool wantedState = physics.DisableCollisionState == Physics.CollisionState.DisableCollisions;

            if (wantedState != physics.InternalDisableCollisionState)
            {
                physics.InternalDisableCollisionState = wantedState;

                // And then flip it again in this call
                physicalWorld.SetBodyCollisionsEnabledState(body, !wantedState);
            }
        }
    }
}
