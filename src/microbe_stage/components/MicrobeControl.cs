namespace Components
{
    using System;
    using DefaultEcs;
    using Godot;

    /// <summary>
    ///   Control variables for specifying how a microbe wants to move / behave
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct MicrobeControl
    {
        /// <summary>
        ///   The point towards which the microbe will move to point to
        /// </summary>
        public Vector3 LookAtPoint;

        /// <summary>
        ///   The direction the microbe wants to move. Doesn't need to be normalized
        /// </summary>
        public Vector3 MovementDirection;

        /// <summary>
        ///   If not null this microbe will fire the specified toxin on next update. This is done to allow
        ///   multithreaded AI to decide to fire a toxin.
        /// </summary>
        public Compound? QueuedToxinToEmit;

        /// <summary>
        ///   This is here as this is very closely related to <see cref="QueuedSlimeSecretionTime"/>
        /// </summary>
        public float SlimeSecretionCooldown;

        /// <summary>
        ///   How long this microbe wants to emit slime (this is done so that AI which doesn't run each frame can still
        ///   sufficiently control the emission of slime)
        /// </summary>
        public float QueuedSlimeSecretionTime;

        /// <summary>
        ///   Time until this microbe can fire agents (toxin) again
        /// </summary>
        public float AgentEmissionCooldown;

        /// <summary>
        ///   This is an overall state of the Microbe. Use <see cref="MicrobeControlHelpers.SetStateColonyAware"/> to
        ///   apply state for a colony lead cell in a way that also applies it to other colony members.
        /// </summary>
        public MicrobeState State;

        /// <summary>
        ///   Whether this microbe is currently being slowed by environmental slime
        /// </summary>
        public bool SlowedBySlime;

        /// <summary>
        ///   Whether this microbe is currenyly sprinting
        /// </summary>
        public bool Sprinting;

        /// <summary>
        ///   Constructs an instance with a sensible <see cref="LookAtPoint"/> set
        /// </summary>
        /// <param name="startingPosition">World position this entity is starting at</param>
        public MicrobeControl(Vector3 startingPosition)
        {
            LookAtPoint = startingPosition + new Vector3(0, 0, -1);
            MovementDirection = new Vector3(0, 0, 0);
            QueuedToxinToEmit = null;
            SlimeSecretionCooldown = 0;
            QueuedSlimeSecretionTime = 0;
            AgentEmissionCooldown = 0;
            State = MicrobeState.Normal;
            SlowedBySlime = false;
            Sprinting = false;
        }
    }

    public static class MicrobeControlHelpers
    {
        /// <summary>
        ///   Sets microbe state in a way that also applies the state to a colony if the entity is a lead cell
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Applies a colony-wide state (for example makes all cells that can be in engulf mode in the colony be in
        ///     engulf mode even if the lead cell cannot engulf)
        ///   </para>
        /// </remarks>
        public static void SetStateColonyAware(this ref MicrobeControl control, in Entity entity,
            MicrobeState targetState)
        {
            if (entity.Has<MicrobeColony>())
            {
                ref var colony = ref entity.Get<MicrobeColony>();

                if (colony.ColonyState != targetState)
                {
                    colony.ColonyState = targetState;

                    foreach (var colonyMember in colony.ColonyMembers)
                    {
                        // The IsAlive check should be unnecessary here but as this is a general method there's this
                        // extra safety against crashing due to colony bugs
                        if (colonyMember != entity && colonyMember.IsAlive)
                        {
                            ref var memberControl = ref colonyMember.Get<MicrobeControl>();
                            memberControl.State = targetState;
                        }
                    }
                }
            }

            control.State = targetState;
        }

        /// <summary>
        ///   Queues a toxin emit if possible. Only one can be queued at a time.
        /// </summary>
        public static bool EmitToxin(this ref MicrobeControl control, ref OrganelleContainer organelles,
            CompoundBag availableCompounds, in Entity entity, Compound? agentType = null)
        {
            // Disallow toxins when engulfed
            if (entity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None)
                return false;

            agentType ??= SimulationParameters.Instance.GetCompound("oxytoxy");

            if (entity.Has<MicrobeColony>())
            {
                ref var colony = ref entity.Get<MicrobeColony>();

                colony.PerformForOtherColonyMembersThanLeader(m =>
                    m.Get<MicrobeControl>()
                        .EmitToxin(ref m.Get<OrganelleContainer>(), m.Get<CompoundStorage>().Compounds,
                            m, agentType));
            }

            if (control.AgentEmissionCooldown > 0)
                return false;

            // Only shoot if you have an agent vacuole.
            if (organelles.AgentVacuoleCount < 1)
                return false;

            float amountAvailable = availableCompounds.GetCompoundAmount(agentType);

            // Skip if too little agent available
            if (amountAvailable < Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
                return false;

            control.QueuedToxinToEmit = agentType;

            return true;
        }

        public static void SetMoveSpeed(this ref MicrobeControl control, float speed)
        {
            control.MovementDirection = new Vector3(0, 0, -speed);
        }

        public static void QueueSecreteSlime(this ref MicrobeControl control,
            ref OrganelleContainer organelleInfo, in Entity entity, float duration)
        {
            if (entity.Has<MicrobeColony>())
            {
                ref var colony = ref entity.Get<MicrobeColony>();

                // TODO: is it a good idea to allocate a delegate here?
                colony.PerformForOtherColonyMembersThanLeader(m =>
                    m.Get<MicrobeControl>()
                        .QueueSecreteSlime(ref m.Get<OrganelleContainer>(), m, duration));
            }

            if (organelleInfo.SlimeJets == null || organelleInfo.SlimeJets.Count < 1)
                return;

            control.QueuedSlimeSecretionTime += duration;
        }

        public static void SecreteSlimeForSomeTime(this ref MicrobeControl control,
            ref OrganelleContainer organelleInfo, Random random)
        {
            // TODO: AI might want in the future to use all slime jets in a colony

            if ((organelleInfo.SlimeJets?.Count ?? 0) > 0)
            {
                // Randomise the time spent ejecting slime, from 0 to 3 seconds
                control.QueuedSlimeSecretionTime = 3 * random.NextFloat();
            }
        }
    }
}
