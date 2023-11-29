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
        ///   This is an overall state of the Microbe
        /// </summary>
        public MicrobeState State;

        /// <summary>
        ///   Whether this microbe is currently being slowed by environmental slime
        /// </summary>
        public bool SlowedBySlime;

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
        }
    }

    public static class MicrobeControlHelpers
    {
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

                colony.PerformForOtherColonyMembersThanLeader(member =>
                    member.Get<MicrobeControl>()
                        .EmitToxin(ref member.Get<OrganelleContainer>(), member.Get<CompoundStorage>().Compounds,
                            member, agentType));
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
                colony.PerformForOtherColonyMembersThanLeader(member =>
                    member.Get<MicrobeControl>()
                        .QueueSecreteSlime(ref member.Get<OrganelleContainer>(), member, duration));
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
