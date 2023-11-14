namespace Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DefaultEcs;
    using DefaultEcs.Command;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Microbe colony newMember. This component is added to the colony lead cell. This contains the overall info
    ///   about the cell colony or early multicellular creature.
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct MicrobeColony
    {
        /// <summary>
        ///   All colony members of this colony. The cell at index 0 has to be the <see cref="Leader"/>. Only modify
        ///   this data through the helper methods to ensure everything is consistent.
        /// </summary>
        public Entity[] ColonyMembers;

        /// <summary>
        ///   Lead cell of the colony. This is the newMember that exists separately in the world, all others are
        ///   attached to it with <see cref="AttachedToEntity"/> components. Note this is always assumed to be the
        ///   same as the entity that has this <see cref="MicrobeColony"/> component on it.
        /// </summary>
        public Entity Leader;

        /// <summary>
        ///   This maps parent cells to their children in the colony hierarchy. All cells are merged into the leader,
        ///   but certain operations like removing cells need to not leave gaps in the colony for that this is used to
        ///   detect which cells are also lost if one cell is lost. Key is the dependent cell and the value is its
        ///   parent.
        /// </summary>
        public Dictionary<Entity, Entity> ColonyStructure;

        /// <summary>
        ///   The colony compounds. Use the <see cref="MicrobeColonyHelpers.GetCompounds"/> for accessing this as it
        ///   automatically sets this up if missing.
        /// </summary>
        [JsonIgnore]
        public ColonyCompoundBag? ColonyCompounds;

        public float ColonyRotationMultiplier;

        /// <summary>
        ///   The overall state of the colony, this variable is required to allow colonies where only some cells can
        ///   engulf to properly enter engulf mode etc. and ensure newly added cells pick up the right mode.
        /// </summary>
        public MicrobeState ColonyState;

        // Note that the following statistics should be accessed through the helpers to ensure that they have been
        // calculated. This is implemented like this to simplify spawning to not require full entities to exist at that
        // point. Instead only when the properties are used they are calculated when the colony member entities are
        // certainly created.

        public int HexCount;
        public bool CanEngulf;

        /// <summary>
        ///   Internal variable don't touch.
        /// </summary>
        public bool DerivedStatisticsCalculated;

        public bool EntityWeightApplied;

        /// <summary>
        ///   Internal variable, don't touch
        /// </summary>
        [JsonIgnore]
        public bool ColonyRotationMultiplierCalculated;

        /// <summary>
        ///   Creates a new colony with a leader and cells attached to it. Assumes a flat hierarchy where all members
        ///   are directly attached to the leader
        /// </summary>
        public MicrobeColony(in Entity leader, MicrobeState initialState, params Entity[] allMembers)
        {
            if (allMembers.Length < 2)
                throw new ArgumentException("Microbe colony requires at least one lead cell and one member");

#if DEBUG
            if (allMembers[0] != leader || allMembers[1] == leader)
                throw new ArgumentException("Colony leader needs to be first in member array");
#endif

            Leader = leader;
            ColonyMembers = allMembers;

            // Grab initial state from leader to preserve that (only really important for multicellular)
            ColonyState = initialState;

            ColonyStructure = new Dictionary<Entity, Entity>();

            foreach (var member in allMembers)
            {
                if (member == leader)
                    continue;

                ColonyStructure[member] = leader;
            }

            ColonyRotationMultiplier = 1;
            ColonyRotationMultiplierCalculated = false;
            ColonyCompounds = null;

            HexCount = 0;
            CanEngulf = false;
            DerivedStatisticsCalculated = false;
            EntityWeightApplied = false;
        }
    }

    public static class MicrobeColonyHelpers
    {
        // TODO: implement this (will need to swap all users of the member list to also read a new count variable
        // from the colony class)
        // public static readonly ArrayPool<Entity> MicrobeColonyMemberListPool = ArrayPool<Entity>.Create(100, 50);

        private static readonly List<Entity> DependentMembersToRemove = new();

        public static ColonyCompoundBag GetCompounds(this ref MicrobeColony colony)
        {
            if (colony.ColonyCompounds != null)
                return colony.ColonyCompounds;

            return colony.ColonyCompounds = new ColonyCompoundBag(colony.ColonyMembers);
        }

        /// <summary>
        ///   Applies a colony-wide state (for example makes all cells that can be in engulf mode in the colony be in
        ///   engulf mode)
        /// </summary>
        public static void SetColonyState(this ref MicrobeColony colony, MicrobeState state)
        {
            if (state == colony.ColonyState)
                return;

            colony.ColonyState = state;

            foreach (var cell in colony.ColonyMembers)
            {
                if (cell.IsAlive)
                {
                    // Setting this directly relies on all systems unsetting the state on cells that can't actually
                    // perform the state
                    cell.Get<MicrobeControl>().State = state;
                }
            }
        }

        /// <summary>
        ///   Whether one or more member of this colony is allowed to enter engulf mode. This is recalculated if
        ///   the value is not currently known.
        /// </summary>
        /// <returns>True if any can engulf</returns>
        public static bool CanEngulf(this ref MicrobeColony colony)
        {
            if (!colony.DerivedStatisticsCalculated)
                colony.UpdateColonyEntityCachedStatistics();

            return colony.CanEngulf;
        }

        /// <summary>
        ///   Hex count in the entire colony. Recalculates the value if it isn't currently known.
        /// </summary>
        /// <returns>Total number of organelle hexes in the colony</returns>
        public static int HexCount(this ref MicrobeColony colony)
        {
            if (!colony.DerivedStatisticsCalculated)
                colony.UpdateColonyEntityCachedStatistics();

            return colony.HexCount;
        }

        /// <summary>
        ///   The accumulation of all the colony member's <see cref="Engulfer.UsedIngestionCapacity"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This unfortunately is not cached as <see cref="Engulfer.UsedIngestionCapacity"/> can change
        ///     every frame. And this is relatively expensive to calculate as this needs to read a lot of entities.
        ///   </para>
        /// </remarks>
        public static float CalculateUsedIngestionCapacity(this ref MicrobeColony colony)
        {
            float usedCapacity = 0;

            foreach (var colonyMember in colony.ColonyMembers)
            {
                ref var engulfer = ref colonyMember.Get<Engulfer>();
                usedCapacity += engulfer.UsedIngestionCapacity;
            }

            return usedCapacity;
        }

        /// <summary>
        ///   Perform an action for all members of this cell's colony other than this cell if this is the colony leader.
        /// </summary>
        public static void PerformForOtherColonyMembersThanLeader(this ref MicrobeColony colony, Action<Entity> action,
            Entity skipEntity)
        {
            foreach (var cell in colony.ColonyMembers)
            {
                if (cell == skipEntity)
                    continue;

                action(cell);
            }
        }

        public static bool GetMicrobeFromSubShape(this ref MicrobeColony colony,
            ref MicrobePhysicsExtraData physicsExtraData, uint subShape, out Entity microbe)
        {
            if (physicsExtraData.MicrobeIndexFromSubShape(subShape, out int microbeIndex))
            {
#if DEBUG
                if (microbeIndex == -1)
                    throw new InvalidOperationException("Bad calculated microbe index");
#endif

                // In case the physics data is not yet up to date compared to the colony members, skip
                if (microbeIndex > colony.ColonyMembers.Length)
                {
                    microbe = default;
                    return false;
                }

                microbe = colony.ColonyMembers[microbeIndex];
                return true;
            }

            microbe = default;
            return false;
        }

        /// <summary>
        ///   Adds a member to a colony. This takes in a recorder to ensure thread safety during a world update.
        /// </summary>
        /// <returns>
        ///   True when added. False if some data like membrane wasn't ready yet (this will print an error)
        /// </returns>
        public static bool AddToColony(this ref MicrobeColony colony, in Entity colonyEntity, int parentIndex,
            Entity newMember, EntityCommandRecorder recorder)
        {
            if (newMember.Has<MicrobeColonyMember>())
                throw new ArgumentException("Microbe or master null or microbe already is in a colony");

#if DEBUG
            if (colony.ColonyMembers.Contains(newMember))
            {
                throw new InvalidOperationException("Trying to add same newMember twice to colony");
            }
#endif

            ref var cellProperties = ref colonyEntity.Get<CellProperties>();

            ref var newMemberPosition = ref newMember.Get<WorldPosition>();
            ref var newMemberProperties = ref newMember.Get<CellProperties>();

            var parentMicrobe = colony.ColonyMembers[parentIndex];

            // Calculate the attach position
            Vector3 offsetToColonyLeader;
            Quat rotationToLeader;

            try
            {
                (offsetToColonyLeader, rotationToLeader) = GetNewRelativeTransform(
                    ref parentMicrobe.Get<WorldPosition>(),
                    ref parentMicrobe.Get<CellProperties>(), ref newMemberPosition, ref newMemberProperties);

                if (parentIndex != 0)
                {
                    // Not attaching directly to the colony leader, need to combine the offsets
                    ref var parentsAttachOffset = ref parentMicrobe.Get<AttachedToEntity>();

                    offsetToColonyLeader += parentsAttachOffset.RelativePosition;

                    // TODO: check that the multiply order is right here
                    rotationToLeader = parentsAttachOffset.RelativeRotation * rotationToLeader;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Microbe colony related data not initialized enough to add colony member: ", e);
                return false;
            }

            // TODO: switch to using a pool here. Can't easily switch right now as the array length is used in various
            // places currently so having the length exceed the actual member count will be problematic
            var newMembers = new Entity[colony.ColonyMembers.Length + 1];

            for (int i = 0; i < colony.ColonyMembers.Length; ++i)
            {
                newMembers[i] = colony.ColonyMembers[i];
            }

            newMembers[colony.ColonyMembers.Length] = newMember;
            colony.ColonyMembers = newMembers;

            colony.MarkMembersChanged();

            // Need to recreate the physics body for this colony
            cellProperties.ShapeCreated = false;

            colony.ColonyStructure[newMember] = parentMicrobe;

            var memberRecord = recorder.Record(newMember);
            memberRecord.Set(new MicrobeColonyMember(colonyEntity));
            memberRecord.Set(new AttachedToEntity(colonyEntity, offsetToColonyLeader, rotationToLeader));

            OnColonyMemberAdded(newMember);
            return true;
        }

        /// <summary>
        ///   Removes a member from this colony. If this is called directly check the usage of
        ///   <see cref="AttachedToEntityHelpers.EntityAttachRelationshipModifyLock"/>
        /// </summary>
        /// <returns>True when the colony still exists. False if the entire colony was disbanded</returns>
        public static bool RemoveFromColony(this ref MicrobeColony colony, in Entity colonyEntity, Entity removedMember,
            EntityCommandRecorder recorder)
        {
            if (colonyEntity.Has<EarlyMulticellularSpeciesMember>())
            {
                // Lost a member of the multicellular organism
                throw new NotImplementedException();

                // OnMulticellularColonyCellLost(microbe);
            }

            if (!removedMember.Has<MicrobeColonyMember>())
                throw new ArgumentException("Microbe not a member of a colony");

            if (!colony.ColonyMembers.Contains(removedMember))
                throw new ArgumentException("Cannot remove a colony member who isn't actually a member");

            ref var control = ref colonyEntity.Get<MicrobeControl>();

            // Exit cell unbind mode if currently in it (as the user has selected something to unbind)
            if (control.State == MicrobeState.Unbinding)
                control.State = MicrobeState.Normal;

            // Need to recreate the physics body
            ref var cellProperties = ref colonyEntity.Get<CellProperties>();
            cellProperties.ShapeCreated = false;

            if (colony.ColonyMembers.Length <= 2)
            {
                // The whole colony is disbanding
                recorder.Record(colonyEntity).Remove<MicrobeColony>();

                // Call the remove callback on the members
                for (int i = 0; i < colony.ColonyMembers.Length; ++i)
                {
                    if (colony.ColonyMembers[i] != colonyEntity)
                    {
                        // Handle the normal cleanup here for the non-leader cells (we already queued delete of the
                        // entire colony component above)
                        QueueRemoveFormerColonyMemberComponents(removedMember, recorder);
                    }

                    OnColonyMemberRemoved(colony.ColonyMembers[i]);
                }

                return false;
            }

            // TODO: pooling (see the TODO in the add method)
            // TODO: when recursively removing members somehow make sure that we don't need to keep creating more and
            // more of these lists...
            var newMembers = new Entity[colony.ColonyMembers.Length - 1];

            int writeIndex = 0;

            // Copy all members except the removed one
            for (int i = 0; i < colony.ColonyMembers.Length; ++i)
            {
                var member = colony.ColonyMembers[i];

                if (member == removedMember)
                    continue;

                newMembers[writeIndex++] = member;
            }

            if (writeIndex != newMembers.Length)
                throw new Exception("Logic error in new member array copy");

            colony.ColonyMembers = newMembers;

            OnColonyMemberRemoved(removedMember);

            // Remove colony members that depend on the removed member
            foreach (var entry in colony.ColonyStructure)
            {
                if (entry.Value == removedMember)
                    DependentMembersToRemove.Add(entry.Key);
            }

            while (DependentMembersToRemove.Count > 0)
            {
                var next = DependentMembersToRemove[DependentMembersToRemove.Count - 1];

                // This is this way around to support recursive calls also adding things here
                DependentMembersToRemove.RemoveAt(DependentMembersToRemove.Count - 1);

                if (!colony.RemoveFromColony(colonyEntity, next, recorder))
                {
                    // Colony is entirely disbanded, doesn't make sense to continue removing things
                    DependentMembersToRemove.Clear();
                    return false;
                }
            }

            // Remove structure data regarding the removed member
            colony.ColonyStructure.Remove(removedMember);

            colony.MarkMembersChanged();

            QueueRemoveFormerColonyMemberComponents(removedMember, recorder);

            return true;
        }

        /// <summary>
        ///   Removes this cell and child cells from the colony.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     If this is the colony leader, this disbands the whole colony
        ///   </para>
        /// </remarks>
        /// <returns>True if unbind happened</returns>
        public static bool UnbindAll(in Entity entity, EntityCommandRecorder entityCommandRecorder)
        {
            ref var control = ref entity.Get<MicrobeControl>();

            if (control.State is MicrobeState.Unbinding or MicrobeState.Binding)
                control.State = MicrobeState.Normal;

            ref var organelles = ref entity.Get<OrganelleContainer>();

            if (!organelles.CanUnbind(ref entity.Get<SpeciesMember>(), entity))
                return false;

            lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
            {
                if (entity.Has<MicrobeColony>())
                {
                    // TODO: once the colony leader can leave without the entire colony disbanding this perhaps should
                    // keep the disband entire colony functionality
                    // Colony!.RemoveFromColony(this);

                    ref var colony = ref entity.Get<MicrobeColony>();

                    try
                    {
                        colony.RemoveFromColony(entity, entity, entityCommandRecorder);
                    }
                    catch (Exception e)
                    {
                        GD.PrintErr("Disbanding a colony for a leader failed: ", e);
                    }
                }
                else if (entity.Has<MicrobeColonyMember>())
                {
                    ref var member = ref entity.Get<MicrobeColonyMember>();

                    if (!member.ColonyLeader.Has<MicrobeColony>())
                    {
                        GD.PrintErr("Microbe colony lead newMember is invalid for unbind");
                        return false;
                    }

                    ref var colony = ref member.ColonyLeader.Get<MicrobeColony>();

                    try
                    {
                        colony.RemoveFromColony(member.ColonyLeader, entity, entityCommandRecorder);
                    }
                    catch (Exception e)
                    {
                        GD.PrintErr("Disbanding a colony from a member failed: ", e);
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///   Variant of unbind allowed to be called *only* outside the game update loop
        /// </summary>
        public static bool UnbindAllOutsideGameUpdate(in Entity entity, IWorldSimulation entityWorld)
        {
            if (entityWorld.Processing)
            {
                throw new InvalidOperationException(
                    "Cannot unbind all with this method while running a world simulation");
            }

            var recorder = entityWorld.StartRecordingEntityCommands();
            var result = UnbindAll(entity, recorder);

            // TODO: should this skip applying the recorder if the operation failed

            recorder.Execute();
            entityWorld.FinishRecordingEntityCommands(recorder);
            return result;
        }

        /// <summary>
        ///   Called for each newMember that is removed from a cell colony
        /// </summary>
        public static void OnColonyMemberRemoved(in Entity removedEntity)
        {
            if (removedEntity.Has<MicrobeEventCallbacks>())
            {
                ref var callbacks = ref removedEntity.Get<MicrobeEventCallbacks>();

                callbacks.OnUnbound?.Invoke(removedEntity);
            }

            if (removedEntity.Has<MicrobeAI>())
            {
                ref var ai = ref removedEntity.Get<MicrobeAI>();
                ai.ResetAI();
            }

            ref var control = ref removedEntity.Get<MicrobeControl>();

            // Reset movement to not immediately move after unbind
            control.MovementDirection = Vector3.Zero;

            // TODO: should we calculate a look at point here that doesn't cause immediate rotation?
        }

        /// <summary>
        ///   Called for each newMember that is added to a cell colony
        /// </summary>
        public static void OnColonyMemberAdded(in Entity addedEntity)
        {
            ref var control = ref addedEntity.Get<MicrobeControl>();

            // Multicellular creature can stay in engulf mode when growing things
            if (!addedEntity.Has<EarlyMulticellularSpeciesMember>() || control.State != MicrobeState.Engulf)
            {
                control.State = MicrobeState.Normal;
            }

            throw new NotImplementedException();

            // UnreadyToReproduce();
        }

        /// <summary>
        ///   Calculates an updated newMember weight for a microbe colony to be passed to the <see cref="Spawned"/>
        ///   component as a new value
        /// </summary>
        /// <returns>Recalculated newMember weight of the colony</returns>
        public static bool CalculateEntityWeight(this ref MicrobeColony colony, in Entity entity, out float weight)
        {
            // As a good enough approximation assume each cell is about as complex as the first cell
            var organelles = entity.Get<OrganelleContainer>().Organelles;

            if (organelles == null)
            {
                weight = 0;
                return false;
            }

            var singleCellWeight = OrganelleContainerHelpers.CalculateCellEntityWeight(organelles.Count);

            weight = singleCellWeight + singleCellWeight * Constants.MICROBE_COLONY_MEMBER_ENTITY_WEIGHT_MULTIPLIER *
                colony.ColonyMembers.Length;
            return true;
        }

        public static void CalculateRotationMultiplier(this ref MicrobeColony colony)
        {
            throw new NotImplementedException();

            // TODO: reimplement the colony rotation multiplier. The code for this is probably commented out currently
            // in the movement system, but calculating it should be moved here and uncommented + fixed

            // colony.ColonyRotationMultiplierCalculated = true;
        }

        /// <summary>
        ///   This method calculates the relative rotation and translation this microbe should have to its
        ///   microbe colony parent.
        ///   <a href="https://randomthrivefiles.b-cdn.net/documentation/fixed_colony_rotation_explanation_image.png">
        ///     Visual explanation
        ///   </a>
        /// </summary>
        /// <returns>Returns relative translation and rotation</returns>
        public static (Vector3 Translation, Quat Rotation) GetNewRelativeTransform(
            ref WorldPosition colonyParentPosition, ref CellProperties colonyParentProperties,
            ref WorldPosition cellPosition, ref CellProperties cellProperties)
        {
            if (colonyParentProperties.CreatedMembrane == null)
                throw new InvalidOperationException("Colony parent cell has no membrane set");

            if (cellProperties.CreatedMembrane == null)
                throw new InvalidOperationException("Cell to add to colony has no membrane set");

            // Gets the global rotation of the parent
            // TODO: verify that the quaternion math is correct here
            var globalParentRotation = colonyParentPosition.Rotation;

            // A vector from the parent to me
            var vectorFromParent = cellPosition.Position - colonyParentPosition.Position;

            // A vector from me to the parent
            var vectorToParent = -vectorFromParent;

            // TODO: using quaternions here instead of assuming that rotating about the up/down axis is right
            // would be nice
            // This vector represents the vectorToParent as if I had no rotation.
            // This works by rotating vectorToParent by the negative value (therefore Down) of my current rotation
            // This is important, because GetVectorTowardsNearestPointOfMembrane only works with non-rotated microbes
            var vectorToParentWithoutRotation =
                vectorToParent.Rotated(Vector3.Down, cellPosition.Rotation.GetEuler().y);

            // This vector represents the vectorFromParent as if the parent had no rotation.
            var vectorFromParentWithoutRotation = vectorFromParent.Rotated(Vector3.Down, globalParentRotation.y);

            // Calculates the vector from the center of the parent's membrane towards me with canceled out rotation.
            // This gets added to the vector calculated one call before.
            var correctedVectorFromParent = colonyParentProperties.CreatedMembrane
                .GetVectorTowardsNearestPointOfMembrane(vectorFromParentWithoutRotation.x,
                    vectorFromParentWithoutRotation.z).Rotated(Vector3.Up, globalParentRotation.y);

            // Calculates the vector from my center to my membrane towards the parent.
            // This vector gets rotated back to cancel out the rotation applied two calls above.
            // -= to negate the vector, so that the two membrane vectors amplify
            correctedVectorFromParent -= cellProperties.CreatedMembrane
                .GetVectorTowardsNearestPointOfMembrane(vectorToParentWithoutRotation.x,
                    vectorToParentWithoutRotation.z)
                .Rotated(Vector3.Up, cellPosition.Rotation.GetEuler().y);

            // Rotated because the rotational scope is different.
            var newTranslation = correctedVectorFromParent.Rotated(Vector3.Down, globalParentRotation.y);

            // TODO: this used to just negate the euler angles here, check that multiplying by inverse rotation is
            // correct
            return (newTranslation, cellPosition.Rotation * globalParentRotation.Inverse());
        }

        private static void MarkMembersChanged(this ref MicrobeColony colony)
        {
            colony.DerivedStatisticsCalculated = false;
            colony.EntityWeightApplied = false;
            colony.ColonyRotationMultiplierCalculated = false;

            // TODO: maybe in some situations creating the compound bag could be entirely safely skipped here
            colony.GetCompounds().UpdateColonyMembers(colony.ColonyMembers);
        }

        /// <summary>
        ///   Removes the components from the detached entity that no longer should be on it
        /// </summary>
        private static void QueueRemoveFormerColonyMemberComponents(in Entity removedMember,
            EntityCommandRecorder recorder)
        {
            var memberRecord = recorder.Record(removedMember);
            memberRecord.Remove<MicrobeColonyMember>();
            memberRecord.Remove<AttachedToEntity>();
        }

        private static void UpdateColonyEntityCachedStatistics(this ref MicrobeColony colony)
        {
            bool canEngulf = false;
            int hexCount = 0;
            bool success = true;

            foreach (var colonyMember in colony.ColonyMembers)
            {
                var organelles = colonyMember.Get<OrganelleContainer>().Organelles;

                if (organelles != null)
                    hexCount += organelles.HexCount;

                if (colonyMember.Get<CellProperties>().MembraneType.CanEngulf)
                    canEngulf = true;
            }

            colony.CanEngulf = canEngulf;

            if (success)
            {
                colony.HexCount = hexCount;
                colony.DerivedStatisticsCalculated = true;
            }
        }
    }
}
