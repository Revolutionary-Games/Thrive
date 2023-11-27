namespace Systems
{
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

        public DelayedColonyOperationSystem(IWorldSimulation worldSimulation, World world, IParallelRunner runner) :
            base(world, runner, Constants.HUGE_MAX_SPAWNED_ENTITIES)
        {
            this.worldSimulation = worldSimulation;
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
                var colony = new MicrobeColony(true, entity, entity.Get<MicrobeControl>().State);

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
            }

            ref var species = ref entity.Get<EarlyMulticellularSpeciesMember>();

            bool added = false;
            var cellsToGrow = species.Species.Cells.Skip(bodyPlanIndex).Take(members);

            ref var parentPosition = ref entity.Get<WorldPosition>();

            foreach (var cellTemplate in cellsToGrow)
            {
                var attachPosition = new AttachedToEntity
                {
                    AttachedTo = entity,
                };

                attachPosition.CreateMulticellularAttachPosition(cellTemplate.Position, cellTemplate.Orientation);

                SpawnHelpers.SpawnMicrobeWithoutFinalizing(worldSimulation, species.Species,
                    parentPosition.Position + parentPosition.Rotation.Xform(attachPosition.RelativePosition), true,
                    cellTemplate.CellType, recorder, out var member, MulticellularSpawnState.Bud);

                member.Set(attachPosition);

                member.Set(new DelayedMicrobeColony(entity, bodyPlanIndex++));

                // Ensure no physics is created before the attach completes
                member.Set(PhysicsHelpers.CreatePhysicsForMicrobe(true));

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
