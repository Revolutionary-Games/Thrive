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
        [JsonConverter(typeof(DictionaryWithJSONKeysConverter<Entity, Entity>))]
        public Dictionary<Entity, Entity> ColonyStructure;

        /// <summary>
        ///   The colony compounds. Use the <see cref="MicrobeColonyHelpers.GetCompounds"/> for accessing this as it
        ///   automatically sets this up if missing.
        /// </summary>
        [JsonIgnore]
        public ColonyCompoundBag? ColonyCompounds;

        public float ColonyRotationSpeed;

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
        ///   Creates a new colony with a leader and cells attached to it. Assumes a flat hierarchy where all members
        ///   are directly attached to the leader
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     It is mandatory to call <see cref="MicrobeColonyHelpers.AddInitialColonyMember"/> on each of the
        ///     members *after* the leader to setup the state for the entities correctly.
        ///   </para>
        /// </remarks>
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

            ColonyRotationSpeed = 1;
            ColonyCompounds = null;

            HexCount = 0;
            CanEngulf = false;
            DerivedStatisticsCalculated = false;
            EntityWeightApplied = false;
        }

        /// <summary>
        ///   Creates a delay-filled microbe colony. Mainly used by <see cref="Systems.DelayedColonyOperationSystem"/>
        /// </summary>
        public MicrobeColony(bool delayAdded, in Entity leader, MicrobeState initialState)
        {
            // This parameter exists to differentiate the call signature from the fully immediately created colony
            // constructor
            if (!delayAdded)
                throw new ArgumentException("This constructor is for delay created colonies");

            _ = delayAdded;

            Leader = leader;

            // TODO: pooling
            // As we need to spawn the entities to add to the colony the next frame, we can only add the lead
            // cell here when constructing the colony
            ColonyMembers = new[] { leader };

            ColonyState = initialState;

            ColonyStructure = new Dictionary<Entity, Entity>();

            ColonyRotationSpeed = 1;
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
        ///   The accumulation of all the colony member's <see cref="Engulfer.UsedEngulfingCapacity"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This unfortunately is not cached as <see cref="Engulfer.UsedEngulfingCapacity"/> can change
        ///     every frame. And this is relatively expensive to calculate as this needs to read a lot of entities.
        ///   </para>
        /// </remarks>
        public static float CalculateUsedEngulfingCapacity(this ref MicrobeColony colony)
        {
#if DEBUG
            colony.DebugCheckColonyHasNoDeadEntities();
#endif

            float usedCapacity = 0;

            foreach (var colonyMember in colony.ColonyMembers)
            {
                ref var engulfer = ref colonyMember.Get<Engulfer>();
                usedCapacity += engulfer.UsedEngulfingCapacity;
            }

            return usedCapacity;
        }

        /// <summary>
        ///   Calculates the total ingest capacity of all members of a colony
        /// </summary>
        /// <returns>The ingestion storage size</returns>
        public static float CalculateTotalEngulfStorageSize(this ref MicrobeColony colony)
        {
            float capacity = 0;

            foreach (var colonyMember in colony.ColonyMembers)
            {
                ref var engulfer = ref colonyMember.Get<Engulfer>();
                capacity += engulfer.EngulfStorageSize;
            }

            return capacity;
        }

        /// <summary>
        ///   Calculates the total counts of special organelles in a colony
        /// </summary>
        public static void CalculateColonySpecialOrganelles(this ref MicrobeColony colony, out int agentVacuoles,
            out int slimeJets)
        {
            agentVacuoles = 0;
            slimeJets = 0;

            foreach (var colonyMember in colony.ColonyMembers)
            {
                ref var organelles = ref colonyMember.Get<OrganelleContainer>();

                agentVacuoles += organelles.AgentVacuoleCount;
                slimeJets += organelles.SlimeJets?.Count ?? 0;
            }
        }

        public static HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>?
            CollectUniqueCompoundDetections(this ref MicrobeColony colony)
        {
            HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>? result = null;

            foreach (var colonyMember in colony.ColonyMembers)
            {
                ref var organelles = ref colonyMember.Get<OrganelleContainer>();

                if (organelles.ActiveCompoundDetections != null)
                {
                    result ??= new HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>();

                    foreach (var entry in organelles.ActiveCompoundDetections)
                    {
                        result.Add(entry);
                    }
                }
            }

            return result;
        }

        public static HashSet<(Species TargetSpecies, float Range, Color Colour)>?
            CollectUniqueSpeciesDetections(this ref MicrobeColony colony)
        {
            HashSet<(Species TargetSpecies, float Range, Color Colour)>? result = null;

            foreach (var colonyMember in colony.ColonyMembers)
            {
                ref var organelles = ref colonyMember.Get<OrganelleContainer>();

                if (organelles.ActiveSpeciesDetections != null)
                {
                    result ??= new HashSet<(Species TargetSpecies, float Range, Color Colour)>();

                    foreach (var entry in organelles.ActiveSpeciesDetections)
                    {
                        result.Add(entry);
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///   Perform an action for all members of this cell's colony other than the leader
        /// </summary>
        public static void PerformForOtherColonyMembersThanLeader(this ref MicrobeColony colony, Action<Entity> action)
        {
            var skipEntity = colony.Leader;

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

                var members = colony.ColonyMembers;

                // In case the physics data is not yet up to date compared to the colony members, skip
                if (microbeIndex >= members.Length)
                {
                    microbe = default;
                    return false;
                }

                microbe = members[microbeIndex];
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
                throw new ArgumentException("Microbe already is in a colony");

#if DEBUG
            if (colony.ColonyMembers.Contains(newMember))
            {
                throw new InvalidOperationException("Trying to add same newMember twice to colony");
            }
#endif

            ref var newMemberPosition = ref newMember.Get<WorldPosition>();
            ref var newMemberProperties = ref newMember.Get<CellProperties>();

            var parentMicrobe = colony.ColonyMembers[parentIndex];

            if (!CalculateColonyMemberAttachPosition(colonyEntity, parentMicrobe, newMemberPosition,
                    newMemberProperties, out var offsetToColonyLeader, out var rotationToLeader))
            {
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

            SetupColonyMemberData(ref colony, colonyEntity, parentIndex, newMember, offsetToColonyLeader,
                rotationToLeader, recorder);
            return true;
        }

        /// <summary>
        ///   Finishes adding a queued colony member. This is used by
        ///   <see cref="Systems.DelayedColonyOperationSystem"/>. This automatically adjusts the parent index to +1 if
        ///   the inserted member index is before the parent index (to make sure the parent index points to the
        ///   intended member of the colony and won't point to the newly added member which would cause in invalid
        ///   parent loop)
        /// </summary>
        public static void FinishQueuedMemberAdd(this ref MicrobeColony colony, in Entity colonyEntity, int parentIndex,
            Entity newMember, int intendedNewMemberIndex, EntityCommandRecorder recorder)
        {
            if (!newMember.Has<AttachedToEntity>())
            {
                throw new InvalidOperationException(
                    "This add variant may only be called for partially already added entity");
            }

            if (newMember.Has<MicrobeColonyMember>())
                throw new ArgumentException("Microbe already is in a colony");

            if (intendedNewMemberIndex == 0)
                throw new ArgumentException("Intended new member index may not be 0");

#if DEBUG
            if (colony.ColonyMembers.Contains(newMember))
            {
                throw new InvalidOperationException("Trying to add same newMember twice to colony");
            }
#endif

            // TODO: switch to using a pool here. Can't easily switch right now as the array length is used in various
            // places currently so having the length exceed the actual member count will be problematic
            // When converting all uses of the member list need to be checked as this is set in quite many places
            var newMembers = new Entity[colony.ColonyMembers.Length + 1];

            // Specifying an index after all items is the same as specifying last. This makes sure that slightly out
            // of sync data in delayed apply colony membership doesn't cause issues.
            if (intendedNewMemberIndex >= newMembers.Length)
                intendedNewMemberIndex = newMembers.Length - 1;

            // Place at the intended index
            for (int i = 0; i < colony.ColonyMembers.Length && i < intendedNewMemberIndex; ++i)
            {
                newMembers[i] = colony.ColonyMembers[i];
            }

            newMembers[intendedNewMemberIndex] = newMember;

            for (int i = intendedNewMemberIndex + 1; i < newMembers.Length; ++i)
            {
                // As we inserted one new item, the items don't anymore map 1-to-1 between the arrays
                newMembers[i] = colony.ColonyMembers[i - 1];
            }

            colony.ColonyMembers = newMembers;

            colony.MarkMembersChanged();

            // If the new member is inserted before the parent index, increment the parent index
            if (parentIndex >= intendedNewMemberIndex)
                ++parentIndex;

            if (parentIndex == intendedNewMemberIndex)
            {
                throw new ArgumentException(
                    "New member intended index and its parent's index ended up being the same (self would be parent)");
            }

            SetupDelayAddedColonyMemberData(ref colony, colonyEntity, parentIndex, newMember, recorder);
        }

        /// <summary>
        ///   Sets up an initial colony member that is added in the <see cref="MicrobeColony"/> constructor. Variant
        ///   of <see cref="AddToColony"/> that works a bit specially
        /// </summary>
        public static bool AddInitialColonyMember(this ref MicrobeColony colony, in Entity colonyEntity,
            int parentIndex, in Entity addedColonyMember, EntityCommandRecorder recorder)
        {
            if (addedColonyMember.Has<MicrobeColonyMember>())
                throw new ArgumentException("Microbe already is in a colony");

            if (!colony.ColonyMembers.Contains(addedColonyMember))
                throw new InvalidOperationException("This can only be called on a microbe already in the colony");

            ref var newMemberPosition = ref addedColonyMember.Get<WorldPosition>();
            ref var newMemberProperties = ref addedColonyMember.Get<CellProperties>();

            var parentMicrobe = colony.ColonyMembers[parentIndex];

            // Calculate the attach position
            // Note that if the multicellular growth is moved away from precomputed locations, this won't likely be
            // usable to calculate the positions instead
            if (!CalculateColonyMemberAttachPosition(colonyEntity, parentMicrobe, newMemberPosition,
                    newMemberProperties, out var offsetToColonyLeader, out var rotationToLeader))
            {
                return false;
            }

            SetupColonyMemberData(ref colony, colonyEntity, parentIndex, addedColonyMember, offsetToColonyLeader,
                rotationToLeader, recorder);
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
            if (colonyEntity.Has<MulticellularGrowth>())
            {
                // Lost a member of the multicellular organism
                ref var growth = ref colonyEntity.Get<MulticellularGrowth>();
                growth.OnMulticellularColonyCellLost(ref colonyEntity.Get<OrganelleContainer>(),
                    colonyEntity.Get<CompoundStorage>().Compounds, colonyEntity, removedMember);
            }

            if (!removedMember.IsAlive)
                throw new Exception("Cannot process a dead entity remove from a colony");

            bool removedMemberIsLeader = false;

            // Colony members or leader can be removed by this method
            if (removedMember.Has<MicrobeColony>())
            {
                removedMemberIsLeader = true;
            }
            else if (!removedMember.Has<MicrobeColonyMember>())
            {
                throw new ArgumentException("Microbe not a member of a colony (missing component)");
            }

            if (!colony.ColonyMembers.Contains(removedMember))
                throw new ArgumentException("Cannot remove a colony member who isn't actually a member");

            ref var control = ref colonyEntity.Get<MicrobeControl>();

            // Exit cell unbind mode if currently in it (as the user has selected something to unbind)
            if (control.State == MicrobeState.Unbinding || colony.ColonyState == MicrobeState.Unbinding)
            {
                control.SetStateColonyAware(colonyEntity, MicrobeState.Normal);
            }

            // TODO: should unbound cell have its state reset for example if it was in binding or engulf mode?

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
                    bool leader = true;

                    var currentMember = colony.ColonyMembers[i];
                    if (currentMember != colonyEntity)
                    {
                        // Handle the normal cleanup here for the non-leader cells (we already queued delete of the
                        // entire colony component above)
                        QueueRemoveFormerColonyMemberComponents(currentMember, recorder);
                        leader = false;
                    }

                    OnColonyMemberRemoved(currentMember, leader);
                }

                return false;
            }

            RemoveColonyMemberFromMemberList(ref colony, removedMember);

            if (!removedMemberIsLeader)
                QueueRemoveFormerColonyMemberComponents(removedMember, recorder);

            OnColonyMemberRemoved(removedMember, removedMemberIsLeader);

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

                if (!next.IsAlive)
                {
                    // This entity is already dead, this should hopefully never trigger. If this does then this would
                    // give some more info on a colony despawn crash problem.
                    GD.PrintErr("Dependent colony member to remove is already dead, doing only " +
                        "light fallback cleanup");

                    colony.ColonyStructure.Remove(next);

                    if (colony.ColonyMembers.Contains(next))
                        RemoveColonyMemberFromMemberList(ref colony, next);

                    // Normal remove can't be performed so the above emergency cleanup needs to be enough
                    continue;
                }

                // This might stackoverflow if we have absolute hugely nested cell colonies but there would probably
                // need to be colonies with thousands of cells, which would already choke the game so that isn't much
                // of a concern
                if (!colony.RemoveFromColony(colonyEntity, next, recorder))
                {
                    // Colony is entirely disbanded, doesn't make sense to continue removing things
                    DependentMembersToRemove.Clear();
                    return false;
                }
            }

            // Check against a logic error. All colony members ultimately depend on the leader so the above early
            // exit must trigger uf the colony leader is removed
            if (removedMember == colony.Leader)
                throw new Exception("Colony should have been fully disbanded when leader was removed");

            // Remove structure data regarding the removed member
            colony.ColonyStructure.Remove(removedMember);

            colony.MarkMembersChanged();

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
            {
                control.SetStateColonyAware(entity, MicrobeState.Normal);
            }

            ref var organelles = ref entity.Get<OrganelleContainer>();

            if (!organelles.CanUnbind(ref entity.Get<SpeciesMember>(), entity))
                return false;

            // TODO: once the colony leader can leave without the entire colony disbanding this perhaps should
            // keep the disband entire colony functionality
            // Colony!.RemoveFromColony(this); (when entity is a colony leader)
            return RemoveFromColony(entity, entityCommandRecorder);
        }

        /// <summary>
        ///   Removes the given entity from the microbe colony it is in (if any)
        /// </summary>
        /// <returns>True on success</returns>
        public static bool RemoveFromColony(in Entity entity, EntityCommandRecorder entityCommandRecorder)
        {
            lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
            {
                if (entity.Has<MicrobeColony>())
                {
                    ref var colony = ref entity.Get<MicrobeColony>();

                    try
                    {
                        colony.RemoveFromColony(entity, entity, entityCommandRecorder);
                    }
                    catch (Exception e)
                    {
                        GD.PrintErr("Disbanding a colony for a leader failed: ", e);
                        return false;
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
                        return false;
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

            // Extra debugs checks to ensure the unbind function doesn't have serious bugs with incorrect component
            // handling
#if DEBUG
            Entity[]? members = null;

            if (entity.Has<MicrobeColony>())
            {
                ref var colony = ref entity.Get<MicrobeColony>();
                members = colony.ColonyMembers;
            }
#endif

            var recorder = entityWorld.StartRecordingEntityCommands();
            var result = UnbindAll(entity, recorder);

            // TODO: should this skip applying the recorder if the operation failed

            recorder.Execute();
            entityWorld.FinishRecordingEntityCommands(recorder);

#if DEBUG
            if (entity.Has<MicrobeColony>() || entity.Has<MicrobeColonyMember>())
            {
                throw new Exception("Microbe colony unbind didn't delete components correctly");
            }

            if (members != null)
            {
                foreach (var member in members)
                {
                    if (member.Has<MicrobeColonyMember>() || member.Has<AttachedToEntity>())
                    {
                        throw new Exception("Microbe colony unbind didn't delete components correctly");
                    }
                }
            }
#endif

            return result;
        }

        public static void ReportReproductionStatusOnAddToColony(in Entity entity)
        {
            if (entity.Has<MicrobeEventCallbacks>() && !entity.Has<EarlyMulticellularSpeciesMember>())
            {
                ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

                callbacks.OnReproductionStatus?.Invoke(entity, false);
            }
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

            weight = singleCellWeight +
                CalculateColonyAdditionalEntityWeight(singleCellWeight, colony.ColonyMembers.Length);
            return true;
        }

        public static float CalculateColonyAdditionalEntityWeight(float singleCellWeight, int memberCount)
        {
            return singleCellWeight * Constants.MICROBE_COLONY_MEMBER_ENTITY_WEIGHT_MULTIPLIER * memberCount;
        }

        /// <summary>
        ///   Makes an entity that is being spawned into a fully grown multicellular colony
        /// </summary>
        /// <returns>How much the added colony members add entity weight</returns>
        public static float SpawnAsFullyGrownMulticellularColony(EntityRecord entity, EarlyMulticellularSpecies species,
            float originalWeight)
        {
            int members = species.Cells.Count - 1;

            // Ignore fully spawning early multicellular species that only have one cell in them
            if (members < 1)
                return 0;

            SetupColonyWithMembersDelayed(entity, members);

            return CalculateColonyAdditionalEntityWeight(originalWeight, members);
        }

        public static float SpawnAsPartialMulticellularColony(EntityRecord entity, float originalWeight,
            int membersToAdd)
        {
            if (membersToAdd < 1)
            {
                GD.PrintErr("Spawn as partially grown colony was passed 0 as number of cells to add");
                return 0;
            }

            SetupColonyWithMembersDelayed(entity, membersToAdd);

            return CalculateColonyAdditionalEntityWeight(originalWeight, membersToAdd);
        }

        /// <summary>
        ///   Calculates the total rotation rate of a colony. Physics shape is not currently used.
        /// </summary>
        public static void CalculateRotationSpeed(this ref MicrobeColony colony)
        {
            // TODO: see the comment in MicrobeInternalCalculations.CalculateRotationSpeed about:
            // shape.TestYRotationInertiaFactor() how to make this take the colony shape into account in rotation to
            // be more physically accurate

            float colonyRotation = MicrobeInternalCalculations
                .CalculateRotationSpeed(colony.Leader.Get<OrganelleContainer>().Organelles!.Organelles);

            foreach (var colonyMember in colony.ColonyMembers)
            {
                // Colony leader is set before the loop
                if (colonyMember == colony.Leader)
                    continue;

                ref var memberPosition = ref colonyMember.Get<AttachedToEntity>();

                var distanceSquared = memberPosition.RelativePosition.LengthSquared();

                // Multiply both the propulsion and mass by the distance from center to simulate leverage
                // This relies on the bounding of the cell rotation, as a colony can never be faster than the
                // fastest cell inside it
                var memberRotation = MicrobeInternalCalculations
                        .CalculateRotationSpeed(colonyMember.Get<OrganelleContainer>().Organelles!.Organelles)
                    * (1 + 0.03f * distanceSquared);

                colonyRotation += memberRotation;
            }

            colony.ColonyRotationSpeed = colonyRotation / colony.ColonyMembers.Length;
        }

        /// <summary>
        ///   Looks at all the existing colony members and picks the one that is most sensible for the new cell to
        ///   attach to in the colony.
        /// </summary>
        /// <returns>Parent index</returns>
        /// <remarks>
        ///   <para>
        ///     As the multicellular editor doesn't specify the parent cells of the cells in the body layout, this
        ///     method is used to determine dynamically which cell a given new colony member should be parented to.
        ///   </para>
        /// </remarks>
        public static int CalculateSensibleParentIndexForMulticellular(this ref MicrobeColony colony,
            ref AttachedToEntity calculatedPosition)
        {
            float bestDistance = float.MaxValue;
            int bestParentIndex = 0;

            var members = colony.ColonyMembers;

            for (int i = 0; i < members.Length; ++i)
            {
                float distance;
                if (i == 0)
                {
                    // Colony leader has no attached to component
                    distance = calculatedPosition.RelativePosition.LengthSquared();
                }
                else
                {
                    distance = members[i].Get<AttachedToEntity>().RelativePosition
                        .DistanceSquaredTo(calculatedPosition.RelativePosition);
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestParentIndex = i;
                }
            }

            // Check against the exact starting value indicating nothing was found
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (bestDistance == float.MaxValue)
                GD.PrintErr("Could not find sensible parent index to attach to colony");

            return bestParentIndex;
        }

        /// <summary>
        ///   This might be useful in the future to check body plan parent distances when taking into account the
        ///   growth order
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Looks at all the colony members until the given index (exclusive upper bound) and picks the one that is
        ///     most sensible for the new cell to attach to in the colony.
        ///   </para>
        /// </remarks>
        /// <returns>Parent index</returns>
        public static int CalculateSensibleParentIndexForMulticellularGrowthOrder(this ref MicrobeColony colony,
            int indexToAddTo, ref AttachedToEntity calculatedPosition)
        {
            if (indexToAddTo < 1)
                GD.PrintErr("Index to add to should be at least 1 when looking for colony parent to use");

            float bestDistance = float.MaxValue;
            int bestParentIndex = 0;

            var members = colony.ColonyMembers;

            for (int i = 0; i < members.Length && i < indexToAddTo; ++i)
            {
                float distance;
                if (i == 0)
                {
                    // Colony leader has no attached to component
                    distance = calculatedPosition.RelativePosition.LengthSquared();
                }
                else
                {
                    distance = members[i].Get<AttachedToEntity>().RelativePosition
                        .DistanceSquaredTo(calculatedPosition.RelativePosition);
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestParentIndex = i;
                }
            }

            // Check against the exact starting value indicating nothing was found
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (bestDistance == float.MaxValue)
                GD.PrintErr("Could not find sensible parent index to attach to colony");

            return bestParentIndex;
        }

        public static void DebugCheckColonyHasNoDeadEntities(this ref MicrobeColony colony)
        {
            foreach (var colonyMember in colony.ColonyMembers)
            {
                if (!colonyMember.IsAlive)
                    throw new Exception("Colony has a non-alive member");
            }
        }

        /// <summary>
        ///   Called for each newMember that is added to a cell colony. Not called for the lead cell.
        /// </summary>
        private static void OnColonyMemberAdded(ref MicrobeColony colony, in Entity addedEntity)
        {
            ref var physics = ref addedEntity.Get<Physics>();
            physics.BodyDisabled = true;

            ref var control = ref addedEntity.Get<MicrobeControl>();

            // Move the added cell to the same state as the overall colony
            control.State = colony.ColonyState;

            ResetReadyToReproduceStatus(addedEntity);

            // Clear queued slime jet force
            control.QueuedSlimeSecretionTime = 0;

            ReportReproductionStatusOnAddToColony(addedEntity);
        }

        /// <summary>
        ///   Called for each newMember that is removed from a cell colony. Also called for the colony lead cell when
        ///   colony is disbanding. Note that is in contrast to <see cref="OnColonyMemberAdded"/> which is not called
        ///   on the lead cell.
        /// </summary>
        private static void OnColonyMemberRemoved(in Entity removedEntity, bool wasLeader)
        {
            // Restore physics
            ref var physics = ref removedEntity.Get<Physics>();
            physics.BodyDisabled = false;

            if (removedEntity.Has<MicrobeEventCallbacks>())
            {
                ref var callbacks = ref removedEntity.Get<MicrobeEventCallbacks>();

                callbacks.OnUnbound?.Invoke(removedEntity);
            }

            // Reset reproduction status to cause a re-check afterwards to not lock the player out of the editor
            ResetReadyToReproduceStatus(removedEntity);

            // For the lead cell when disbanding the colony we don't want to reset all stuff
            if (wasLeader)
                return;

            if (removedEntity.Has<MicrobeAI>())
            {
                ref var ai = ref removedEntity.Get<MicrobeAI>();
                ai.ResetAI(removedEntity);
            }

            ref var control = ref removedEntity.Get<MicrobeControl>();

            // Reset movement to not immediately move after unbind
            control.MovementDirection = Vector3.Zero;

            // TODO: should we calculate a look at point here that doesn't cause immediate rotation?
        }

        private static void ResetReadyToReproduceStatus(Entity colonyMember)
        {
            if (colonyMember.Has<OrganelleContainer>())
            {
                ref var organelles = ref colonyMember.Get<OrganelleContainer>();
                organelles.AllOrganellesDivided = false;
            }
        }

        /// <summary>
        ///   Calculate the position of a new microbe to attach to a colony. Requires membrane data to be generated to
        ///   calculate an accurate position.
        /// </summary>
        /// <returns>True on success, false if data not ready for calculation yet</returns>
        private static bool CalculateColonyMemberAttachPosition(Entity colonyEntity, Entity parentMicrobe,
            WorldPosition newMemberPosition, CellProperties newMemberProperties, out Vector3 offsetToColonyLeader,
            out Quaternion rotationToLeader)
        {
            try
            {
                var targetWorldPosition = GetNewAttachPosition(ref parentMicrobe.Get<WorldPosition>(),
                    ref parentMicrobe.Get<CellProperties>(), ref newMemberPosition, ref newMemberProperties);

                ref var leaderPosition = ref colonyEntity.Get<WorldPosition>();
                var inverseParentRotation = leaderPosition.Rotation.Inverse();

                // Convert result to be relative to the colony lead cell (which is the colony's base position)
                offsetToColonyLeader = inverseParentRotation.Xform(targetWorldPosition - leaderPosition.Position);

                // Convert the rotation to keep the global rotation intact after attaching
                rotationToLeader = newMemberPosition.Rotation * inverseParentRotation;
            }
            catch (Exception e)
            {
                GD.PrintErr("Microbe colony related data not initialized enough to add colony member: ", e);
                offsetToColonyLeader = Vector3.Zero;
                rotationToLeader = Quaternion.Identity;
                return false;
            }

            return true;
        }

        /// <summary>
        ///   This method calculates the new world rotation and translation this microbe should have after attaching to
        ///   its microbe colony parent.
        ///   Note this explanation image was made for the old version of this method but may still be useful:
        ///   <a href="https://randomthrivefiles.b-cdn.net/documentation/fixed_colony_rotation_explanation_image.png">
        ///     Visual explanation
        ///   </a>
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This method used to calculate a relative offset, but now calculates a world position, which is less
        ///     efficient, but no one could come up with the right math for the harder way to solve this:
        ///     https://github.com/Revolutionary-Games/Thrive/issues/4666
        ///   </para>
        /// </remarks>
        /// <returns>Returns the world position</returns>
        private static Vector3 GetNewAttachPosition(ref WorldPosition colonyParentPosition,
            ref CellProperties colonyParentProperties,
            ref WorldPosition cellPosition, ref CellProperties cellProperties)
        {
            if (colonyParentProperties.CreatedMembrane == null)
                throw new InvalidOperationException("Colony parent cell has no membrane set");

            if (cellProperties.CreatedMembrane == null)
                throw new InvalidOperationException("Cell to add to colony has no membrane set");

            // A vector from the parent to me
            var vectorFromParent = cellPosition.Position - colonyParentPosition.Position;

            // A vector from me to the parent
            var vectorToParent = -vectorFromParent;

            var parentRotationInverse = colonyParentPosition.Rotation.Inverse();
            var cellRotationInverse = cellPosition.Rotation.Inverse();

            // These are in the local space of the given two cells respectively
            var vectorFromParentInLocalSpace = parentRotationInverse.Xform(vectorFromParent);

            var vectorToParentInLocalSpace = cellRotationInverse.Xform(vectorToParent);

            // Calculate membrane sizes that should be added to the distance between the center's of the cells so that
            // the cells don't overlap. The membrane points are in local coordinates so they are translated back to
            // world coordinates.
            var parentMembranePoint = colonyParentPosition.Rotation.Xform(
                colonyParentProperties.CreatedMembrane.GetVectorTowardsNearestPointOfMembrane(
                    vectorFromParentInLocalSpace.X, vectorFromParentInLocalSpace.Z));

            var cellMembranePoint = cellPosition.Rotation.Xform(
                cellProperties.CreatedMembrane.GetVectorTowardsNearestPointOfMembrane(vectorToParentInLocalSpace.X,
                    vectorToParentInLocalSpace.Z));

            // The cell membrane point is negated here to amplify the distance as it is the opposite sign of the parent
            // membrane point otherwise.
            var finalOffsetVector = parentMembranePoint - cellMembranePoint;

            return colonyParentPosition.Position + finalOffsetVector;
        }

        /// <summary>
        ///   Common code for new colony member setup
        /// </summary>
        private static void SetupColonyMemberData(ref MicrobeColony colony, in Entity colonyEntity, int parentIndex,
            in Entity newMember, Vector3 offsetToColonyLeader, Quaternion rotationToLeader, EntityCommandRecorder recorder)
        {
            OnCommonColonyMemberSetup(ref colony, colonyEntity, parentIndex, newMember, recorder);

            var memberRecord = recorder.Record(newMember);
            memberRecord.Set(new AttachedToEntity(colonyEntity, offsetToColonyLeader, rotationToLeader));

            OnColonyMemberAdded(ref colony, newMember);
        }

        private static void SetupDelayAddedColonyMemberData(ref MicrobeColony colony, in Entity colonyEntity,
            int parentIndex, in Entity newMember, EntityCommandRecorder recorder)
        {
            OnCommonColonyMemberSetup(ref colony, colonyEntity, parentIndex, newMember, recorder);

            // This variant is used for entities that have already the attached to component created

            OnColonyMemberAdded(ref colony, newMember);
        }

        private static void OnCommonColonyMemberSetup(ref MicrobeColony colony, Entity colonyEntity, int parentIndex,
            Entity newMember, EntityCommandRecorder recorder)
        {
            if (parentIndex >= colony.ColonyMembers.Length)
                throw new ArgumentException("Cannot use out of range parent index for new colony member parent");

            ref var cellProperties = ref colonyEntity.Get<CellProperties>();

            // Need to recreate the physics body for this colony
            cellProperties.ShapeCreated = false;

            var parentMicrobe = colony.ColonyMembers[parentIndex];

            if (newMember == parentMicrobe)
                throw new ArgumentException("Entity cannot be its own parent in a colony");

            colony.ColonyStructure[newMember] = parentMicrobe;

            var memberRecord = recorder.Record(newMember);
            memberRecord.Set(new MicrobeColonyMember(colonyEntity));
        }

        /// <summary>
        ///   Removes a member from the colony. It is incorrect to call this with an entity that is not contained in
        ///   the list of members.
        /// </summary>
        /// <exception cref="Exception">If this is called with a member not in the members list</exception>
        private static void RemoveColonyMemberFromMemberList(ref MicrobeColony colony, in Entity removedMember)
        {
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
            {
                throw new Exception("Logic error in new member array copy without member " +
                    "(was it ensured removed member was in the list)");
            }

            colony.ColonyMembers = newMembers;
        }

        private static void MarkMembersChanged(this ref MicrobeColony colony)
        {
            colony.DerivedStatisticsCalculated = false;
            colony.EntityWeightApplied = false;

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

        /// <summary>
        ///   Sets up a colony to be created for an entity after it is spawned. This works in a delayed way as the
        ///   entity is not known when it is being spawned so normal colony creation doesn't work.
        /// </summary>
        private static void SetupColonyWithMembersDelayed(EntityRecord entity, int membersAfterLeader)
        {
            entity.Set(new DelayedMicrobeColony(membersAfterLeader));
        }
    }
}
