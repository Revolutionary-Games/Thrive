namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.Command;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles microbes dying when they run out of health and also updates the membrane visuals to indicate how
    ///   close to death a microbe is
    /// </summary>
    [With(typeof(Health))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(MicrobeShaderParameters))]
    [With(typeof(CellProperties))]
    [With(typeof(Physics))]
    [With(typeof(WorldPosition))]
    [With(typeof(MicrobeControl))]
    [With(typeof(ManualPhysicsControl))]
    [With(typeof(SoundEffectPlayer))]
    [With(typeof(CompoundAbsorber))]
    [WritesToComponent(typeof(MicrobeColony))]
    [RunsAfter(typeof(OsmoregulationAndHealingSystem))]
    [RunsBefore(typeof(EngulfingSystem))]
    public sealed class MicrobeDeathSystem : AEntitySetSystem<float>
    {
        private readonly IWorldSimulation worldSimulation;
        private readonly ISpawnSystem spawnSystem;

        private readonly Random random = new();

        private readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

        private GameWorld? gameWorld;

        public MicrobeDeathSystem(IWorldSimulation worldSimulation, ISpawnSystem spawnSystem, World world,
            IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
            this.worldSimulation = worldSimulation;
            this.spawnSystem = spawnSystem;
        }

        /// <summary>
        ///   Delegate called to customize things spawned by <see cref="MicrobeDeathSystem.SpawnCorpseChunks"/>.
        ///   The return value defines the initial velocity to set. Modifying the position parameter allows
        ///   controlling the spawn location.
        /// </summary>
        public delegate Vector3 CustomizeSpawnedChunk(ref Vector3 position);

        public static void SpawnCorpseChunks(ref OrganelleContainer organelleContainer, CompoundBag compounds,
            ISpawnSystem spawnSystem, IWorldSimulation worldSimulation, EntityCommandRecorder recorder,
            Vector3 basePosition, Random random,
            CustomizeSpawnedChunk? customizeCallback,
            Compound? glucose)
        {
            if (organelleContainer.Organelles == null)
                throw new InvalidOperationException("Organelles can't be null when determining chunks to drop");

            // Eject the compounds that was in the microbe
            var compoundsToRelease = new Dictionary<Compound, float>();

            foreach (var type in SimulationParameters.Instance.GetCloudCompounds())
            {
                var amount = compounds.GetCompoundAmount(type) *
                    Constants.COMPOUND_RELEASE_FRACTION;

                compoundsToRelease[type] = amount;
            }

            // Eject some part of the build cost of all the organelles
            foreach (var organelle in organelleContainer.Organelles!)
            {
                foreach (var entry in organelle.Definition.InitialComposition)
                {
                    compoundsToRelease.TryGetValue(entry.Key, out var existing);

                    // Only add up if there's still some compounds left, otherwise
                    // we're releasing compounds out of thin air.
                    if (existing > 0)
                    {
                        compoundsToRelease[entry.Key] = existing + (entry.Value *
                            Constants.COMPOUND_MAKEUP_RELEASE_FRACTION);
                    }
                }
            }

            EngulfableHelpers.CalculateBonusDigestibleGlucose(compoundsToRelease, compounds, glucose);

            // Queues either 1 corpse chunk or a factor of the hexes
            // TODO: should there be a max amount (maybe like 20?)
            int chunksToSpawn = Math.Max(1, organelleContainer.HexCount / Constants.CORPSE_CHUNK_DIVISOR);

            // An enumerator to step through all available organelles in a random order when making chunks
            using var organellesAvailableEnumerator =
                organelleContainer.Organelles.Organelles.OrderBy(_ => random.Next()).GetEnumerator();

            // The default model for chunks is the cytoplasm model in case there isn't a model left in the species
            var defaultChunkScene = SimulationParameters.Instance
                    .GetOrganelleType(Constants.DEFAULT_CHUNK_MODEL_NAME).CorpseChunkScene ??
                throw new Exception("No chunk scene set on default organelle type to use");

            var chunkName = TranslationServer.Translate("CHUNK_CELL_CORPSE_PART");

            for (int i = 0; i < chunksToSpawn; ++i)
            {
                // Amount of compound in one chunk
                float amount = organelleContainer.HexCount / Constants.CORPSE_CHUNK_AMOUNT_DIVISOR;

                var positionAdded = new Vector3(random.Next(-2.0f, 2.0f), 0,
                    random.Next(-2.0f, 2.0f));

                var chunkType = new ChunkConfiguration
                {
                    ChunkScale = 1.0f,
                    Dissolves = true,
                    PhysicsDensity = 1200.0f,
                    Radius = 1.0f,
                    Size = 3.0f,
                    VentAmount = 0.1f,

                    // Add compounds
                    Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>(),

                    Name = chunkName,
                };

                // They were added in order already so looping through this other thing is fine
                foreach (var entry in compoundsToRelease)
                {
                    var compoundValue = new ChunkConfiguration.ChunkCompound
                    {
                        // Randomize compound amount a bit so things "rot away"
                        Amount = (entry.Value / (random.Next(amount / 3.0f, amount) *
                            Constants.CHUNK_ENGULF_COMPOUND_DIVISOR)) * Constants.CORPSE_COMPOUND_COMPENSATION,
                    };

                    chunkType.Compounds[entry.Key] = compoundValue;
                }

                chunkType.Meshes = new List<ChunkConfiguration.ChunkScene>();

                var sceneToUse = new ChunkConfiguration.ChunkScene
                {
                    ScenePath = defaultChunkScene,
                    PlayAnimation = false,
                };

                // Will only loop if there are still organelles available
                while (organellesAvailableEnumerator.MoveNext() && organellesAvailableEnumerator.Current != null)
                {
                    var organelleDefinition = organellesAvailableEnumerator.Current.Definition;

                    if (!string.IsNullOrEmpty(organelleDefinition.CorpseChunkScene))
                    {
                        sceneToUse.ScenePath = organelleDefinition.CorpseChunkScene!;
                    }
                    else if (!string.IsNullOrEmpty(organelleDefinition.DisplayScene))
                    {
                        sceneToUse.ScenePath = organelleDefinition.DisplayScene!;
                        sceneToUse.SceneModelPath = organelleDefinition.DisplaySceneModelPath;
                        sceneToUse.SceneAnimationPath = organelleDefinition.DisplaySceneAnimation;
                    }
                    else
                    {
                        continue;
                    }

                    // ScenePath is always valid now here so we just break after the first organelle we were able to
                    // use
                    break;
                }

                chunkType.Meshes.Add(sceneToUse);

                var position = basePosition + positionAdded;
                Vector3 velocity = Vector3.Zero;

                if (customizeCallback != null)
                {
                    velocity = customizeCallback.Invoke(ref position);
                }

                // Finally spawn a chunk with the settings

                var chunk = SpawnHelpers.SpawnChunkWithoutFinalizing(worldSimulation, recorder, chunkType,
                    position, random, true, velocity);

                // Add to the spawn system to make these chunks limit possible number of entities
                spawnSystem.NotifyExternalEntitySpawned(chunk, Constants.MICROBE_DESPAWN_RADIUS_SQUARED, 1);

                ModLoader.ModInterface.TriggerOnChunkSpawned(chunk, false);
            }
        }

        public void SetWorld(GameWorld world)
        {
            gameWorld = world;
        }

        protected override void PreUpdate(float state)
        {
            base.PreUpdate(state);

            if (gameWorld == null)
                throw new InvalidOperationException("GameWorld not set");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var health = ref entity.Get<Health>();

            if (health.DeathProcessed)
                return;

            ref var cellProperties = ref entity.Get<CellProperties>();
            if (cellProperties.CreatedMembrane != null)
            {
                if (health.MaxHealth <= 0)
                {
                    GD.PrintErr("Cell doesn't have max health set");
                    cellProperties.CreatedMembrane.HealthFraction = 0;
                }
                else
                {
                    cellProperties.CreatedMembrane.HealthFraction = health.CurrentHealth / health.MaxHealth;
                }
            }

            if (health.CurrentHealth <= 0 || health.Dead)
            {
                // Ensure dead flag is always set, as otherwise this will cause "zombies"
                health.Dead = true;

                if (HandleMicrobeDeath(ref cellProperties, entity))
                    health.DeathProcessed = true;
            }
        }

        private bool HandleMicrobeDeath(ref CellProperties cellProperties, in Entity entity)
        {
            EntityCommandRecorder? commandRecorder = null;

            if (entity.Has<AttachedToEntity>())
            {
                // When in a colony needs to detach
                if (entity.Has<MicrobeColonyMember>())
                {
                    commandRecorder = worldSimulation.StartRecordingEntityCommands();

                    if (!MicrobeColonyHelpers.UnbindAll(entity, commandRecorder))
                    {
                        GD.PrintErr("Failed to unbind microbe from colony on death");
                    }
                }

                // Being engulfed handling is in OnKilled and OnExpelledFromEngulfment
                // Dropping corpse chunks won't make sense while inside a cell (being engulfed)
            }

            if (entity.Has<MicrobeColony>())
            {
                // Handle colony lead cell dying (disband the colony)
                commandRecorder ??= worldSimulation.StartRecordingEntityCommands();

                if (!MicrobeColonyHelpers.UnbindAll(entity, commandRecorder))
                {
                    GD.PrintErr("Failed to unbind colony of a dying lead cell");
                }
            }

            if (OnKilled(ref cellProperties, entity, ref commandRecorder))
            {
                // TODO: engulfed death doesn't trigger this mod interface...
                ModLoader.ModInterface.TriggerOnMicrobeDied(entity, entity.Has<PlayerMarker>());

                if (commandRecorder != null)
                    worldSimulation.FinishRecordingEntityCommands(commandRecorder);

                return true;
            }

            if (commandRecorder != null)
                worldSimulation.FinishRecordingEntityCommands(commandRecorder);

            return false;
        }

        /// <summary>
        ///   Operations that should be done when this cell is killed
        /// </summary>
        /// <returns>
        ///   True when the death could be processed, false if the entity isn't ready to process the death
        /// </returns>
        private bool OnKilled(ref CellProperties cellProperties, in Entity entity,
            ref EntityCommandRecorder? commandRecorder)
        {
            ref var organelleContainer = ref entity.Get<OrganelleContainer>();

            if (organelleContainer.Organelles == null)
            {
                GD.Print("Can't kill a microbe yet with uninitialized organelles");
                return false;
            }

            ref var control = ref entity.Get<MicrobeControl>();
            ref var physics = ref entity.Get<Physics>();

            // Reset some stuff
            // This uses normal set to allow still potentially alive colony members to stay in their states
            control.State = MicrobeState.Normal;
            control.MovementDirection = new Vector3(0, 0, 0);
            organelleContainer.AllOrganellesDivided = false;

            // Stop compound absorbing
            ref var absorber = ref entity.Get<CompoundAbsorber>();
            absorber.AbsorbSpeed = -1;
            absorber.AbsorbRadius = -1;

            // Disable collisions
            physics.SetCollisionDisableState(true);

            // TODO: should we reset the velocity here?
            // ref var physicsControl = ref entity.Get<ManualPhysicsControl>();
            // physicsControl.RemoveVelocity = true;
            // physicsControl.PhysicsApplied = false;

            var species = entity.Get<SpeciesMember>().Species;

            // Subtract population
            if (!entity.Has<PlayerMarker>() && !species.PlayerSpecies)
            {
                gameWorld!.AlterSpeciesPopulationInCurrentPatch(species,
                    Constants.CREATURE_DEATH_POPULATION_LOSS, TranslationServer.Translate("DEATH"));
            }

            // Record player death in statistics
            if (entity.Has<PlayerMarker>())
            {
                gameWorld?.StatisticsTracker.TotalPlayerDeaths.Increment(1);
            }

            ref var engulfable = ref entity.Get<Engulfable>();

            commandRecorder ??= worldSimulation.StartRecordingEntityCommands();
            var entityRecord = commandRecorder.Record(entity);

            // Add a timed life component to make sure the entity will despawn after the death animation
            entityRecord.Set(new TimedLife
            {
                TimeToLiveRemaining = 1 / Constants.MEMBRANE_DISSOLVE_SPEED * 2,
            });

            // TODO: if we have problems with dead microbes behaving weirdly in loaded saves, uncomment the next line
            // worldSimulation.ReportEntityDyingSoon(entity);

            if (entity.Has<MicrobeEventCallbacks>())
            {
                ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

                // If a microbe died, notify about this. This does really important stuff if the player died before
                // entering the editor
                callbacks.OnReproductionStatus?.Invoke(entity, false);
            }

            if (engulfable.PhagocytosisStep != PhagocytosisPhase.None)
            {
                // When dying when engulfed all of the normal actions don't apply
                // Special handling for this is in EngulfableHelpers.OnExpelledFromEngulfment
                return true;
            }

            var compounds = entity.Get<CompoundStorage>().Compounds;
            ref var position = ref entity.Get<WorldPosition>();

            ApplyDeathVisuals(ref cellProperties, ref organelleContainer, ref position, entity, commandRecorder);

            // Ejecting all engulfed objects on death are now handled by EngulfingSystem

            // Releasing all the agents.

            if (organelleContainer.AgentVacuoleCount > 0)
            {
                ReleaseAllAgents(ref position, entity, compounds, species, commandRecorder);
            }

            // Eject compounds and build costs as corpse chunks of the cell
            SpawnCorpseChunks(ref organelleContainer, compounds, spawnSystem, worldSimulation, commandRecorder,
                position.Position, random, null, glucose);

            ref var soundPlayer = ref entity.Get<SoundEffectPlayer>();

            soundPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-death-2.ogg");

            return true;
        }

        private void ReleaseAllAgents(ref WorldPosition position, in Entity entity, CompoundBag compounds,
            Species species, EntityCommandRecorder recorder)
        {
            // To not completely deadlock in this there is a maximum limit
            int createdAgents = 0;

            var amount = compounds.GetCompoundAmount(oxytoxy);

            var props = new AgentProperties(species, oxytoxy);

            while (amount > Constants.MAXIMUM_AGENT_EMISSION_AMOUNT)
            {
                var direction = new Vector3(random.Next(0.0f, 1.0f) * 2 - 1,
                    0, random.Next(0.0f, 1.0f) * 2 - 1);

                var spawnedRecord = SpawnHelpers.SpawnAgentProjectileWithoutFinalizing(worldSimulation, recorder,
                    props, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT, Constants.EMITTED_AGENT_LIFETIME,
                    position.Position, direction, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT, entity);

                ModLoader.ModInterface.TriggerOnToxinEmitted(spawnedRecord);

                amount -= Constants.MAXIMUM_AGENT_EMISSION_AMOUNT;
                ++createdAgents;

                if (createdAgents >= Constants.MAX_EMITTED_AGENTS_ON_DEATH)
                    break;
            }
        }

        private void ApplyDeathVisuals(ref CellProperties cellProperties, ref OrganelleContainer organelleContainer,
            ref WorldPosition position, in Entity entity, EntityCommandRecorder recorder)
        {
            // Spawn cell death particles.
            float radius = 1;

            if (cellProperties.CreatedMembrane != null)
            {
                radius = cellProperties.CreatedMembrane.EncompassingCircleRadius;

                if (cellProperties.IsBacteria)
                    radius *= 0.5f;
            }

            SpawnHelpers.SpawnCellBurstEffectWithoutFinalizing(recorder, worldSimulation, position.Position, radius);

            // Mark visuals as needing an update to have visuals system re-process this
            // TODO: determine if it is necessary to re-implement the organelle hiding on death or if a fade animation
            // will be fine for those
            organelleContainer.OrganelleVisualsCreated = false;

            ref var shaderParameters = ref entity.Get<MicrobeShaderParameters>();

            shaderParameters.DissolveAnimationSpeed = Constants.MEMBRANE_DISSOLVE_SPEED;
            shaderParameters.PlayAnimations = true;
            shaderParameters.ParametersApplied = false;
        }
    }
}
