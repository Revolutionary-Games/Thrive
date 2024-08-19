namespace Components;

using System;
using DefaultEcs;
using Godot;
using Systems;

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
    ///   If true, microbe will fire iron breakdown substance on next update.
    /// </summary>
    public bool QueuedSiderophoreToEmit;

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
    ///   How long to remain in <see cref="State"/> even if ATP requirements are not fulfilled (time in seconds)
    /// </summary>
    public float ForcedStateRemaining;

    /// <summary>
    ///   This is an overall state of the Microbe. Use <see cref="MicrobeControlHelpers.SetStateColonyAware"/> to
    ///   apply state for a colony lead cell in a way that also applies it to other colony members.
    /// </summary>
    public MicrobeState State;

    /// <summary>
    ///   A counter to determine the next toxin type to be fired (keeps an approximate count of fired toxins).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is a byte to increase the size of this struct less, and it is unlikely there needs to be more than
    ///     256 types of toxins to be fired so overflows are not serious.
    ///   </para>
    /// </remarks>
    public byte FiredToxinCount;

    /// <summary>
    ///   Whether this microbe is currently being slowed by environmental slime
    /// </summary>
    public bool SlowedBySlime;

    /// <summary>
    ///   Whether this microbe cannot sprint
    /// </summary>
    public bool OutOfSprint;

    /// <summary>
    ///   Whether this microbe is currently sprinting
    /// </summary>
    public bool Sprinting;

    /// <summary>
    ///   Whether this microbe is currently using mucocyst and is protected. This is an internal variable for
    ///   <see cref="MucocystSystem"/>. Don't modify!
    /// </summary>
    public bool MucocystEffectsApplied;

    /// <summary>
    ///   Constructs an instance with a sensible <see cref="LookAtPoint"/> set
    /// </summary>
    /// <param name="startingPosition">World position this entity is starting at</param>
    public MicrobeControl(Vector3 startingPosition)
    {
        LookAtPoint = startingPosition + new Vector3(0, 0, -1);
        MovementDirection = new Vector3(0, 0, 0);
        QueuedToxinToEmit = null;
        QueuedSiderophoreToEmit = false;
        SlimeSecretionCooldown = 0;
        QueuedSlimeSecretionTime = 0;
        AgentEmissionCooldown = 0;
        State = MicrobeState.Normal;
        SlowedBySlime = false;
        Sprinting = false;
        MucocystEffectsApplied = false;
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
    ///   Enters engulf mode. This variant forces the mode on for a few seconds even if ATP requirements are not
    ///   fulfilled at the cost of a bit of damage.
    /// </summary>
    public static void EnterEngulfModeForcedState(this ref MicrobeControl control, ref Health health,
        ref CompoundStorage compoundStorage, in Entity entity, Compound atp)
    {
        if (control.State == MicrobeState.Engulf)
            return;

        if (entity.Has<MicrobeColony>())
        {
            ref var colony = ref entity.Get<MicrobeColony>();

            if (colony.ColonyState != MicrobeState.Engulf)
            {
                colony.ColonyState = MicrobeState.Engulf;

                foreach (var colonyMember in colony.ColonyMembers)
                {
                    // The IsAlive check should be unnecessary here but as this is a general method there's this
                    // extra safety against crashing due to colony bugs
                    if (colonyMember != entity && colonyMember.IsAlive)
                    {
                        ref var memberControl = ref colonyMember.Get<MicrobeControl>();
                        ref var memberHealth = ref colonyMember.Get<Health>();
                        ref var memberCompoundStorage = ref colonyMember.Get<CompoundStorage>();

                        ForceStateApplyIfRequired(ref memberControl, ref memberHealth, ref memberCompoundStorage,
                            colonyMember, MicrobeState.Engulf, false, atp);
                    }
                }
            }
        }

        ForceStateApplyIfRequired(ref control, ref health, ref compoundStorage, entity, MicrobeState.Engulf, true, atp);
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

            // TODO: remove the delegate allocation here
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

    public static bool EmitSiderophore(this ref MicrobeControl control, ref OrganelleContainer organelles,
        in Entity entity)
    {
        // Disallow when engulfed
        if (entity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None)
            return false;

        if (entity.Has<MicrobeColony>())
        {
            ref var colony = ref entity.Get<MicrobeColony>();

            // TODO: remove the delegate allocation here
            colony.PerformForOtherColonyMembersThanLeader(m =>
                m.Get<MicrobeControl>()
                    .EmitSiderophore(ref m.Get<OrganelleContainer>(),
                        m));
        }

        if (control.AgentEmissionCooldown > 0)
            return false;

        // Only shoot if you have any iron-breaking organelles
        if (organelles.IronBreakdownEfficiency < 1)
            return false;

        control.QueuedSiderophoreToEmit = true;

        return true;
    }

    /// <summary>
    ///   Sets microbe speed straight forward.
    /// </summary>
    /// <param name="control">Control to hold commands.</param>
    /// <param name="speed">Speed at which to move.</param>
    public static void SetMoveSpeed(this ref MicrobeControl control, float speed)
    {
        control.MovementDirection = new Vector3(0, 0, -speed);
    }

    /// <summary>
    ///   Moves microbe towards target position, even if that position is not forward.
    ///   This does NOT handle any turning. So this is basically cell drifting.
    /// </summary>
    /// <param name="control">Control to hold commands.</param>
    /// <param name="selfPosition">Position of microbe moving.</param>
    /// <param name="targetPosition">Vector3 that microbe will move towards.</param>
    /// <param name="speed">Speed at which to move.</param>
    public static void SetMoveSpeedTowardsPoint(this ref MicrobeControl control, ref WorldPosition selfPosition,
        Vector3 targetPosition, float speed)
    {
        var vectorToTarget = targetPosition - selfPosition.Position;

        // If already at target don't move anywhere
        if (vectorToTarget.LengthSquared() < MathUtils.EPSILON)
        {
            control.MovementDirection = Vector3.Zero;
            return;
        }

        // MovementDirection doesn't have to be normalized, so it isn't here
        control.MovementDirection = selfPosition.Rotation.Inverse() * vectorToTarget * speed;
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
            control.QueuedSlimeSecretionTime = 3 * random.NextSingle();
        }
    }

    public static void SetMucocystState(this ref MicrobeControl control,
        ref OrganelleContainer organelleInfo, ref CompoundStorage availableCompounds, in Entity entity, bool state,
        Compound? mucilageCompound = null)
    {
        mucilageCompound ??= SimulationParameters.Instance.GetCompound("mucilage");

        if (entity.Has<MicrobeColony>())
        {
            ref var colony = ref entity.Get<MicrobeColony>();

            // TODO: is it a good idea to allocate a delegate here?
            colony.PerformForOtherColonyMembersThanLeader(m =>
                m.Get<MicrobeControl>()
                    .SetMucocystState(ref m.Get<OrganelleContainer>(), ref m.Get<CompoundStorage>(), m, state,
                        mucilageCompound));
        }

        if (organelleInfo.MucocystCount < 1)
            return;

        if (state)
        {
            // Don't allow spamming if not enough mucocyst. This is inside this if to allow exiting mucocyst shield
            // mode even without enough mucilage remaining.
            if (availableCompounds.Compounds.GetCompoundAmount(mucilageCompound) < Constants.MUCOCYST_MINIMUM_MUCILAGE)
                return;

            control.State = MicrobeState.MucocystShield;

            // TODO: maybe it is too loud if all cells in a colony play the sound?
            entity.Get<SoundEffectPlayer>().PlaySoundEffect("res://assets/sounds/soundeffects/microbe-slime-jet.ogg");
        }
        else
        {
            control.State = MicrobeState.Normal;
        }
    }

    /// <summary>
    ///   Forcefully sets a cell in a state and deals damage
    /// </summary>
    public static void ForceStateApplyIfRequired(this ref MicrobeControl control, ref Health health,
        ref CompoundStorage compoundStorage, in Entity entity, MicrobeState targetState, bool sendHUDNotice,
        Compound atp)
    {
        // Do nothing if already in correct state
        if (control.State == targetState)
            return;

        control.State = targetState;

        if (NeedsToUseForcedState(ref compoundStorage, atp))
        {
            // Need to force this cell into a mode, so deal the damage
            health.DealDamage(Constants.ENGULF_NO_ATP_DAMAGE, "forcedState");

            control.ForcedStateRemaining = Constants.ENGULF_NO_ATP_TIME;

            if (sendHUDNotice)
            {
                entity.SendNoticeIfPossible(() =>
                    new SimpleHUDMessage(Localization.Translate("ENGULF_NO_ATP_DAMAGE_MESSAGE")));
            }
        }
    }

    public static bool NeedsToUseForcedState(ref CompoundStorage compoundStorage, Compound atp)
    {
        if (compoundStorage.Compounds.GetCompoundAmount(atp) > Constants.ENGULF_NO_ATP_TRIGGER_THRESHOLD)
        {
            // If cell has good amount of ATP, don't force it into engulf mode
            return false;
        }

        return true;
    }
}
