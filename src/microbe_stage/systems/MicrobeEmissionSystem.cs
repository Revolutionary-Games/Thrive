namespace Systems;

using System;
using System.Linq;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Handles microbes emitting agents (toxins) or slime
/// </summary>
/// <remarks>
///   <para>
///     This technically writes to the <see cref="OrganelleContainer"/> but only to the Slime Jets' active property
///     and nothing else so this shouldn't conflict with other things.
///   </para>
/// </remarks>
[With(typeof(MicrobeControl))]
[With(typeof(SpeciesMember))]
[With(typeof(OrganelleContainer))]
[With(typeof(CellProperties))]
[With(typeof(SoundEffectPlayer))]
[With(typeof(WorldPosition))]
[With(typeof(CompoundStorage))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(Engulfable))]
[ReadsComponent(typeof(AttachedToEntity))]
[WritesToComponent(typeof(OrganelleContainer))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RunsAfter(typeof(ProcessSystem))]
[RuntimeCost(1)]
public sealed class MicrobeEmissionSystem : AEntitySetSystem<float>
{
    private readonly IWorldSimulation worldSimulation;
    private readonly CompoundCloudSystem clouds;

    private readonly Compound mucilage;

    public MicrobeEmissionSystem(IWorldSimulation worldSimulation, CompoundCloudSystem cloudSystem, World world,
        IParallelRunner parallelRunner) :
        base(world, parallelRunner)
    {
        this.worldSimulation = worldSimulation;
        clouds = cloudSystem;

        mucilage = SimulationParameters.Instance.GetCompound("mucilage");
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var control = ref entity.Get<MicrobeControl>();

        // Reduce agent emission cooldown
        // TODO: is it faster to check than to just blindly always subtract delta here?
        control.AgentEmissionCooldown -= delta;
        if (control.AgentEmissionCooldown < 0)
            control.AgentEmissionCooldown = 0;

        control.SlimeSecretionCooldown -= delta;
        if (control.SlimeSecretionCooldown < 0)
            control.SlimeSecretionCooldown = 0;

        ref var organelles = ref entity.Get<OrganelleContainer>();
        ref var cellProperties = ref entity.Get<CellProperties>();
        ref var position = ref entity.Get<WorldPosition>();
        ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

        var compounds = entity.Get<CompoundStorage>().Compounds;

        bool engulfed = entity.Has<Engulfable>() &&
            entity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None;

        // Fire queued agents
        if (control.QueuedToxinToEmit != null)
        {
            EmitProjectile(entity, ref control, ref organelles, ref cellProperties, ref soundEffectPlayer, ref position,
                control.QueuedToxinToEmit, compounds, engulfed, false);
            control.QueuedToxinToEmit = null;
        }

        if (control.QueuedSiderophoreToEmit)
        {
            EmitProjectile(entity, ref control, ref organelles, ref cellProperties, ref soundEffectPlayer, ref position,
                null, null, engulfed, true);
            control.QueuedSiderophoreToEmit = false;
        }

        // This method itself checks for the preconditions on emitting slime
        HandleSlimeSecretion(entity, ref control, ref organelles, ref cellProperties, ref soundEffectPlayer,
            ref position, compounds, engulfed, delta);
    }

    /// <summary>
    ///   Handles colony logic to determine the actual facing vector of this microbe
    /// </summary>
    /// <returns>A Vector3 of this microbe's real facing</returns>
    private static Vector3 FacingDirection(in Entity entity, ref WorldPosition position)
    {
        if (entity.Has<AttachedToEntity>())
        {
            var attachedTo = entity.Get<AttachedToEntity>().AttachedTo;

            if (attachedTo.Has<WorldPosition>())
            {
                // Use parent rotation rather than our own to get the whole cell colony facing direction rather
                // than our facing direction in world space
                return attachedTo.Get<WorldPosition>().Rotation * Vector3.Forward;
            }
        }

        return position.Rotation * Vector3.Forward;
    }

