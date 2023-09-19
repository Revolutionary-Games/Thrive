namespace Components
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using DefaultEcs;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Microbe colony entity. This component is added to the colony lead cell. This contains the overall info about
    ///   the cell colony or early multicellular creature.
    /// </summary>
    public struct MicrobeColony
    {
        /// <summary>
        ///   All colony members of this colony. The cell at index 0 has to be the <see cref="Leader"/>. Only modify
        ///   this data through the helper methods to ensure everything is consistent.
        /// </summary>
        public Entity[] ColonyMembers;

        /// <summary>
        ///   Lead cell of the colony. This is the entity that exists separately in the world, all others are attached
        ///   to it with <see cref="AttachedToEntity"/> components.
        /// </summary>
        public Entity Leader;

        /// <summary>
        ///   This maps parent cells to their children in the colony hierarchy. All cells are merged into the leader,
        ///   but certain operations like removing cells need to not leave gaps in the colony for that this is used to
        ///   detect which cells are also lost if one cell is lost.
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

                ColonyStructure[leader] = member;
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
        public static readonly ArrayPool<Entity> MicrobeColonyMemberListPool = ArrayPool<Entity>.Create(100, 50);

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
        ///     This unfortunately is not cached as <see cref="Microbe.UsedIngestionCapacity"/> can change
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

        public static void AddToColony(this ref MicrobeColony colony, in Entity colonyEntity, Entity entity)
        {
            if (microbe == null || master == null || microbe.Colony != null)
                throw new ArgumentException("Microbe or master null or microbe already is in a colony");

            ColonyMembers.Add(microbe);
            Master.Mass += microbe.Mass;

            microbe.ColonyParent = master;
            master.ColonyChildren!.Add(microbe);
            microbe.Colony = this;
            microbe.ColonyChildren = new List<Microbe>();

            ColonyMembers.ForEach(m => m.OnColonyMemberAdded(microbe));

            // TODO: maybe in some situations creating the compound bag could be entirely safely skipped here
            colony.GetCompounds().UpdateColonyMembers(colony.ColonyMembers);

            membersDirty = true;
        }

        public static void RemoveFromColony(this ref MicrobeColony colony, in Entity colonyEntity, Entity entity)
        {
            colony.ColonyRotationMultiplierCalculated = false;

            throw new NotImplementedException();

            // OnColonyMemberRemoved(entity);

            if (colonyEntity.Has<EarlyMulticellularSpeciesMember>())
            {
                // Lost a member of the multicellular organism
                throw new NotImplementedException();

                // OnMulticellularColonyCellLost(microbe);
            }

            if (microbe?.Colony == null)
                throw new ArgumentException("Microbe null or not a member of a colony");

            if (!Equals(microbe.Colony, this))
                throw new ArgumentException("Cannot remove a colony member who isn't a member");

            if (microbe.ColonyChildren == null)
                throw new ArgumentException("Invalid microbe with no colony children setup on it");

            if (State == MicrobeState.Unbinding)
                State = MicrobeState.Normal;

            foreach (var colonyMember in ColonyMembers)
                colonyMember.OnColonyMemberRemoved(microbe);

            microbe.Colony = null;

            microbe.ReParentShapes(microbe, Vector3.Zero);

            while (microbe.ColonyChildren.Count != 0)
                RemoveFromColony(microbe.ColonyChildren[0]);

            ColonyMembers.Remove(microbe);

            microbe.ColonyParent?.ColonyChildren?.Remove(microbe);

            if (microbe.ColonyParent?.Colony != null && microbe.ColonyParent?.ColonyParent == null &&
                microbe.ColonyParent?.ColonyChildren?.Count == 0)
            {
                RemoveFromColony(microbe.ColonyParent);
            }

            microbe.ColonyParent = null;
            microbe.ColonyChildren = null;
            if (microbe != Master)
                Master.Mass -= microbe.Mass;

            colony.GetCompounds().UpdateColonyMembers(colony.ColonyMembers);

            membersDirty = true;
        }

        /// <summary>
        ///   Removes this cell and child cells from the colony.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     If this is the colony leader, this disbands the whole colony
        ///   </para>
        /// </remarks>
        public static void UnbindAll(in Entity entity)
        {
            ref var control = ref entity.Get<MicrobeControl>();

            if (control.State is MicrobeState.Unbinding or MicrobeState.Binding)
                control.State = MicrobeState.Normal;

            ref var organelles = ref entity.Get<OrganelleContainer>();

            if (!organelles.CanUnbind(ref entity.Get<SpeciesMember>(), entity))
                return;

            if (entity.Has<MicrobeColony>())
            {
                // TODO: once the colony leader can leave without the entire colony disbanding this perhaps should keep
                // the disband entire colony functionality
                // Colony!.RemoveFromColony(this);

                ref var colony = ref entity.Get<MicrobeColony>();
                colony.RemoveFromColony(entity, entity);
            }
            else if (entity.Has<MicrobeColonyMember>())
            {
                ref var member = ref entity.Get<MicrobeColonyMember>();

                if (!member.ColonyLeader.Has<MicrobeColony>())
                {
                    GD.PrintErr("Microbe colony lead entity is invalid for unbind");
                    return;
                }

                ref var colony = ref member.ColonyLeader.Get<MicrobeColony>();
                colony.RemoveFromColony(member.ColonyLeader, entity);
            }
        }

        /// <summary>
        ///   Called for each entity that is removed from a cell colony
        /// </summary>
        public static void OnColonyMemberRemoved(in Entity removedEntity)
        {
            if (removedEntity.Has<MicrobeEventCallbacks>())
            {
                ref var callbacks = ref removedEntity.Get<MicrobeEventCallbacks>();

                callbacks.OnUnbound?.Invoke(removedEntity);
            }

            // TODO: should this call AI reset?
            // ai?.ResetAI();
        }

        /// <summary>
        ///   Called for each entity that is added to a cell colony
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
        ///   Calculates an updated entity weight for a microbe colony to be passed to the <see cref="Spawned"/>
        ///   component as a new value
        /// </summary>
        /// <returns>Recalculated entity weight of the colony</returns>
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

            colony.ColonyRotationMultiplierCalculated = true;
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
