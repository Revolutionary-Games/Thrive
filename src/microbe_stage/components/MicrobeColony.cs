namespace Components
{
    using System;
    using System.Buffers;
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
        ///   All colony members of this colony
        /// </summary>
        public Entity[] ColonyMembers;

        /// <summary>
        ///   Lead cell of the colony. This is the entity that exists separately in the world, all others are attached
        ///   to it with <see cref="AttachedToEntity"/> components.
        /// </summary>
        public Entity Leader;

        public float ColonyRotationMultiplier;

        /// <summary>
        ///   Set to false when colony members change
        /// </summary>
        [JsonIgnore]
        public bool ColonyRotationMultiplierCalculated;

        public MicrobeColony(in Entity leader, params Entity[] otherMembers)
        {
            if (otherMembers.Length < 1)
                throw new ArgumentException("Microbe colony requires at least one lead cell and one member");

            Leader = leader;
            ColonyMembers = otherMembers;

            ColonyRotationMultiplier = 0;
            ColonyRotationMultiplierCalculated = false;
        }
    }

    public static class MicrobeColonyHelpers
    {
        public static readonly ArrayPool<Entity> MicrobeColonyMemberListPool = ArrayPool<Entity>.Create(100, 50);

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
    }
}
