namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using World = DefaultEcs.World;

/// <summary>
///   Handles siderophore projectile collisions
/// </summary>
[With(typeof(CollisionManagement))]
[With(typeof(Physics))]
[With(typeof(TimedLife))]
[With(typeof(SiderophoreProjectile))]
[RuntimeCost(0.5f, false)]
public sealed class SiderophoreSystem : AEntitySetSystem<float>
{
    /// <summary>
    ///   Holds a persistent instance of the collision filter callback to not need to create multiple delegates, and
    ///   to make doubly sure this callback won't be garbage collected while the native side still has a reference to
    ///   it.
    /// </summary>
    private readonly IWorldSimulation worldSimulation;

    public SiderophoreSystem(World world, IParallelRunner runner, IWorldSimulation worldSimulation) :
        base(world, runner)
    {
        this.worldSimulation = worldSimulation;
    }

    protected override void Update(float delta, in Entity entity)
    {
        if (!entity.Has<SiderophoreProjectile>())
            return;

        ref var projectile = ref entity.Get<SiderophoreProjectile>();

        if (projectile.IsUsed)
            return;

        ref var collisions = ref entity.Get<CollisionManagement>();

        if (!projectile.ProjectileInitialized)
        {
            projectile.ProjectileInitialized = true;

            collisions.StartCollisionRecording(Constants.MAX_SIMULTANEOUS_COLLISIONS_TINY);
        }

        // Check for active collisions that count as a hit and use up this projectile
        var count = collisions.GetActiveCollisions(out var activeCollisions);
        for (int i = 0; i < count; ++i)
        {
            ref var collision = ref activeCollisions![i];

            if (!HandleSiderophoreCollision(ref collision, in worldSimulation, entity
                    .Get<SiderophoreProjectile>().Sender))
            {
                continue;
            }

            // Applied a damaging hit, destroy this toxin
            // TODO: We should probably get some *POP* effect here.

            // Expire right now
            ref var timedLife = ref entity.Get<TimedLife>();
            timedLife.TimeToLiveRemaining = -1;

            ref var physics = ref entity.Get<Physics>();

            // TODO: should this instead of disabling the further collisions be removed from the world immediately
            // to cause less of a physics impact?
            // physics.BodyDisabled = true;
            physics.DisableCollisionState = Physics.CollisionState.DisableCollisions;

            // And make sure the flag we check for is set immediately to not process this projectile again
            // (this is just extra safety against the time over callback configuration not working correctly)
            projectile.IsUsed = true;

            // Only deal damage at most to a single thing
            break;
        }
    }

    private static bool HandleSiderophoreCollision(ref PhysicsCollision collision,
        in IWorldSimulation worldSimulation, Entity sender)
    {
        var target = collision.SecondEntity;

        // Skip if hit something that's not a chunk
        if (!target.Has<ChunkConfiguration>())
            return false;

        ref var configuration = ref target.Get<ChunkConfiguration>();

        var iron = SimulationParameters.Instance.GetCompound("iron");

        if (configuration.Compounds == null)
            return false;

        // Check if it is the big iron chunk
        if (configuration.Name == "BIG_IRON_CHUNK")
        {
            if (configuration.Compounds[iron].Amount > 0)
            {
                var smallIronChunk = SimulationParameters.Instance.GetBiome("default")
                    .Conditions.Chunks["ironSmallChunk"];

                var efficiency = sender.Get<OrganelleContainer>().IronBreakdownEfficiency;

                // Not to do operation twice
                var size = (float)Math.Min(efficiency / 3, 20);

                smallIronChunk.ChunkScale = (float)Math.Sqrt(size);
                smallIronChunk.Size = Math.Min(size, configuration.Compounds[iron].Amount);
                smallIronChunk.Compounds![iron] = new ChunkConfiguration.ChunkCompound
                {
                    Amount = smallIronChunk.Size,
                };

                // TODO: biting into iron should deplete it faster

                SpawnHelpers.SpawnChunk(worldSimulation, smallIronChunk, collision.FirstEntity.Get<WorldPosition>()
                    .Position, new Random(), false);

                // Spawn effect
                SpawnHelpers.SpawnCellBurstEffect(worldSimulation, collision.FirstEntity.Get<WorldPosition>()
                    .Position, efficiency - 2);
            }
        }

        return true;
    }
}
