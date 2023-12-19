namespace Systems
{
    using System;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.Command;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles delayed microbe colony operations that couldn't run immediately due to entities being spawned not
    ///   having <see cref="Entity"/> instances yet
    /// </summary>
    [With(typeof(DelayedMicrobeColony))]
    public sealed class DelayedColonyOperationSystem : AEntitySetSystem<float>
    {
        private readonly IWorldSimulation worldSimulation;
        private readonly ISpawnSystem spawnSystem;

        public DelayedColonyOperationSystem(IWorldSimulation worldSimulation, ISpawnSystem spawnSystem, World world,
            IParallelRunner runner) : base(world, runner, Constants.HUGE_MAX_SPAWNED_ENTITIES)
        {
            this.worldSimulation = worldSimulation;
            this.spawnSystem = spawnSystem;
        }

        public static void CreateDelayAttachedMicrobe(ref WorldPosition colonyPosition, in Entity colonyEntity,
            int colonyTargetIndex, CellTemplate cellTemplate, EarlyMulticellularSpecies species,
            IWorldSimulation worldSimulation,
            EntityCommandRecorder recorder, ISpawnSystem notifySpawnTo, bool giveStartingCompounds)
        {
            if (colonyTargetIndex == 0)
                throw new ArgumentException("Cannot delay add the root colony cell");

            int bodyPlanIndex = colonyTargetIndex;

            if (bodyPlanIndex < 0 || bodyPlanIndex >= species.Cells.Count)
            {
                GD.PrintErr($"Correcting incorrect body plan index for delay attached cell from {bodyPlanIndex} to " +
                    "a valid value");
                bodyPlanIndex = Mathf.Clamp(bodyPlanIndex, 0, species.Cells.Count - 1);
            }

            var attachPosition = new AttachedToEntity
            {
                AttachedTo = colonyEntity,
            };

            // For now we rely on absolute positions instead of needing to wait until all relevant membranes are ready
            // and calculate the attach position like that
            attachPosition.CreateMulticellularAttachPosition(cellTemplate.Position, cellTemplate.Orientation);

            var weight = SpawnHelpers.SpawnMicrobeWithoutFinalizing(worldSimulation, species,
                colonyPosition.Position + colonyPosition.Rotation.Xform(attachPosition.RelativePosition), true,
                (cellTemplate.CellType, bodyPlanIndex), recorder, out var member, MulticellularSpawnState.Bud,
                giveStartingCompounds);

            // Register with the spawn system to allow this entity to despawn if it gets cut off from the colony later
            // or attaching fails
            notifySpawnTo.NotifyExternalEntitySpawned(member,
                Constants.MICROBE_SPAWN_RADIUS * Constants.MICROBE_SPAWN_RADIUS, weight);

            member.Set(attachPosition);

            member.Set(new DelayedMicrobeColony(colonyEntity, colonyTargetIndex));

            // Ensure no physics is created before the attach completes
            member.Set(PhysicsHelpers.CreatePhysicsForMicrobe(true));
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var delayed = ref entity.Get<DelayedMicrobeColony>();

            var recorder = worldSimulation.StartRecordingEntityCommands();
            var recorderEntity = recorder.Record(entity);

            if (delayed.GrowAdditionalMembers > 0)
            {
                GrowColonyMembers(entity, recorderEntity, recorder, delayed.GrowAdditionalMembers);
            }
            else if (delayed.FinishAttachingToColony.IsAlive)
            {
                if (delayed.FinishAttachingToColony.Has<MicrobeColony>())
                {
                    ref var colony = ref delayed.FinishAttachingToColony.Get<MicrobeColony>();

                    lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
                    {
                        CompleteDelayedColonyAttach(ref colony, delayed.FinishAttachingToColony, entity,
                            recorder, delayed.AttachIndex);
                    }
                }
                else
                {
                    GD.PrintErr("Delayed attach target entity is missing colony, ignoring attach request");
                }
            }
            else
            {
                GD.PrintErr("Unknown operation for delayed microbe colony action");
            }

            // Remove the component now that it is processed
            recorderEntity.Remove<DelayedMicrobeColony>();

            worldSimulation.FinishRecordingEntityCommands(recorder);
        }

        private void GrowColonyMembers(in Entity entity, EntityRecord recorderEntity,
            EntityCommandRecorder recorder, int members)
        {
            if (!entity.Has<EarlyMulticellularSpeciesMember>())
            {
                GD.PrintErr("Only early multicellular species members can have delayed colony growth");
                return;
            }

            // First cell is at index 0 so it is always skipped (as it is the lead cell)
            int bodyPlanIndex = 1;

            if (!entity.Has<MicrobeColony>())
            {
                if (entity.Has<MicrobeColonyMember>())
                {
                    // This shouldn't happen as colony members shouldn't be able to collide
                    GD.PrintErr(
                        "Entity that is part of another microbe colony is trying to grow a colony in a delayed way");
                    return;
                }

                // Growing a new colony
                var colony = new MicrobeColony(true, entity, entity.Get<MicrobeControl>().State)
                {
                    // Mark as entity weight applied until the new members are attached
                    EntityWeightApplied = true,
                };

                recorderEntity.Set(colony);
            }
            else
            {
                // Growing to an existing colony
                ref var colony = ref entity.Get<MicrobeColony>();

                // Assume that things have been added in order for much more simple check here than needing to look
                // at multicellular growth etc. info here that might not even exist in all cases so a fallback like
                // this might be needed
                bodyPlanIndex = colony.ColonyMembers.Length;

                colony.EntityWeightApplied = true;
            }

            ref var species = ref entity.Get<EarlyMulticellularSpeciesMember>();

            bool added = false;
            var cellsToGrow = species.Species.Cells.Skip(bodyPlanIndex).Take(members);

            ref var parentPosition = ref entity.Get<WorldPosition>();

            foreach (var cellTemplate in cellsToGrow)
            {
                CreateDelayAttachedMicrobe(ref parentPosition, entity, bodyPlanIndex++, cellTemplate, species.Species,
                    worldSimulation, recorder, spawnSystem, true);

                added = true;
            }

            if (!added)
            {
                GD.Print("Delayed colony growth didn't add any new cells as add from index was higher than " +
                    "available cell count");
            }
        }

        private void CompleteDelayedColonyAttach(ref MicrobeColony colony, in Entity colonyEntity, in Entity entity,
            EntityCommandRecorder recorder, int targetMemberIndex)
        {
            var parentIndex = colony.CalculateSensibleParentIndexForMulticellular(ref entity.Get<AttachedToEntity>());
            colony.FinishQueuedMemberAdd(colonyEntity, parentIndex, entity, targetMemberIndex, recorder);
        }
    }
}
