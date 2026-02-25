namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;

/// <summary>
///   Handles delayed microbe colony operations that couldn't run immediately due to entities being spawned not
///   having <see cref="Entity"/> instances yet
/// </summary>
[WritesToComponent(typeof(AttachedToEntity))]
[WritesToComponent(typeof(MicrobeColony))]
[WritesToComponent(typeof(MicrobeControl))]
[WritesToComponent(typeof(CellProperties))]
[WritesToComponent(typeof(Physics))]
[WritesToComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(MulticellularSpeciesMember))]
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(ColonyBindingSystem))]
[RuntimeCost(0.25f)]
public partial class DelayedColonyOperationSystem : BaseSystem<World, float>
{
    private readonly object attachLock = new();
    private readonly List<(Entity Cell, DelayedMicrobeColony Delayed)> attachmentOrder = new();

    private readonly IComparer<(Entity Cell, DelayedMicrobeColony Delayed)> attachmentOrderComparer;

    private readonly IWorldSimulation worldSimulation;
    private readonly IMicrobeSpawnEnvironment spawnEnvironment;
    private readonly ISpawnSystem spawnSystem;

    public DelayedColonyOperationSystem(IWorldSimulation worldSimulation, IMicrobeSpawnEnvironment spawnEnvironment,
        ISpawnSystem spawnSystem, World world) :
        base(world)
    {
        this.worldSimulation = worldSimulation;
        this.spawnEnvironment = spawnEnvironment;
        this.spawnSystem = spawnSystem;

        attachmentOrderComparer = new AttachmentOrderComparer();
    }

    public static void CreateDelayAttachedMicrobe(ref WorldPosition colonyPosition, in Entity colonyEntity,
        int colonyTargetIndex, CellTemplate cellTemplate, MulticellularSpecies species,
        IWorldSimulation worldSimulation, IMicrobeSpawnEnvironment spawnEnvironment,
        CommandBuffer recorder, ISpawnSystem notifySpawnTo, bool giveStartingCompounds)
    {
        if (colonyTargetIndex == 0)
            throw new ArgumentException("Cannot delay add the root colony cell");

        int bodyPlanIndex = colonyTargetIndex;

        if (bodyPlanIndex < 0 || bodyPlanIndex >= species.ModifiableGameplayCells.Count)
        {
            GD.PrintErr($"Correcting incorrect body plan index for delay attached cell from {bodyPlanIndex} to " +
                "a valid value");
            bodyPlanIndex = Math.Clamp(bodyPlanIndex, 0, species.ModifiableGameplayCells.Count - 1);
        }

        var attachPosition = new AttachedToEntity
        {
            AttachedTo = colonyEntity,
        };

        // For now, we rely on absolute positions instead of needing to wait until all relevant membranes are ready
        // and calculate the attachment position like that
        attachPosition.CreateMulticellularAttachPosition(cellTemplate.Position, cellTemplate.Orientation);

        var weight = SpawnHelpers.SpawnMicrobeWithoutFinalizing(worldSimulation, spawnEnvironment, species,
            colonyPosition.Position + colonyPosition.Rotation * attachPosition.RelativePosition, true,
            (cellTemplate.ModifiableCellType, bodyPlanIndex), recorder, out var member, MulticellularSpawnState.Bud,
            giveStartingCompounds, colonyEntity.Has<PlayerMarker>());

        // Register with the spawn system to allow this entity to despawn if it gets cut off from the colony later
        // or attaching fails
        notifySpawnTo.NotifyExternalEntitySpawned(member, recorder, Constants.MICROBE_DESPAWN_RADIUS_SQUARED, weight);

        recorder.Add(member, attachPosition);

        recorder.Add(member, new DelayedMicrobeColony(colonyEntity, colonyTargetIndex));

        // Ensure no physics is created before the attach-operation completes
        recorder.Set(member, PhysicsHelpers.CreatePhysicsForMicrobe(true));

        if (colonyEntity.Has<MicrobeEventCallbacks>())
        {
            ref var originalEvents = ref colonyEntity.Get<MicrobeEventCallbacks>();

            if (!originalEvents.IsTemporary)
            {
                recorder.Add(member, originalEvents.CloneEventCallbacksForColonyMember());
            }
        }
    }

