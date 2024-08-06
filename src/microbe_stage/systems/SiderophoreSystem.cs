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
[RunsAfter(typeof(PhysicsCollisionManagementSystem))]
[RuntimeCost(0.5f, false)]
public sealed class SiderophoreSystem : AEntitySetSystem<float>
{
    private static ChunkConfiguration smallIronChunk = SimulationParameters.Instance.GetBiome("default")
        .Conditions.Chunks["ironSmallChunk"];

    private static Compound iron = SimulationParameters.Instance.GetCompound("iron");

    private readonly IWorldSimulation worldSimulation;

    public SiderophoreSystem(World world, IParallelRunner runner, IWorldSimulation worldSimulation) :
        base(world, runner)
    {
        this.worldSimulation = worldSimulation;
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var projectile = ref entity.Get<SiderophoreProjectile>();

        if (projectile.IsUsed)
            return;

        ref var collisions = ref entity.Get<CollisionManagement>();

        ref var sender = ref entity.Get<SiderophoreProjectile>().Sender;

        if (!projectile.ProjectileInitialized)
        {
            projectile.ProjectileInitialized = true;

            projectile.Amount = sender.Get<OrganelleContainer>().IronBreakdownEfficiency;

            collisions.StartCollisionRecording(Constants.MAX_SIMULTANEOUS_COLLISIONS_TINY);
        }

        // Check for active collisions that count as a hit and use up this projectile
        var count = collisions.GetActiveCollisions(out var activeCollisions);
        for (int i = 0; i < count; ++i)
        {
            ref var collision = ref activeCollisions![i];

            if (!HandleSiderophoreCollision(ref collision, in worldSimulation, ref projectile))
            {
                continue;
            }

            // Expire right now
            ref var timedLife = ref entity.Get<TimedLife>();
            timedLife.TimeToLiveRemaining = -1;

            ref var physics = ref entity.Get<Physics>();

            physics.DisableCollisionState = Physics.CollisionState.DisableCollisions;

            projectile.IsUsed = true;

            break;
        }
    }

    private static bool HandleSiderophoreCollision(ref PhysicsCollision collision,
        in IWorldSimulation worldSimulation, ref SiderophoreProjectile projectile)
    {
        var target = collision.SecondEntity;

        // Skip if hit something that's not a chunk
        if (!target.Has<CompoundStorage>())
            return false;

        ref var compounds = ref target.Get<CompoundStorage>();

        if (compounds.Compounds.Compounds.Count < 1)
            return false;

        if (target.Has<SiderophoreTarget>())
        {
            var efficiency = projectile.Amount;

            var size = Math.Max(Math.Min(efficiency / 3, 20), 1);

            smallIronChunk.ChunkScale = (float)Math.Sqrt(size);
            smallIronChunk.Size = Math.Min(size, compounds.Compounds.Compounds[iron]);
            smallIronChunk.Compounds![iron] = new ChunkConfiguration.ChunkCompound
            {
                Amount = smallIronChunk.Size,
            };

            SpawnHelpers.SpawnChunk(worldSimulation, smallIronChunk, collision.FirstEntity.Get<WorldPosition>()
                .Position, new Random(), false);

            // Spawn effect
            // TODO: New effect for siderophore collision with iron chunk
            SpawnHelpers.SpawnCellBurstEffect(worldSimulation, collision.FirstEntity.Get<WorldPosition>()
                .Position, efficiency - 2);
        }

        return true;
    }
}
