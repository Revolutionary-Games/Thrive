namespace Systems;

using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   Makes sure the player's physics body is never allowed to sleep. This makes sure the microbe stage doesn't get
///   stuck as microbe movement cannot be applied if the physics world has only sleeping bodies (as the body
///   control apply operation will be skipped).
/// </summary>
/// <remarks>
///   <para>
///     Marked as just reading the physics as the body property modify locks the body on the native code side.
///   </para>
/// </remarks>
[ReadsComponent(typeof(PlayerMarker))]
[ReadsComponent(typeof(Physics))]
[RunsAfter(typeof(PhysicsBodyCreationSystem))]
[RunsAfter(typeof(PhysicsBodyDisablingSystem))]
[RuntimeCost(0.25f)]
public partial class DisallowPlayerBodySleepSystem : BaseSystem<World, float>
{
    private readonly PhysicalWorld physicalWorld;
    private WeakReference<NativePhysicsBody>? appliedSleepDisableTo;

    public DisallowPlayerBodySleepSystem(PhysicalWorld physicalWorld, World world) : base(world)
    {
        this.physicalWorld = physicalWorld;
    }

    [Query]
    [All<PlayerMarker>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref Physics physics)
    {
        if (!physics.IsBodyEffectivelyEnabled())
            return;

        if (appliedSleepDisableTo != null && appliedSleepDisableTo.TryGetTarget(out var appliedTo) &&
            ReferenceEquals(appliedTo, physics.Body))
        {
            return;
        }

        // Apply no sleep to the new body
        physicalWorld.SetBodyAllowSleep(physics.Body!, false);
        appliedSleepDisableTo = new WeakReference<NativePhysicsBody>(physics.Body!);
    }
}
