namespace Systems
{
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Despawns entities with <see cref="CountLimited"/> when there are too many of them, starting from entities
    ///   the farthest from the player.
    /// </summary>
    [With(typeof(CountLimited))]
    [With(typeof(WorldPosition))]
    [ReadsComponent(typeof(CountLimited))]
    [ReadsComponent(typeof(WorldPosition))]
    public sealed class CountLimitedDespawnSystem : AEntitySetSystem<float>
    {
        private readonly IEntityContainer entityContainer;

        private readonly Dictionary<LimitGroup, EntityGroup> groupData = new();

        private int maxDespawnsPerFrame = Constants.MAX_DESPAWNS_PER_FRAME;

        private Vector3 playerPosition;

        public CountLimitedDespawnSystem(IEntityContainer entityContainer, World world) : base(world, null)
        {
            this.entityContainer = entityContainer;
        }

        public void ReportPlayerPosition(Vector3 position)
        {
            playerPosition = position;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var countLimited = ref entity.Get<CountLimited>();
            ref var position = ref entity.Get<WorldPosition>();

            if (!groupData.TryGetValue(countLimited.Group, out var group))
            {
                groupData[countLimited.Group] = group = new EntityGroup();

                switch (countLimited.Group)
                {
                    case LimitGroup.General:
                        break;

                    case LimitGroup.Chunk:
                        group.Limit = Constants.FLOATING_CHUNK_MAX_COUNT;
                        break;

                    case LimitGroup.ChunkSpawned:
                        group.Limit = Constants.FLOATING_CHUNK_MAX_COUNT;
                        break;

                    default:
                        GD.PrintErr("Unknown entity limit for group: ", countLimited.Group);
                        break;
                }
            }

            ++group.Count;

            // TODO: determine if this part at least could be run in parallel
            var distance = position.Position.DistanceSquaredTo(playerPosition);

            if (!group.HasFarthestEntity)
            {
                group.HasFarthestEntity = true;
                group.FarthestDistance = distance;
                group.FarthestEntity = entity;
                return;
            }

            if (distance > group.FarthestDistance)
            {
                group.FarthestDistance = distance;
                group.FarthestEntity = entity;
            }
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

            // Limit despawns per frame
            int despawnsLeft = maxDespawnsPerFrame;

            // Process all the groups and despawn the farthest entity from each group where the group size is over its
            // limit
            foreach (var pair in groupData)
            {
                var group = pair.Value;

                if (group.Count > group.Limit && group.HasFarthestEntity && despawnsLeft > 0)
                {
                    if (group.Limit < 1)
                    {
                        GD.PrintErr("Badly configured entity group limit");
                    }
                    else
                    {
                        // TODO: allow things like chunks to pop out their compounds when they are removed
                        // if (group.FarthestEntity.Has<CompoundStorage>())
                        // {
                        //
                        // }

                        if (!entityContainer.DestroyEntity(group.FarthestEntity))
                        {
                            GD.PrintErr("Count limited entity despawn failed");
                        }

                        --despawnsLeft;
                    }
                }

                // Clear the data to prepare for next frame
                group.Count = 0;
                group.FarthestDistance = float.MaxValue;
                group.HasFarthestEntity = false;
            }
        }

        private class EntityGroup
        {
            // For now only one entity of each group can be despawned per frame, this is probably good enough. This
            // design is done to not need a dynamically allocated list here
            public Entity FarthestEntity;
            public float FarthestDistance = float.MaxValue;

            public int Count;
            public int Limit = 100;

            /// <summary>
            ///   True when <see cref="FarthestEntity"/> has valid data, this is used instead of nullable field type
            ///   to avoid boxing of the data
            /// </summary>
            public bool HasFarthestEntity;
        }
    }
}