    /// <summary>
    ///   Tries to fire a toxin/siderophore if possible
    /// </summary>
    private void EmitProjectile(in Entity entity, ref MicrobeControl control, ref OrganelleContainer organelles,
        ref CellProperties cellProperties, ref SoundEffectPlayer soundEffectPlayer, ref WorldPosition position,
        Compound? agentType, CompoundBag? compounds, bool engulfed, bool siderophore)
    {
        if (engulfed)
            return;

        if (control.AgentEmissionCooldown > 0)
            return;

        // Can't shoot if membrane is not ready
        if (!cellProperties.IsMembraneReady())
            return;

        float ejectionDistance = cellProperties.CreatedMembrane!.EncompassingCircleRadius +
            Constants.AGENT_EMISSION_DISTANCE_OFFSET;

        if (cellProperties.IsBacteria)
            ejectionDistance *= 0.5f;

        // Find the direction the microbe is facing
        // (actual rotation, not LookAtPoint, also takes colony membership into account and uses the
        // parent rotation)
        var direction = FacingDirection(entity, ref position);

        var emissionPosition = position.Position + (direction * ejectionDistance);

        if (siderophore)
        {
            // Only shoot if you have any iron organelles
            if (organelles.IronBreakdownEfficiency < 1)
                return;

            // The cooldown time is inversely proportional to the power of iron agents in total
            control.AgentEmissionCooldown = Constants.AGENT_EMISSION_COOLDOWN * 5 / organelles.IronBreakdownEfficiency;

            SpawnHelpers.SpawnIronProjectile(worldSimulation, organelles.IronBreakdownEfficiency,
                Constants.EMITTED_AGENT_LIFETIME, emissionPosition, direction, organelles.IronBreakdownEfficiency,
                entity);

            // TODO: a separate siderophore sound effect?
            soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");
        }
        else
        {
            // Only shoot if you have an agent vacuole.
            if (organelles.AgentVacuoleCount < 1)
                return;

            if (compounds == null || agentType == null)
                return;

            float amountAvailable = compounds.GetCompoundAmount(agentType);

            var selectedToxinType = ToxinType.Oxytoxy;

            // Pick the next toxin type to fire, but only if the data is present (for example loading an earlier save
            // wouldn't have this data set). This uses a round-robin algorithm to pick the next toxin type.
            if (organelles.AvailableToxinTypes != null)
            {
                var totalToxins = organelles.AvailableToxinTypes.Count;

                // TODO: should there be a shortcut path for cases where there is just one toxin type?

                if (totalToxins != 0)
                {
                    var selectedRange = control.FiredToxinCount % totalToxins;

                    int typeCounter = 0;

                    foreach (var toxinType in organelles.AvailableToxinTypes)
                    {
                        if (typeCounter > selectedRange)
                            break;

                        selectedToxinType = toxinType.Key;
                        ++typeCounter;
                    }
                }
                else
                {
                    GD.PrintErr("Cell has total count of toxin types 0 with agent vacuoles above 0");
                }

                // TODO: this needs changing if fire/toxicity is customizable per agent type (and separate compounds
                // aren't used per agent type)

                // Emit as much as you have, but don't start if there's way too little toxin
                float amountEmitted = Math.Min(amountAvailable, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT);
                if (amountEmitted < Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
                    return;

                compounds.TakeCompound(agentType, amountEmitted);

                // Adjust amount based on toxicity to make the shot more or less effective
                var damagingToxinAmount =
                    amountEmitted * ToxinAmountMultiplierFromToxicity(organelles.AverageToxinToxicity);

                var agent = SpawnHelpers.SpawnAgentProjectile(worldSimulation,
                    new AgentProperties(entity.Get<SpeciesMember>().Species, agentType, selectedToxinType),
                    damagingToxinAmount, Constants.EMITTED_AGENT_LIFETIME, emissionPosition, direction, amountEmitted,
                    entity);

                ModLoader.ModInterface.TriggerOnToxinEmitted(agent);

                ++control.FiredToxinCount;

                if (amountEmitted < Constants.MAXIMUM_AGENT_EMISSION_AMOUNT / 2)
                {
                    soundEffectPlayer
                        .PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin-low.ogg");
                }
                else
                {
                    soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");
                }
            }

            // TODO: the above part is already implemented as extension for PlayerMicrobeInput (so could share a bit of
            // code for checking if ready to shoot yet)

            // The cooldown time is inversely proportional to the amount of agent vacuoles.
            control.AgentEmissionCooldown =
                ToxinCooldownWithToxicity(organelles.AgentVacuoleCount, organelles.AverageToxinToxicity);
        }
    }

    private float ToxinAmountMultiplierFromToxicity(float toxicity)
    {
        // Scale toxin damage from a low-damage high-firerate, to low-firerate high-damage

        if (toxicity < 0)
        {
            // Low-damage
            return 0.89f * (1 - Math.Abs(toxicity)) + 0.1f;
        }

        if (toxicity > 0)
        {
            // High-damage
            return 0.99f * toxicity + 1.0f;
        }

        // No modification from default
        return 1;
    }

    private float ToxinCooldownWithToxicity(int vacuoleCount, float toxicity)
    {
        if (toxicity < 0)
        {
            // High-firerate
            return Constants.AGENT_EMISSION_COOLDOWN / vacuoleCount * (1.0f - (0.5f * Math.Abs(toxicity)));
        }

        if (toxicity > 0)
        {
            // Low-firerate
            return Constants.AGENT_EMISSION_COOLDOWN / vacuoleCount * (1 + toxicity);
        }

        // No modification from default
        return Constants.AGENT_EMISSION_COOLDOWN / vacuoleCount;
    }

    private void HandleSlimeSecretion(in Entity entity, ref MicrobeControl control,
        ref OrganelleContainer organelles, ref CellProperties cellProperties,
        ref SoundEffectPlayer soundEffectPlayer, ref WorldPosition worldPosition,
        CompoundBag compounds, bool engulfed, float delta)
    {
        // Ignore if we have no slime jets
        if (organelles.SlimeJets == null)
            return;

        int jetCount = organelles.SlimeJets.Count;

        if (jetCount < 1)
            return;

        // Start a cooldown timer if we're out of mucilage to prevent visible trails or puffs when empty.
        // Scaling by slime jet count ensures we aren't producing mucilage fast enough to beat this check.
        if (compounds.GetCompoundAmount(mucilage) < Constants.MUCILAGE_MIN_TO_VENT * jetCount)
            control.SlimeSecretionCooldown = Constants.MUCILAGE_COOLDOWN_TIMER;

        // Don't emit slime when engulfed
        if (engulfed)
            control.QueuedSlimeSecretionTime = 0;

        // If we've been told to secrete slime and can do it, proceed
        if (control.QueuedSlimeSecretionTime > 0 && control.SlimeSecretionCooldown <= 0)
        {
            // Play a sound only if we've just started, i.e. only if no jets are already active
            if (organelles.SlimeJets.All(c => !c.Active))
                soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-slime-jet.ogg");

            // Activate all jets, which will constantly secrete slime until we turn them off
            foreach (var jet in organelles.SlimeJets)
            {
                // Make sure this is animating
                jet.Active = true;

                // Secrete the slime
                float slimeToSecrete = Math.Min(Constants.COMPOUNDS_TO_VENT_PER_SECOND * delta,
                    compounds.GetCompoundAmount(mucilage));

                var direction = jet.GetDirection();

                // Eject mucilage at the maximum rate in the opposite direction to this organelle's rotation
                slimeToSecrete = cellProperties.EjectCompound(ref worldPosition, compounds, clouds, mucilage,
                    slimeToSecrete, -direction, 2);

                // Queue movement force to be used by the movement system based on the amount of slime ejected
                jet.AddQueuedForce(entity, slimeToSecrete);
            }
        }
        else
        {
            // Deactivate the jets if we aren't supposed to secrete slime
            foreach (var jet in organelles.SlimeJets)
                jet.Active = false;
        }

        control.QueuedSlimeSecretionTime -= delta;
        if (control.QueuedSlimeSecretionTime < 0)
            control.QueuedSlimeSecretionTime = 0;
    }
}