    public override void AfterUpdate(in float delta)
    {
        lock (attachLock)
        {
            if (attachmentOrder.Count == 0)
                return;

            var recorder = worldSimulation.StartRecordingEntityCommands();

            attachmentOrder.Sort(attachmentOrderComparer);

            foreach (var pair in attachmentOrder)
            {
                if (!pair.Delayed.FinishAttachingToColony.IsAlive())
                {
                    GD.PrintErr("Delayed attach target entity is dead, ignoring attach request");
                    continue;
                }

                if (!pair.Delayed.FinishAttachingToColony.Has<MicrobeColony>())
                {
                    GD.PrintErr("Delayed attach target entity is missing colony, ignoring attach request");
                    continue;
                }

                CompleteDelayedColonyAttach(ref pair.Delayed.FinishAttachingToColony.Get<MicrobeColony>(),
                    pair.Delayed.FinishAttachingToColony, pair.Cell, recorder, pair.Delayed.AttachIndex);
            }

            attachmentOrder.Clear();

            worldSimulation.FinishRecordingEntityCommands(recorder);
        }
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref DelayedMicrobeColony delayed, in Entity entity)
    {
        var recorder = worldSimulation.StartRecordingEntityCommands();

        if (delayed.GrowAdditionalMembers > 0)
        {
            GrowColonyMembers(entity, recorder, delayed.GrowAdditionalMembers);
        }
        else if (delayed.FinishAttachingToColony != Entity.Null)
        {
            lock (attachLock)
            {
                attachmentOrder.Add((entity, delayed));
            }
        }
        else
        {
            GD.PrintErr("Unknown operation for delayed microbe colony action");
        }

        // Remove the component now that it is processed
        recorder.Remove<DelayedMicrobeColony>(entity);

        worldSimulation.FinishRecordingEntityCommands(recorder);
    }

    private void GrowColonyMembers(in Entity entity, CommandBuffer recorder, int members)
    {
        if (!entity.Has<MulticellularSpeciesMember>())
        {
            GD.PrintErr("Only multicellular species members can have delayed colony growth");
            return;
        }

        // The first cell is at index 0, so it is always skipped (as it is the lead cell)
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

            recorder.Add(entity, colony);
        }
        else
        {
            // Growing to an existing colony
            ref var colony = ref entity.Get<MicrobeColony>();

            // Assume that things have been added in order for a much more simple check here than needing to look
            // at multicellular growth etc. info here that might not even exist in all cases so a fallback like
            // this might be needed
            bodyPlanIndex = colony.ColonyMembers.Length;

            colony.EntityWeightApplied = true;
        }

        ref var species = ref entity.Get<MulticellularSpeciesMember>();

        bool added = false;

        ref var parentPosition = ref entity.Get<WorldPosition>();

        for (int i = bodyPlanIndex; i < bodyPlanIndex + members && i < species.Species.ModifiableGameplayCells.Count;
             ++i)
        {
            CreateDelayAttachedMicrobe(ref parentPosition, entity, bodyPlanIndex++,
                species.Species.ModifiableGameplayCells[i], species.Species, worldSimulation, spawnEnvironment,
                recorder, spawnSystem, true);

            added = true;
        }

        if (!added)
        {
            GD.Print("Delayed colony growth didn't add any new cells as add from index was higher than " +
                "available cell count");
        }
    }

    private void CompleteDelayedColonyAttach(ref MicrobeColony colony, in Entity colonyEntity, in Entity entity,
        CommandBuffer recorder, int targetMemberIndex)
    {
        var parentIndex = colony.CalculateSensibleParentIndexForMulticellular(ref entity.Get<AttachedToEntity>());
        colony.FinishQueuedMemberAdd(colonyEntity, parentIndex, entity, targetMemberIndex, recorder);
    }

    private class AttachmentOrderComparer : IComparer<(Entity Cell, DelayedMicrobeColony Delayed)>
    {
        public int Compare((Entity Cell, DelayedMicrobeColony Delayed) first,
            (Entity Cell, DelayedMicrobeColony Delayed) second)
        {
            return first.Delayed.AttachIndex.CompareTo(second.Delayed.AttachIndex);
        }
    }
}
