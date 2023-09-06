namespace Systems
{
    using System;
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
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
    [RunsAfter(typeof(OsmoregulationAndHealingSystem))]
    public sealed class MicrobeDeathSystem : AEntitySetSystem<float>
    {
        public MicrobeDeathSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
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
                HandleMicrobeDeath(entity);
                health.DeathProcessed = true;
            }
        }

        private void HandleMicrobeDeath(in Entity entity)
        {
            if (entity.Has<AttachedToEntity>())
            {
                // TODO: handle either dying in engulfment or in a cell colony
                // When in a colony needs to detach

                // Dropping corpse chunks won't make sense while inside a cell
                throw new NotImplementedException();
            }

            if (entity.Has<MicrobeColony>())
            {
                // TODO: handle colony lead cell dying (disband the colony)
                throw new NotImplementedException();
            }

            // TODO: release all engulfed objects (unless this is being currently engulfed in which case our engulfer
            // should take them)

            ModLoader.ModInterface.TriggerOnMicrobeDied(entity, entity.Has<PlayerMarker>());

            // TODO: the kill method used to return the list of dropped corpse chunks, not sure if that is still
            // needed by anything
            OnKilled(entity);
        }

        /// <summary>
        ///   Operations that should be done when this cell is killed
        /// </summary>
        /// <returns>
        ///   The dropped corpse chunks.
        /// </returns>
        private void /*IEnumerable<Entity>*/ OnKilled(in Entity entity)
        {
            // Reset some stuff
            State = MicrobeState.Normal;
            MovementDirection = new Vector3(0, 0, 0);
            LinearVelocity = new Vector3(0, 0, 0);
            allOrganellesDivided = false;

            if (entity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None)
            {
                // When dying when engulfed the normal actions don't apply
                return;
            }

            ApplyDeathVisuals(entity);

            // Eject all of the engulfables here
            foreach (var engulfed in engulfedObjects.ToList())
            {
                if (engulfed.Object.Value != null)
                    EjectEngulfable(engulfed.Object.Value);
            }

            // Releasing all the agents.
            // To not completely deadlock in this there is a maximum limit
            int createdAgents = 0;

            if (AgentVacuoleCount > 0)
            {
                var oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

                var amount = Compounds.GetCompoundAmount(oxytoxy);

                var props = new AgentProperties(Species, oxytoxy);

                throw new NotImplementedException();

                // var agentScene = SpawnHelpers.LoadAgentScene();

                while (amount > Constants.MAXIMUM_AGENT_EMISSION_AMOUNT)
                {
                    var direction = new Vector3(random.Next(0.0f, 1.0f) * 2 - 1,
                        0, random.Next(0.0f, 1.0f) * 2 - 1);

                    // var agent = SpawnHelpers.SpawnAgent(props, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT,
                    //     Constants.EMITTED_AGENT_LIFETIME,
                    //     Translation, direction, GetStageAsParent(),
                    //     agentScene, this);
                    //
                    // ModLoader.ModInterface.TriggerOnToxinEmitted(agent);

                    amount -= Constants.MAXIMUM_AGENT_EMISSION_AMOUNT;
                    ++createdAgents;

                    if (createdAgents >= Constants.MAX_EMITTED_AGENTS_ON_DEATH)
                        break;
                }
            }

            // Eject the compounds that was in the microbe
            var compoundsToRelease = new Dictionary<Compound, float>();

            foreach (var type in SimulationParameters.Instance.GetCloudCompounds())
            {
                var amount = Compounds.GetCompoundAmount(type) *
                    Constants.COMPOUND_RELEASE_FRACTION;

                compoundsToRelease[type] = amount;
            }

            // Eject some part of the build cost of all the organelles
            foreach (var organelle in organelles!)
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

            CalculateBonusDigestibleGlucose(compoundsToRelease);

            // Queues either 1 corpse chunk or a factor of the hexes
            int chunksToSpawn = Math.Max(1, HexCount / Constants.CORPSE_CHUNK_DIVISOR);

            throw new NotImplementedException();

            // var droppedCorpseChunks = new HashSet<FloatingChunk>(chunksToSpawn);
            //
            // var chunkScene = SpawnHelpers.LoadChunkScene();

            // An enumerator to step through all available organelles in a random order when making chunks
            using var organellesAvailableEnumerator = organelles.OrderBy(_ => random.Next()).GetEnumerator();

            // The default model for chunks is the cytoplasm model in case there isn't a model left in the species
            var defaultChunkScene = SimulationParameters.Instance
                    .GetOrganelleType(Constants.DEFAULT_CHUNK_MODEL_NAME).LoadedCorpseChunkScene ??
                throw new Exception("No default chunk scene");

            for (int i = 0; i < chunksToSpawn; ++i)
            {
                // Amount of compound in one chunk
                float amount = HexCount / Constants.CORPSE_CHUNK_AMOUNT_DIVISOR;

                var positionAdded = new Vector3(random.Next(-2.0f, 2.0f), 0,
                    random.Next(-2.0f, 2.0f));

                var chunkType = new ChunkConfiguration
                {
                    ChunkScale = 1.0f,
                    Dissolves = true,
                    Mass = 1.0f,
                    Radius = 1.0f,
                    Size = 3.0f,
                    VentAmount = 0.1f,

                    // Add compounds
                    Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>(),
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

                throw new NotImplementedException();
                /*var sceneToUse = new ChunkConfiguration.ChunkScene
                {
                    LoadedScene = defaultChunkScene,
                };

                // Will only loop if there are still organelles available
                while (organellesAvailableEnumerator.MoveNext() && organellesAvailableEnumerator.Current != null)
                {
                    if (!string.IsNullOrEmpty(organellesAvailableEnumerator.Current.Definition.CorpseChunkScene))
                    {
                        sceneToUse.LoadedScene =
                            organellesAvailableEnumerator.Current.Definition.LoadedCorpseChunkScene;
                    }
                    else if (!string.IsNullOrEmpty(organellesAvailableEnumerator.Current.Definition.DisplayScene))
                    {
                        sceneToUse.LoadedScene = organellesAvailableEnumerator.Current.Definition.LoadedScene;
                        sceneToUse.SceneModelPath =
                            organellesAvailableEnumerator.Current.Definition.DisplaySceneModelPath;
                    }
                    else
                    {
                        continue;
                    }

                    if (sceneToUse.LoadedScene != null)
                        break;
                }

                if (sceneToUse.LoadedScene == null)
                    throw new Exception("loaded scene is null");

                chunkType.Meshes.Add(sceneToUse);

                // Finally spawn a chunk with the settings
                var chunk = SpawnHelpers.SpawnChunk(chunkType, Translation + positionAdded, GetStageAsParent(),
                    chunkScene, random);
                droppedCorpseChunks.Add(chunk);

                // Add to the spawn system to make these chunks limit possible number of entities
                spawnSystem!.NotifyExternalEntitySpawned(chunk);

                ModLoader.ModInterface.TriggerOnChunkSpawned(chunk, false);*/
            }

            // Subtract population
            if (!IsPlayerMicrobe && !Species.PlayerSpecies)
            {
                GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
                    Constants.CREATURE_DEATH_POPULATION_LOSS, TranslationServer.Translate("DEATH"));
            }

            if (IsPlayerMicrobe)
            {
                // If you died before entering the editor disable that
                OnReproductionStatus?.Invoke(this, false);
            }

            if (IsPlayerMicrobe)
            {
                // Playing from a positional audio player won't have any effect since the listener is
                // directly on it.
                PlayNonPositionalSoundEffect("res://assets/sounds/soundeffects/microbe-death-2.ogg", 0.5f);
            }
            else
            {
                PlaySoundEffect("res://assets/sounds/soundeffects/microbe-death-2.ogg");
            }

            // Disable collisions
            CollisionLayer = 0;
            CollisionMask = 0;

            throw new NotImplementedException();

            // return droppedCorpseChunks;
        }

        private void ApplyDeathVisuals(in Entity entity)
        {
            // Spawn cell death particles.


                throw new NotImplementedException();

                // var cellBurstEffectParticles = (CellBurstEffect)cellBurstEffectScene.Instance();
                // cellBurstEffectParticles.Translation = Translation;
                // cellBurstEffectParticles.Radius = Radius;
                // cellBurstEffectParticles.AddToGroup(Constants.TIMED_GROUP);
                //
                // GetParent().AddChild(cellBurstEffectParticles);



            foreach (var organelle in organelles!)
            {
                organelle.Hide();
            }


            throw new NotImplementedException();

            // Membrane.DissolveEffectValue += delta * Constants.MEMBRANE_DISSOLVE_SPEED;
            //
            // if (Membrane.DissolveEffectValue >= 1)
            // {
            //     this.DestroyDetachAndQueueFree();
            // }
        }
    }
}
