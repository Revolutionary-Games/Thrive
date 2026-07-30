namespace Systems;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   A system that handles gamete movement "AI" and collisions that spawn new cells.
/// </summary>
/// <remarks>
///   <para>
///     This runs on the main thread as the gamete callback for the player reads a bit of Godot data.
///   </para>
/// </remarks>
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[WritesToComponent(typeof(PhysicsSensor))]
[RunsOnMainThread]
public partial class GameteSystem : BaseSystem<World, float>
{
    private const float CloseMergeDistanceSquared = 10.0f;
    private const float GametePushTogetherForce = 45000;

    private readonly IWorldSimulation worldSimulation;
    private readonly IMicrobeSpawnEnvironment spawnEnvironment;
    private readonly ISpawnSystem spawnSystem;
    private GameWorld? gameWorld;

    private Action<Entity, Entity, Vector3>? playerGameteCallback;

    public GameteSystem(IWorldSimulation worldSimulation, IMicrobeSpawnEnvironment spawnEnvironment,
        ISpawnSystem spawnSystem, World world) :
        base(world)
    {
        this.worldSimulation = worldSimulation;
        this.spawnEnvironment = spawnEnvironment;
        this.spawnSystem = spawnSystem;
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    /// <summary>
    ///   Set a callback that runs when a player gamete hits something and merges. This overrides the usual behaviour
    ///   of despawning the gametes and spawning a cell.
    /// </summary>
    /// <param name="callback">Callback to trigger</param>
    public void SetPlayerGameteCallback(Action<Entity, Entity, Vector3> callback)
    {
        playerGameteCallback = callback;
    }

    public void ClearPlayerGameteCallback()
    {
        playerGameteCallback = null;
    }

    public override void BeforeUpdate(in float delta)
    {
        base.BeforeUpdate(in delta);

        if (gameWorld == null)
            throw new InvalidOperationException("World not set");
    }

    public override void Dispose()
    {
        base.Dispose();
        playerGameteCallback = null;
        gameWorld = null;
    }

    [Query]
    [None<AttachedToEntity>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref GameteCell gamete, ref CellProperties cellProperties,
        ref MicrobeControl control, ref WorldPosition position, ref Physics physics,
        ref CollisionManagement collisionManagement, in Entity entity)
    {
        if (gamete.IsUsed)
            return;

        if (gamete.IsMerging)
        {
            HandleMerging(delta, ref gamete, ref physics, ref position, ref control, entity);
            return;
        }

        // Initialize sensor if not created
        if (!gamete.IsSensorCreated && cellProperties.CreatedMembrane != null)
        {
            InitializeSensor(ref gamete, ref cellProperties, entity);
        }

        // Detection logic
        bool detectedSomething = false;
        if (entity.Has<PhysicsSensor>())
        {
            detectedSomething = HandleDetection(ref gamete, ref control, ref position, entity);
        }

        if (!detectedSomething)
        {
            // Default movement (full speed forward)
            control.LookAtPoint = position.Position + position.Rotation * Vector3.Forward * 100;
            control.MovementDirection = Vector3.Forward;
        }

        // Collision logic
        HandleCollisions(ref gamete, ref collisionManagement, entity);
    }

    private void InitializeSensor(ref GameteCell gamete, ref CellProperties cellProperties, in Entity entity)
    {
        // Radius based on membrane (no bacteria adjustment as they shouldn't be doing sexual reproduction anyway)
        var radius = cellProperties.CreatedMembrane!.EncompassingCircleRadius;
        var sensorRadius = radius + 10;

        ref var sensor = ref entity.Get<PhysicsSensor>();
        sensor.ActiveArea = PhysicsShape.CreateSphere(sensorRadius);
        sensor.ApplyNewShape = true;

        gamete.IsSensorCreated = true;
    }

    private bool HandleDetection(ref GameteCell gamete, ref MicrobeControl control, ref WorldPosition position,
        in Entity entity)
    {
        Entity bestTarget = Entity.Null;
        float bestDistance = float.MaxValue;
        ref var sensor = ref entity.Get<PhysicsSensor>();
        Vector3 lookPosition = default;

        var count = sensor.GetActiveCollisions(out var collisions);
        for (int i = 0; i < count; i++)
        {
            var other = collisions![i].SecondEntity;
            if (other == entity || !other.IsAliveAndHas<GameteCell>() || !other.Has<WorldPosition>())
            {
                continue;
            }

            ref var otherGamete = ref other.Get<GameteCell>();

            // Don't allow self-fertilization but require same species and compatible types
            if (gamete.ForSpecies == otherGamete.ForSpecies &&
                IsCompatible(gamete.ThisGameteType, otherGamete.ThisGameteType) &&
                gamete.EmittedBy != otherGamete.EmittedBy)
            {
                var otherPosition = other.Get<WorldPosition>().Position;
                var squaredDistance = (otherPosition - position.Position).LengthSquared();
                if (squaredDistance < bestDistance)
                {
                    bestDistance = squaredDistance;
                    bestTarget = other;
                    lookPosition = otherPosition;
                }
            }
        }

        if (bestTarget == Entity.Null)
        {
            gamete.HasTarget = false;
            return false;
        }

        gamete.HasTarget = true;
        gamete.LockedOntoTarget = bestTarget;
        control.LookAtPoint = lookPosition;
        return true;
    }

    private void HandleCollisions(ref GameteCell gamete, ref CollisionManagement collisionManagement, in Entity entity)
    {
        // Active collisions should be set up by the spawn code
#if DEBUG
        if (collisionManagement.RecordActiveCollisions < 4)
        {
            GD.PrintErr("Gamete has invalid data in record collisions");
            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif

        var count = collisionManagement.GetActiveCollisions(out var collisions);
        for (int i = 0; i < count; i++)
        {
            var other = collisions![i].SecondEntity;
            if (other == entity || !other.IsAliveAndHas<GameteCell>())
                continue;

            if (gamete.IsMerging)
                return;

            ref var otherGamete = ref other.Get<GameteCell>();

            if (gamete.ForSpecies == otherGamete.ForSpecies &&
                IsCompatible(gamete.ThisGameteType, otherGamete.ThisGameteType) &&
                gamete.EmittedBy != otherGamete.EmittedBy)
            {
                // Start merging
                gamete.IsMerging = true;
                gamete.MergingTimePassed = 0;
                gamete.MergingWith = other;
                collisionManagement.AddTemporaryCollisionIgnoreWith(other);
                break;
            }
        }
    }

    private void HandleMerging(float delta, ref GameteCell gamete, ref Physics physics, ref WorldPosition position,
        ref MicrobeControl control, in Entity entity)
    {
        // TODO: maybe invalidate if distance has become too large for some reason?
        if (!gamete.MergingWith.IsAliveAndHas<GameteCell>() || !gamete.MergingWith.Has<WorldPosition>())
        {
            // Target invalid. Go back to normal.
            gamete.IsMerging = false;
            gamete.MergingWith = Entity.Null;
            return;
        }

#if DEBUG
        if (gamete.MergingWith == entity)
        {
            GD.PrintErr("Gamete trying to merge with itself");

            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif

        var otherPos = gamete.MergingWith.Get<WorldPosition>().Position;
        var vectorToOther = otherPos - position.Position;
        var distanceSquared = vectorToOther.LengthSquared();

        // Finish merging once close enough
        if (distanceSquared <= CloseMergeDistanceSquared)
        {
            // Only one of the merging gametes should spawn the offspring
            if (entity.Id < gamete.MergingWith.Id)
            {
                gamete.IsUsed = true;

                try
                {
                    ref var otherGamete = ref gamete.MergingWith.Get<GameteCell>();

                    // Mark the other as used as well so it does nothing
                    otherGamete.IsUsed = true;

                    bool despawn = SpawnOffspring(ref gamete, entity, ref otherGamete, gamete.MergingWith,
                        position.Position);

                    if (despawn)
                    {
                        worldSimulation.DestroyEntity(entity);
                        worldSimulation.DestroyEntity(gamete.MergingWith);
                    }
                    else
                    {
                        // Mark as used so that the entities won't be processed again (the special callback that took
                        // over, needs to handle deleting etc.)
                        gamete.IsUsed = true;
                        otherGamete.IsUsed = true;
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr("Error when merging gametes: ", e);

                    // Despawn for safety to not loop the error
                    worldSimulation.DestroyEntity(entity);
                    worldSimulation.DestroyEntity(gamete.MergingWith);
                }
            }

            return;
        }

        // Apply increasing physical force to move centres together.
        gamete.MergingTimePassed += delta;
        physics.QueuedImpulse +=
            vectorToOther.Normalized() * GametePushTogetherForce * delta * Math.Min(gamete.MergingTimePassed, 30);
        physics.QueuedForceApplied = false;

        // When merging only use slow movement (as we have detected something nearby)
        if(gamete.MergingTimePassed < 5)
        {
            control.MovementDirection = Vector3.Forward * 0.5f;
        }
        else if(gamete.MergingTimePassed < 10)
        {
            control.MovementDirection = Vector3.Forward * 0.3f;
        }
        else
        {
            control.MovementDirection = Vector3.Forward * 0.1f;
        }

        // And make sure this always looks as the other gamete cell
        control.LookAtPoint = otherPos;
    }

    private bool SpawnOffspring(ref GameteCell gamete, in Entity entity, ref GameteCell otherGamete,
        in Entity otherEntity, Vector3 spawnPosition)
    {
        if (gamete.IsPlayer || otherGamete.IsPlayer)
        {
            GD.Print("Player gamete has managed to merge");

            if (playerGameteCallback != null)
            {
                if (gamete.IsPlayer)
                {
                    playerGameteCallback(entity, otherEntity, spawnPosition);
                }
                else
                {
                    playerGameteCallback(otherEntity, entity, spawnPosition);
                }

                return false;
            }

            GD.Print("Player gamete callback is unset!");
        }

        var (recorder, weight) = SpawnHelpers.SpawnMicrobeWithoutFinalizing(worldSimulation, spawnEnvironment,
            gamete.ForSpecies, spawnPosition, true, (null, 0), out var spawnedEntity,
            MulticellularSpawnState.Offspring);

        // Make it despawn like normal
        spawnSystem.NotifyExternalEntitySpawned(spawnedEntity, recorder,
            Constants.MICROBE_DESPAWN_RADIUS_SQUARED, weight);

        SpawnHelpers.FinalizeEntitySpawn(recorder, worldSimulation);

        // Add the reproduction bonus which is bigger than normal due to the difficulty
        gameWorld!.AlterSpeciesPopulationInCurrentPatch(gamete.ForSpecies,
            Constants.CREATURE_REPRODUCE_SEXUAL_POPULATION_GAIN, Localization.Translate("REPRODUCED"));

        // Did normal spawn immediately, can despawn now
        return true;
    }

    /// <summary>
    ///   Basic gamete compatibility check that doesn't check for species compatibility
    /// </summary>
    /// <returns>True on being compatible</returns>
    private bool IsCompatible(GameteType a, GameteType b)
    {
        if (a == GameteType.All || b == GameteType.All)
            return true;
        if (a == GameteType.A && b == GameteType.B)
            return true;
        if (a == GameteType.B && b == GameteType.A)
            return true;

        return false;
    }
}
