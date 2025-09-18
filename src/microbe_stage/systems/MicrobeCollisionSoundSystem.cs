namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Components;

/// <summary>
///   Plays a sound effect when two cells collide hard enough
/// </summary>
[ReadsComponent(typeof(CollisionManagement))]
[ReadsComponent(typeof(CellProperties))]
[RunsAfter(typeof(PhysicsCollisionManagementSystem))]
[RunsBefore(typeof(SoundEffectSystem))]
public partial class MicrobeCollisionSoundSystem : BaseSystem<World, float>
{
    public MicrobeCollisionSoundSystem(World world) : base(world)
    {
    }

    [Query]
    [All<CellProperties>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref CollisionManagement collisionManagement, ref SoundEffectPlayer soundEffectPlayer)
    {
        var count = collisionManagement.GetActiveCollisions(out var collisions);
        if (count < 1)
            return;

        for (int i = 0; i < count; ++i)
        {
            ref var collision = ref collisions![i];

            // Only process just started collisions to not trigger the sound multiple times
            if (collision.JustStarted != 1)
                continue;

            if (collision.SecondEntity == Entity.Null)
                continue;

            // TODO: should collisions with any physics entities count?
            // For now collisions with just microbes count
            if (!collision.SecondEntity.Has<CellProperties>())
                continue;

            // Play bump sound if the collision is hard enough (there's enough physics bodies overlap)
            if (collision.PenetrationAmount > Constants.CONTACT_PENETRATION_TO_BUMP_SOUND)
            {
                // TODO: scale volume with the impact penetration
                soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-collision.ogg");
            }
        }
    }
}
