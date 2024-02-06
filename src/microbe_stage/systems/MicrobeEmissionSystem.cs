namespace Systems
{
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
    [ReadsComponent(typeof(SpeciesMember))]
    [ReadsComponent(typeof(CellProperties))]
    [ReadsComponent(typeof(WorldPosition))]
    [ReadsComponent(typeof(Engulfable))]
    [ReadsComponent(typeof(AttachedToEntity))]
    [WritesToComponent(typeof(OrganelleContainer))]
    [RunsBefore(typeof(MicrobeMovementSystem))]
    [RunsAfter(typeof(ProcessSystem))]
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
                EmitToxin(entity, ref control, ref organelles, ref cellProperties, ref soundEffectPlayer, ref position,
                    control.QueuedToxinToEmit, compounds, engulfed);
                control.QueuedToxinToEmit = null;
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
                    return attachedTo.Get<WorldPosition>().Rotation.Xform(Vector3.Forward);
                }
            }

            return position.Rotation.Xform(Vector3.Forward);
        }

        /// <summary>
        ///   Tries to fire a toxin if possible
        /// </summary>
        private void EmitToxin(in Entity entity, ref MicrobeControl control, ref OrganelleContainer organelles,
            ref CellProperties cellProperties, ref SoundEffectPlayer soundEffectPlayer, ref WorldPosition position,
            Compound agentType, CompoundBag compounds, bool engulfed)
        {
            if (engulfed)
                return;

            if (control.AgentEmissionCooldown > 0)
                return;

            // Only shoot if you have an agent vacuole.
            if (organelles.AgentVacuoleCount < 1)
                return;

            // Can't shoot if membrane is not ready
            if (!cellProperties.IsMembraneReady())
                return;

            float amountAvailable = compounds.GetCompoundAmount(agentType);

            // Emit as much as you have, but don't start the cooldown if that's zero
            float amountEmitted = Math.Min(amountAvailable, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT);
            if (amountEmitted < Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
                return;

            // TODO: the above part is already implemented as extension for PlayerMicrobeInput

            compounds.TakeCompound(agentType, amountEmitted);

            // The cooldown time is inversely proportional to the amount of agent vacuoles.
            control.AgentEmissionCooldown = Constants.AGENT_EMISSION_COOLDOWN / organelles.AgentVacuoleCount;

            float ejectionDistance = cellProperties.CreatedMembrane!.EncompassingCircleRadius +
                Constants.AGENT_EMISSION_DISTANCE_OFFSET;

            if (cellProperties.IsBacteria)
                ejectionDistance *= 0.5f;

            // Find the direction the microbe is facing
            // (actual rotation, not LookAtPoint, also takes colony membership into account and uses the
            // parent rotation)
            var direction = FacingDirection(entity, ref position);

            var emissionPosition = position.Position + (direction * ejectionDistance);

            var agent = SpawnHelpers.SpawnAgentProjectile(worldSimulation,
                new AgentProperties(entity.Get<SpeciesMember>().Species, agentType), amountEmitted,
                Constants.EMITTED_AGENT_LIFETIME, emissionPosition, direction, amountEmitted, entity);

            ModLoader.ModInterface.TriggerOnToxinEmitted(agent);

            if (amountEmitted < Constants.MAXIMUM_AGENT_EMISSION_AMOUNT / 2)
            {
                soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin-low.ogg");
            }
            else
            {
                soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");
            }
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
}
