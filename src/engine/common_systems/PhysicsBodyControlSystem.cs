namespace Systems;

using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Applies external (impulse, direct velocity) control to physics bodies
/// </summary>
[RunsAfter(typeof(PhysicsBodyCreationSystem))]
[RunsAfter(typeof(PhysicsBodyDisablingSystem))]
[RuntimeCost(4)]
public partial class PhysicsBodyControlSystem : BaseSystem<World, float>
{
    private readonly PhysicalWorld physicalWorld;

    public PhysicsBodyControlSystem(PhysicalWorld physicalWorld, World world) : base(world)
    {
        this.physicalWorld = physicalWorld;
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref Physics physics, ref ManualPhysicsControl control)
    {
        if (!physics.IsBodyEffectivelyEnabled())
        {
            // To make it simple to avoid bodies launching fast after being disabled, we eat up any queued forces here
            if (!physics.QueuedForceApplied)
            {
                physics.QueuedForceApplied = true;
                physics.QueuedImpulse = Vector3.Zero;
                physics.QueuedAngularImpulse = Vector3.Zero;
            }

            return;
        }

        var body = physics.Body!;

        if (control.PhysicsApplied && physics.VelocitiesApplied && physics.QueuedForceApplied)
            return;

        if (!physics.VelocitiesApplied)
        {
            physicalWorld.SetBodyVelocity(body, physics.Velocity, physics.AngularVelocity);
            physics.VelocitiesApplied = true;
        }

        if (!physics.QueuedForceApplied)
        {
            physicalWorld.GiveImpulse(body, physics.QueuedImpulse, true);
            physics.QueuedImpulse = Vector3.Zero;

            if (!physics.QueuedAngularImpulse.IsZeroApprox())
            {
                physicalWorld.GiveAngularImpulse(body, physics.QueuedAngularImpulse, true);
                physics.QueuedAngularImpulse = Vector3.Zero;
            }

            physics.QueuedForceApplied = false;
        }

        if (!control.PhysicsApplied)
        {
            if (control.RemoveVelocity && control.RemoveAngularVelocity)
            {
                control.RemoveVelocity = false;
                control.RemoveAngularVelocity = false;
                physicalWorld.SetBodyVelocity(body, Vector3.Zero, Vector3.Zero);
            }
            else if (control.RemoveVelocity)
            {
                control.RemoveVelocity = false;
                physicalWorld.SetOnlyBodyVelocity(body, Vector3.Zero);
            }
            else if (control.RemoveAngularVelocity)
            {
                control.RemoveAngularVelocity = false;
                physicalWorld.SetOnlyBodyAngularVelocity(body, Vector3.Zero);
            }

            if (control.ImpulseToGive != Vector3.Zero)
            {
                // To not have objects that sit around until touched and then shoot off at high velocity we
                // automatically activate bodies that have accumulated enough linear speed
                physicalWorld.GiveImpulse(body, control.ImpulseToGive, true);
                control.ImpulseToGive = Vector3.Zero;
            }

            if (control.AngularImpulseToGive != Vector3.Zero)
            {
                physicalWorld.GiveAngularImpulse(body, control.AngularImpulseToGive, true);
                control.AngularImpulseToGive = Vector3.Zero;
            }

            control.PhysicsApplied = true;
        }
    }
}
