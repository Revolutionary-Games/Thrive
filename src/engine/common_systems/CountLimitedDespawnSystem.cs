namespace Systems;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Despawns entities with <see cref="CountLimited"/> when there are too many of them, starting from entities
///   the farthest from the player.
/// </summary>
[ReadsComponent(typeof(CountLimited))]
[ReadsComponent(typeof(WorldPosition))]
[RuntimeCost(1.5f)]
public partial class CountLimitedDespawnSystem : BaseSystem<World, float>
{
    private readonly IEntityContainer entityContainer;

    private readonly Dictionary<LimitGroup, EntityGroup> groupData = new();

    private int maxDespawnsPerFrame = Constants.MAX_DESPAWNS_PER_FRAME;

    private Vector3 playerPosition;

    public CountLimitedDespawnSystem(IEntityContainer entityContainer, World world) : base(world)
    {
        this.entityContainer = entityContainer;
    }

    public void ReportPlayerPosition(Vector3 position)
    {
        playerPosition = position;
    }

    public override void AfterUpdate(in float delta)
    {
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

            // Clear the data to prepare for the next frame
            group.Count = 0;
            group.FarthestDistance = float.MaxValue;
            group.HasFarthestEntity = false;
        }
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref CountLimited countLimited, ref WorldPosition position, in Entity entity)
    {
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

    private class EntityGroup
    {
        // For now only one entity of each group can be despawned per frame, this is probably good enough. This
        // design is done to not need a dynamically allocated list here
        public Entity FarthestEntity;
        public float FarthestDistance = float.MaxValue;

        public int Count;
        public int Limit = 100;

        /// <summary>
        ///   True when <see cref="FarthestEntity"/> has valid data, this is used instead of a nullable field type
        ///   to avoid boxing of the data
        /// </summary>
        public bool HasFarthestEntity;
    }
}
