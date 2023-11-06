﻿namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.Command;
    using DefaultEcs.System;
    using Godot;
    using Newtonsoft.Json;
    using Nito.Collections;

    // TODO: need to reimplement saving of the properties here
    /// <summary>
    ///   Spawns AI cells and other environmental things as the player moves around
    /// </summary>
    [JsonObject(IsReference = true)]
    public sealed class SpawnSystem : ISystem<float>, ISpawnSystem
    {
        private readonly EntitySet spawnedEntitiesSet;

        /// <summary>
        ///   Sets how often the spawn system runs and checks things
        /// </summary>
        [JsonProperty]
        private float interval = 1.0f;

        [JsonProperty]
        private float elapsed;

        [JsonProperty]
        private float despawnElapsed;

        private IWorldSimulation world;

        private Vector3 playerPosition;

        private ShuffleBag<Spawner> spawnTypes;

        [JsonProperty]
        private Random random = new();

        /// <summary>
        ///   This is used to spawn only a few entities per frame with minimal changes needed to code that wants to
        ///   spawn a bunch of stuff at once
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This isn't saved but the likelihood that losing out on spawning some things is not super critical.
        ///     Also it is probably the case that this isn't even used on most frames so it is perhaps uncommon
        ///     that there are queued things when saving. In addition it would be very hard to make sure all the
        ///     possible queued spawn type data (as it is mostly based on temporary lambdas) is saved.
        ///   </para>
        /// </remarks>
        private Deque<SpawnQueue> queuedSpawns = new();

        /// <summary>
        ///   Estimate count of existing spawned entities, cached to make delayed spawns cheaper
        /// </summary>
        private float estimateEntityCount;

        /// <summary>
        ///   Estimate count of existing spawn entities within the current spawn radius of the player;
        ///   Used to prevent a "spawn belt" of densely spawned entities when player doesn't move.
        /// </summary>
        [JsonProperty]
        private HashSet<Int2> coordinatesSpawned = new();

        public SpawnSystem(IWorldSimulation world)
        {
            this.world = world;
            spawnedEntitiesSet = world.EntitySystem.GetEntities().With<Spawned>().With<WorldPosition>().AsSet();

            spawnTypes = new ShuffleBag<Spawner>(random);
        }

        public bool IsEnabled { get; set; } = true;

        public void Update(float delta)
        {
            if (!IsEnabled)
                return;

            elapsed += delta;
            despawnElapsed += delta;

            float spawnsLeftThisFrame = Constants.MAX_SPAWNS_PER_FRAME;

            // If we have queued spawns to do spawn those
            HandleQueuedSpawns(ref spawnsLeftThisFrame);

            if (spawnsLeftThisFrame <= 0)
                return;

            // This is now an if to make sure that the spawn system is
            // only ran once per frame to avoid spawning a bunch of stuff
            // all at once after a lag spike
            // NOTE: that as QueueFree is used it's not safe to just switch this to a loop
            if (elapsed >= interval)
            {
                elapsed -= interval;

                estimateEntityCount = DespawnEntities();

                spawnTypes.RemoveAll(entity => entity.DestroyQueued);

                SpawnAllTypes(ref spawnsLeftThisFrame);
            }
            else if (despawnElapsed > Constants.DESPAWN_INTERVAL)
            {
                despawnElapsed = 0;

                DespawnEntities();
            }

            spawnedEntitiesSet.Complete();
        }

        /// <summary>
        ///   Adds a new spawner. Sets up the spawn radius, this radius squared,
        ///   and frequency fields based on the parameters of this
        ///   function.
        /// </summary>
        public void AddSpawnType(Spawner spawner, float spawnDensity, int spawnRadius)
        {
            spawner.SpawnRadius = spawnRadius;
            spawner.SpawnRadiusSquared = spawnRadius * spawnRadius;

            float minSpawnRadius = spawnRadius * Constants.MIN_SPAWN_RADIUS_RATIO;
            spawner.MinSpawnRadiusSquared = minSpawnRadius * minSpawnRadius;
            spawner.Density = spawnDensity;

            spawnTypes.Add(spawner);
        }

        /// <summary>
        ///   Removes a spawn type immediately. Note that it's easier to just set DestroyQueued to true on an spawner.
        /// </summary>
        public void RemoveSpawnType(Spawner spawner)
        {
            spawnTypes.Remove(spawner);
        }

        public void DespawnAll()
        {
            ClearSpawnQueue();

            float despawned = 0.0f;

            foreach (ref readonly var entity in spawnedEntitiesSet.GetEntities())
            {
                ref var spawned = ref entity.Get<Spawned>();

                if (spawned.DisallowDespawning)
                    continue;

                if (world.IsEntityInWorld(entity))
                {
                    despawned += spawned.EntityWeight;
                    world.DestroyEntity(entity);
                }
            }

            var debugOverlay = DebugOverlays.Instance;
            if (debugOverlay.PerformanceMetricsVisible)
                debugOverlay.ReportDespawns(despawned);

            ClearSpawnCoordinates();
        }

        /// <summary>
        ///   Clears all of the queued spawns. For use when the queue might contain something that
        ///   should not be allowed to spawn.
        /// </summary>
        public void ClearSpawnQueue()
        {
            foreach (var queuedSpawn in queuedSpawns)
                queuedSpawn.Dispose();

            queuedSpawns.Clear();
        }

        /// <summary>
        ///   Forgets all record of where clouds have spawned, so clouds can spawn anywhere.
        /// </summary>
        public void ClearSpawnCoordinates()
        {
            coordinatesSpawned.Clear();
        }

        public void ReportPlayerPosition(Vector3 position)
        {
            playerPosition = position;

            // Remove the y-position from player position
            playerPosition.y = 0;
        }

        public void NotifyExternalEntitySpawned(in EntityRecord entity, float despawnRadiusSquared, float entityWeight)
        {
            if (entityWeight <= 0)
                throw new ArgumentException("weight needs to be positive", nameof(entityWeight));

            entity.Set(new Spawned
            {
                DespawnRadiusSquared = despawnRadiusSquared,
                EntityWeight = entityWeight,
            });

            // Update entity count estimate to keep this about up to date, this will be corrected within a few seconds
            // with the next spawn cycle to be exactly correct
            estimateEntityCount += entityWeight;
        }

        public bool IsUnderEntityLimitForReproducing()
        {
            return estimateEntityCount < Settings.Instance.MaxSpawnedEntities.Value *
                Constants.REPRODUCTION_ALLOW_EXCEED_ENTITY_LIMIT_MULTIPLIER;
        }

        /// <summary>
        ///   Ensures that the entity limit is not overfilled by a lot after player reproduction by force despawning
        ///   things
        /// </summary>
        public void EnsureEntityLimitAfterPlayerReproduction(Vector3 keepEntitiesNear, Entity doNotDespawn)
        {
            float extra = 0;

            if (doNotDespawn != default && doNotDespawn.Has<Spawned>())
            {
                // Take the just spawned thing we shouldn't despawn into account in the entity count as our estimate
                // won't likely include it yet
                extra = doNotDespawn.Get<Spawned>().EntityWeight;
            }

            var entityLimit = Settings.Instance.MaxSpawnedEntities.Value;

            float limitExcess = estimateEntityCount + extra - entityLimit *
                Constants.REPRODUCTION_PLAYER_ALLOWED_ENTITY_LIMIT_EXCEED;

            if (limitExcess < 1)
                return;

            // We need to despawn something
            GD.Print("After player reproduction entity limit is exceeded, will force despawn something");

            float playerReproductionWeight = 0;

            var playerReproducedEntities = new List<(Entity Entity, Vector3 Position, float Weight)>();

            foreach (var entity in world.EntitySystem.GetEntities().With<PlayerOffspring>().With<Spawned>()
                         .With<WorldPosition>().AsEnumerable())
            {
                if (world.IsQueuedForDeletion(entity))
                    continue;

                ref var spawned = ref entity.Get<Spawned>();

                if (spawned.DisallowDespawning)
                    continue;

                playerReproductionWeight += spawned.EntityWeight;

                if (doNotDespawn == default || entity != doNotDespawn)
                {
                    ref var position = ref entity.Get<WorldPosition>();

                    playerReproducedEntities.Add((entity, position.Position, spawned.EntityWeight));
                }
            }

            // Despawn one player reproduced copy first if the player reproduced copies are taking up a ton of space
            if (playerReproductionWeight > entityLimit * Constants.PREFER_DESPAWN_PLAYER_REPRODUCED_COPY_AFTER &&
                playerReproducedEntities.Count > 0)
            {
                var despawn = playerReproducedEntities
                    .OrderByDescending(s => s.Position.DistanceSquaredTo(keepEntitiesNear)).First();

                estimateEntityCount -= despawn.Weight;
                limitExcess -= despawn.Weight;

                world.DestroyEntity(despawn.Entity);
            }

            if (limitExcess <= 1)
                return;

            // We take weight as well as distance into account here to not just despawn a ton of really far away objects
            // with weight of 1
            using var deSpawnableEntities = world.EntitySystem.GetEntities().With<Spawned>()
                .With<WorldPosition>().AsEnumerable().Where(e => !e.Get<Spawned>().DisallowDespawning)
                .Select(e =>
                {
                    ref var spawned = ref e.Get<Spawned>();
                    ref var position = ref e.Get<WorldPosition>();

                    return (e, spawned.EntityWeight, position.Position) as (Entity Entity, float EntityWeight, Vector3
                        Position)?;
                })
                .OrderByDescending(t =>
                    Math.Log(t!.Value.Position.DistanceSquaredTo(keepEntitiesNear)) + Math.Log(t.Value.EntityWeight))
                .GetEnumerator();

            // Then try to despawn enough stuff for us to get under the limit
            while (limitExcess >= 1)
            {
                (Entity Entity, float EntityWeight, Vector3 Position)? bestCandidate = null;

                if (deSpawnableEntities.MoveNext() && deSpawnableEntities.Current != null)
                    bestCandidate = deSpawnableEntities.Current;

                if (doNotDespawn != default && bestCandidate?.Entity == doNotDespawn)
                    continue;

                if (bestCandidate != null && world.IsQueuedForDeletion(bestCandidate.Value.Entity))
                    continue;

                if (bestCandidate != null)
                {
                    var weight = bestCandidate.Value.EntityWeight;
                    estimateEntityCount -= weight;
                    limitExcess -= weight;
                    world.DestroyEntity(bestCandidate.Value.Entity);

                    continue;
                }

                // If we couldn't despawn anything sensible, give up
                GD.PrintErr("Force despawning could not find enough things to despawn");
                break;
            }
        }

        public void Dispose()
        {
            spawnedEntitiesSet.Dispose();
        }

        private void HandleQueuedSpawns(ref float spawnsLeftThisFrame)
        {
            float spawnedCount = 0.0f;

            // Spawn from the queue
            while (spawnsLeftThisFrame > 0 && queuedSpawns.Count > 0)
            {
                var spawn = queuedSpawns.First();

                bool finished = false;

                while (estimateEntityCount < Settings.Instance.MaxSpawnedEntities.Value &&
                       spawnsLeftThisFrame > 0)
                {
                    // Disallow spawning too close to the player
                    spawn.CheckIsSpawningStillPossible(playerPosition);

                    if (spawn.Ended)
                    {
                        finished = true;
                        break;
                    }

                    // Next can be spawned
                    var (recorder, weight) = spawn.SpawnNext(out var current);

                    AddSpawnedComponent(current, weight, spawn.RelatedSpawnType);
                    SpawnHelpers.FinalizeEntitySpawn(recorder, world);

                    estimateEntityCount += weight;
                    spawnsLeftThisFrame -= weight;
                    spawnedCount += weight;
                }

                if (finished)
                {
                    // Finished spawning everything from this enumerator, if we didn't finish we save this spawn for the
                    // next queued spawns handling cycle
                    queuedSpawns.RemoveFromFront();
                    spawn.Dispose();
                }
                else
                {
                    break;
                }
            }

            if (spawnedCount > 0)
            {
                var debugOverlay = DebugOverlays.Instance;

                if (debugOverlay.PerformanceMetricsVisible)
                    debugOverlay.ReportSpawns(spawnedCount);
            }
        }

        private void SpawnAllTypes(ref float spawnsLeftThisFrame)
        {
            var playerCoordinatePoint = new Tuple<int, int>(Mathf.RoundToInt(playerPosition.x /
                Constants.SPAWN_SECTOR_SIZE), Mathf.RoundToInt(playerPosition.z / Constants.SPAWN_SECTOR_SIZE));

            // Spawn for all sectors immediately outside a 3x3 box around the player
            var sectorsToSpawn = new List<Int2>(12);
            for (int y = -1; y <= 1; y++)
            {
                sectorsToSpawn.Add(new Int2(playerCoordinatePoint.Item1 - 2, playerCoordinatePoint.Item2 + y));
            }

            for (int x = -1; x <= 1; x++)
            {
                sectorsToSpawn.Add(new Int2(playerCoordinatePoint.Item1 + 2, playerCoordinatePoint.Item2 + x));
            }

            for (int y = -1; y <= 1; y++)
            {
                sectorsToSpawn.Add(new Int2(playerCoordinatePoint.Item1 + y, playerCoordinatePoint.Item2 - 2));
            }

            for (int x = -1; x <= 1; x++)
            {
                sectorsToSpawn.Add(new Int2(playerCoordinatePoint.Item1 + x, playerCoordinatePoint.Item2 + 2));
            }

            foreach (var newSector in sectorsToSpawn)
            {
                if (coordinatesSpawned.Add(newSector))
                {
                    SpawnInSector(newSector, ref spawnsLeftThisFrame);
                }
            }

            // Only spawn microbes around the player if below the threshold.
            // This is to prioritize spawning in sectors.
            float entitiesThreshold = Settings.Instance.MaxSpawnedEntities.Value *
                Constants.ENTITY_SPAWNING_AROUND_PLAYER_THRESHOLD;
            if (estimateEntityCount < entitiesThreshold)
            {
                SpawnMicrobesAroundPlayer(playerPosition, ref spawnsLeftThisFrame);
            }
        }

        /// <summary>
        ///   Handles all spawning for this section of the play area, as it will look when the player enters. Does NOT
        ///   handle recording that the sector was spawned.
        /// </summary>
        /// <param name="sector">
        ///   X/Y coordinates of the sector to be spawned, in <see cref="Constants.SPAWN_SECTOR_SIZE" /> units
        /// </param>
        /// <param name="spawnsLeftThisFrame">How many spawns are still allowed this frame</param>
        private void SpawnInSector(Int2 sector, ref float spawnsLeftThisFrame)
        {
            float spawns = 0.0f;

            foreach (var spawnType in spawnTypes)
            {
                if (SpawnsBlocked(spawnType))
                    continue;

                var sectorCenter = new Vector3(sector.x * Constants.SPAWN_SECTOR_SIZE, 0,
                    sector.y * Constants.SPAWN_SECTOR_SIZE);

                // Distance from the sector center.
                var displacement = new Vector3(random.NextFloat() * Constants.SPAWN_SECTOR_SIZE -
                    (Constants.SPAWN_SECTOR_SIZE / 2), 0,
                    random.NextFloat() * Constants.SPAWN_SECTOR_SIZE - (Constants.SPAWN_SECTOR_SIZE / 2));

                spawns += SpawnWithSpawner(spawnType, sectorCenter + displacement, ref spawnsLeftThisFrame);
            }

            var debugOverlay = DebugOverlays.Instance;

            if (debugOverlay.PerformanceMetricsVisible)
                debugOverlay.ReportSpawns(spawns);
        }

        private void SpawnMicrobesAroundPlayer(Vector3 playerLocation, ref float spawnsLeftThisFrame)
        {
            var angle = random.NextFloat() * 2 * Mathf.Pi;

            float spawns = 0.0f;
            foreach (var spawnType in spawnTypes)
            {
                if (!SpawnsBlocked(spawnType) && spawnType is MicrobeSpawner)
                {
                    spawns += SpawnWithSpawner(spawnType,
                        playerLocation + new Vector3(Mathf.Cos(angle) * Constants.SPAWN_SECTOR_SIZE * 2, 0,
                            Mathf.Sin(angle) * Constants.SPAWN_SECTOR_SIZE * 2), ref spawnsLeftThisFrame);
                }
            }

            var debugOverlay = DebugOverlays.Instance;

            if (debugOverlay.PerformanceMetricsVisible)
                debugOverlay.ReportSpawns(spawns);
        }

        /// <summary>
        ///   Checks whether we're currently blocked from spawning this type
        /// </summary>
        private bool SpawnsBlocked(Spawner spawnType)
        {
            return spawnType.SpawnsEntities && estimateEntityCount >= Settings.Instance.MaxSpawnedEntities.Value;
        }

        /// <summary>
        ///   Does a single spawn with a spawner. Does NOT check we're under the entity limit.
        /// </summary>
        private float SpawnWithSpawner(Spawner spawnType, Vector3 location, ref float spawnsLeftThisFrame)
        {
            float spawns = 0.0f;

            if (random.NextFloat() > spawnType.Density)
            {
                return spawns;
            }

            var spawnQueue = spawnType.Spawn(world, location, this);

            // Non-entity type spawn
            if (spawnQueue == null)
                return spawns;

            bool finished = false;

            while (spawnsLeftThisFrame > 0)
            {
                spawnQueue.CheckIsSpawningStillPossible(playerPosition);

                if (spawnQueue.Ended)
                {
                    finished = true;
                    break;
                }

                var (recorder, weight) = spawnQueue.SpawnNext(out var current);

                AddSpawnedComponent(current, weight, spawnType);
                SpawnHelpers.FinalizeEntitySpawn(recorder, world);

                spawns += weight;
                estimateEntityCount += weight;
                spawnsLeftThisFrame -= weight;
            }

            if (!finished)
            {
                // Store the remaining items in the enumerator for later
                queuedSpawns.AddToBack(spawnQueue);
            }
            else
            {
                spawnQueue.Dispose();
            }

            return spawns;
        }

        /// <summary>
        ///   Despawns entities that are far away from the player
        /// </summary>
        /// <returns>The number of alive entities (combined weight), used to limit the total</returns>
        private float DespawnEntities()
        {
            float entitiesDeleted = 0.0f;
            float spawnedEntityWeight = 0.0f;

            int despawnedCount = 0;

            foreach (ref readonly var entity in spawnedEntitiesSet.GetEntities())
            {
                ref var spawned = ref entity.Get<Spawned>();

                if (spawned.DisallowDespawning)
                    continue;

                var entityWeight = spawned.EntityWeight;
                spawnedEntityWeight += entityWeight;

                // Keep counting all entities to have an accurate count at the end of this loop, even if we are no
                // longer allowed to despawn things
                if (despawnedCount >= Constants.MAX_DESPAWNS_PER_FRAME)
                    continue;

                // Global position must be used here as otherwise colony members are despawned
                // This should now just process the colony lead cells as this now uses GetChildrenToProcess, but
                // GlobalTransform is kept here just for good measure to make sure the distances are accurate.
                ref var position = ref entity.Get<WorldPosition>();
                var squaredDistance = (playerPosition - position.Position).LengthSquared();

                // If the entity is too far away from the player, despawn it.
                if (squaredDistance > spawned.DespawnRadiusSquared)
                {
                    entitiesDeleted += entityWeight;
                    world.DestroyEntity(entity);

                    ++despawnedCount;
                }
            }

            var debugOverlay = DebugOverlays.Instance;

            if (debugOverlay.PerformanceMetricsVisible)
                debugOverlay.ReportDespawns(entitiesDeleted);

            return spawnedEntityWeight - entitiesDeleted;
        }

        private void AddSpawnedComponent(in EntityRecord entity, float weight, Spawner spawnType)
        {
            float radius = spawnType.SpawnRadius + Constants.DESPAWN_RADIUS_OFFSET;

            entity.Set(new Spawned
            {
                DespawnRadiusSquared = radius * radius,
                EntityWeight = weight,
            });
        }
    }
}
