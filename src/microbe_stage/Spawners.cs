// This file contains all the different microbe stage spawner types
// just so that they are in one place.

using System;
using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using DefaultEcs.Command;
using Godot;

/// <summary>
///   Helpers for making different types of spawners
/// </summary>
public static class Spawners
{
    public static MicrobeSpawner MakeMicrobeSpawner(Species species)
    {
        return new MicrobeSpawner(species);
    }

    public static ChunkSpawner MakeChunkSpawner(ChunkConfiguration chunkType)
    {
        return new ChunkSpawner(chunkType);
    }

    public static CompoundCloudSpawner MakeCompoundSpawner(Compound compound,
        CompoundCloudSystem clouds, float amount)
    {
        return new CompoundCloudSpawner(compound, clouds, amount);
    }
}

/// <summary>
///   Helper functions for spawning various things
/// </summary>
public static class SpawnHelpers
{
    /// <summary>
    ///   Call this when using the "WithoutFinalizing" variants of spawn methods that allow additional entity
    ///   customization. This is not mandatory to call when other operations are to be batched with the same recorder.
    ///   In which case the recorder should be just directly submitted to
    ///   <see cref="IWorldSimulation.FinishRecordingEntityCommands"/>.
    /// </summary>
    /// <param name="entityRecorder">The entityRecorder returned from the without finalize method</param>
    /// <param name="worldSimulation">The world simulation used to start the entity spawn</param>
    public static void FinalizeEntitySpawn(EntityCommandRecorder entityRecorder, IWorldSimulation worldSimulation)
    {
        worldSimulation.FinishRecordingEntityCommands(entityRecorder);
    }

    public static void SpawnCellBurstEffect(IWorldSimulation worldSimulation, Vector3 location, float radius)
    {
        // Support spawning this at any time during an update cycle
        var recorder = worldSimulation.StartRecordingEntityCommands();

        SpawnCellBurstEffectWithoutFinalizing(recorder, worldSimulation, location, radius);
        worldSimulation.FinishRecordingEntityCommands(recorder);
    }

    public static void SpawnCellBurstEffectWithoutFinalizing(EntityCommandRecorder entityRecorder,
        IWorldSimulation worldSimulation, Vector3 location, float radius)
    {
        // Support spawning this at any time during an update cycle
        var entityCreator = worldSimulation.GetRecorderWorld(entityRecorder);

        var entity = worldSimulation.CreateEntityDeferred(entityCreator);

        entity.Set(new WorldPosition(location));

        entity.Set<SpatialInstance>();
        entity.Set(new PredefinedVisuals
        {
            VisualIdentifier = VisualResourceIdentifier.CellBurstEffect,
        });

        // The cell burst effect component initialization by its system configures this
        entity.Set<TimedLife>();
        entity.Set(new CellBurstEffect(radius));
    }

    /// <summary>
    ///   Spawns an agent projectile
    /// </summary>
    public static EntityRecord SpawnAgentProjectile(IWorldSimulation worldSimulation, AgentProperties properties,
        float amount, float lifetime, Vector3 location, Vector3 direction, float scale, Entity emitter)
    {
        var recorder = SpawnAgentProjectileWithoutFinalizing(worldSimulation, properties,
            amount, lifetime, location, direction, scale, emitter, out var entity);

        FinalizeEntitySpawn(recorder, worldSimulation);

        return entity;
    }

    public static EntityCommandRecorder SpawnAgentProjectileWithoutFinalizing(IWorldSimulation worldSimulation,
        AgentProperties properties, float amount, float lifetime, Vector3 location, Vector3 direction, float scale,
        Entity emitter, out EntityRecord entity)
    {
        var recorder = worldSimulation.StartRecordingEntityCommands();

        entity = SpawnAgentProjectileWithoutFinalizing(worldSimulation, recorder, properties, amount, lifetime,
            location, direction, scale, emitter);

        return recorder;
    }

    public static EntityRecord SpawnAgentProjectileWithoutFinalizing(IWorldSimulation worldSimulation,
        EntityCommandRecorder commandRecorder, AgentProperties properties, float amount, float lifetime,
        Vector3 location, Vector3 direction, float scale, Entity emitter)
    {
        var normalizedDirection = direction.Normalized();

        var entityCreator = worldSimulation.GetRecorderWorld(commandRecorder);

        var entity = worldSimulation.CreateEntityDeferred(entityCreator);

        entity.Set(new WorldPosition(location + direction * 1.5f));

        entity.Set(new PredefinedVisuals
        {
            VisualIdentifier = VisualResourceIdentifier.AgentProjectile,
        });

        entity.Set(new SpatialInstance
        {
            VisualScale = new Vector3(scale, scale, scale),
            ApplyVisualScale = Math.Abs(scale - 1) > MathUtils.EPSILON,
        });

        entity.Set(new TimedLife
        {
            TimeToLiveRemaining = lifetime,
        });
        entity.Set(new FadeOutActions
        {
            FadeTime = Constants.EMITTER_DESPAWN_DELAY,
            DisableCollisions = true,
            RemoveVelocity = true,
            DisableParticles = true,
        });

        entity.Set(new ToxinDamageSource
        {
            ToxinAmount = amount,
            ToxinProperties = properties,
        });

        entity.Set(new Physics
        {
            Velocity = normalizedDirection * Constants.AGENT_EMISSION_VELOCITY,
            AxisLock = Physics.AxisLockType.YAxisWithRotation,
        });

        // Need to specify shape like this to make saving work
        entity.Set(new SimpleShapeCreator(SimpleShapeType.Sphere, Constants.TOXIN_PROJECTILE_PHYSICS_SIZE,
            Constants.TOXIN_PROJECTILE_PHYSICS_DENSITY));

        entity.Set<PhysicsShapeHolder>();
        entity.Set(new CollisionManagement
        {
            IgnoredCollisionsWith = new List<Entity> { emitter },

            // Callbacks are initialized by ToxinCollisionSystem
        });

        // Needed for fade actions
        entity.Set<ManualPhysicsControl>();

        entity.Set(new ReadableName(properties.Name));

        return entity;
    }

    /// <summary>
    ///   Spawn a floating chunk (cell parts floating around, rocks, hazards)
    /// </summary>
    public static void SpawnChunk(IWorldSimulation worldSimulation, ChunkConfiguration chunkType, Vector3 location,
        Random random, bool microbeDrop)
    {
        var recorder = SpawnChunkWithoutFinalizing(worldSimulation, chunkType, location, random, microbeDrop, out _);

        FinalizeEntitySpawn(recorder, worldSimulation);
    }

    public static EntityCommandRecorder SpawnChunkWithoutFinalizing(IWorldSimulation worldSimulation,
        ChunkConfiguration chunkType, Vector3 location, Random random, bool microbeDrop, out EntityRecord entity)
    {
        var recorder = worldSimulation.StartRecordingEntityCommands();

        entity = SpawnChunkWithoutFinalizing(worldSimulation, recorder, chunkType, location, random, microbeDrop,
            Vector3.Zero);
        return recorder;
    }

    public static EntityRecord SpawnChunkWithoutFinalizing(IWorldSimulation worldSimulation,
        EntityCommandRecorder commandRecorder, ChunkConfiguration chunkType, Vector3 location, Random random,
        bool microbeDrop, Vector3 initialVelocity)
    {
        // Resolve the final chunk settings as the chunk configuration is a group of potential things
        var selectedMesh = chunkType.Meshes.Random(random);

        // Chunk is spawned with random rotation (in the 2D plane if it's an Easter egg)
        var rotationAxis = chunkType.EasterEgg ? new Vector3(0, 1, 0) : new Vector3(0, 1, 1);

        var entityCreator = worldSimulation.GetRecorderWorld(commandRecorder);

        var entity = worldSimulation.CreateEntityDeferred(entityCreator);

        entity.Set(new WorldPosition(location,
            new Quat(rotationAxis.Normalized(), 2 * Mathf.Pi * (float)random.NextDouble())));

        // TODO: redo chunk visuals with the loadable visual definitions
        // entity.Set(new PredefinedVisuals
        // {
        //     VisualIdentifier = VisualResourceIdentifier.AgentProjectile,
        // });
        entity.Set(new PathLoadedSceneVisuals
        {
            ScenePath = selectedMesh.ScenePath,
        });

        entity.Set(new SpatialInstance
        {
            VisualScale = new Vector3(chunkType.ChunkScale, chunkType.ChunkScale, chunkType.ChunkScale),
            ApplyVisualScale = Math.Abs(chunkType.ChunkScale - 1) > MathUtils.EPSILON,
        });

        bool hasMicrobeShaderParameters = false;

        // This needs to be skipped for particle type chunks (as they don't have materials)
        if (!selectedMesh.IsParticles && !selectedMesh.MissingDefaultShaderSupport)
        {
            entity.Set(new EntityMaterial
            {
                AutoRetrieveFromSpatial = true,
                AutoRetrieveModelPath = selectedMesh.SceneModelPath,
            });

            entity.Set<MicrobeShaderParameters>();
            hasMicrobeShaderParameters = true;
        }

        if (!string.IsNullOrEmpty(selectedMesh.SceneAnimationPath))
        {
            // Stop any animations from playing on this organelle when it is dropped as a chunk. Some chunk types do
            // want to keep playing an animation so there's this extra if
            if (!selectedMesh.PlayAnimation)
            {
                entity.Set(new AnimationControl
                {
                    AnimationPlayerPath = selectedMesh.SceneAnimationPath,
                    StopPlaying = true,
                });
            }
        }

        // Setup compounds to vent
        bool hasCompounds = false;
        if (chunkType.Compounds?.Count > 0)
        {
            // Capacity is 0 to disallow adding any more compounds to the compound bag
            var compounds = new CompoundBag(0);

            foreach (var entry in chunkType.Compounds)
            {
                // Directly write compounds to avoid the capacity limit
                compounds.Compounds.Add(entry.Key, entry.Value.Amount);
            }

#if DEBUG
            var toCheck = chunkType.Compounds.First();

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (compounds.GetCompoundAmount(toCheck.Key) != toCheck.Value.Amount)
                throw new Exception("Chunk compound adding failed");
#endif

            entity.Set(new CompoundStorage
            {
                Compounds = compounds,
            });

            hasCompounds = true;

            entity.Set(new CompoundVenter
            {
                VentEachCompoundPerSecond = chunkType.VentAmount,
                DestroyOnEmpty = chunkType.Dissolves,
                UsesMicrobialDissolveEffect = hasMicrobeShaderParameters,
            });
        }

        // Chunks that don't dissolve naturally when running out of compounds, are despawned with a timer
        // TODO: should this be forced if this chunk has no compounds? (at least ice shards probably wouldn't like
        // this)
        if (!chunkType.Dissolves)
        {
            entity.Set(new TimedLife
            {
                TimeToLiveRemaining = Constants.DESPAWNING_CHUNK_LIFETIME,
            });
            entity.Set(new FadeOutActions
            {
                FadeTime = Constants.EMITTER_DESPAWN_DELAY,
                DisableCollisions = true,
                RemoveVelocity = true,
                DisableParticles = selectedMesh.IsParticles,
                UsesMicrobialDissolveEffect = true,
                VentCompounds = hasCompounds,
            });
        }

        entity.Set(new Physics
        {
            // Particles lock rotation to make sure they don't rotate on hit
            AxisLock = selectedMesh.IsParticles ? Physics.AxisLockType.YAxisWithRotation : Physics.AxisLockType.YAxis,
            LinearDamping = Constants.CHUNK_PHYSICS_DAMPING,
            Velocity = initialVelocity,
        });

        if (selectedMesh.ConvexShapePath == null)
        {
            entity.Set(new SimpleShapeCreator(SimpleShapeType.Sphere, chunkType.Radius,
                chunkType.PhysicsDensity));
        }
        else
        {
            entity.Set(new CollisionShapeLoader(selectedMesh.ConvexShapePath, chunkType.PhysicsDensity));
        }

        entity.Set<PhysicsShapeHolder>();

        // See the remarks comment on EntityRadiusInfo
        entity.Set(new EntityRadiusInfo(chunkType.Radius));

        entity.Set<CollisionManagement>();

        if (chunkType.Damages > 0)
        {
            entity.Set(new DamageOnTouch
            {
                DamageAmount = chunkType.Damages,
                DestroyOnTouch = chunkType.DeleteOnTouch,
                DamageType = string.IsNullOrEmpty(chunkType.DamageType) ? "chunk" : chunkType.DamageType,
            });
        }
        else if (chunkType.DeleteOnTouch)
        {
            // No damage but deletes on touch
            entity.Set(new DamageOnTouch
            {
                DamageAmount = 0,
                DestroyOnTouch = chunkType.DeleteOnTouch,
            });
        }

        // TODO: rename Size to EngulfSize after making sure it isn't used for other purposes
        if (chunkType.Size > 0)
        {
            entity.Set(new Engulfable
            {
                BaseEngulfSize = chunkType.Size,
                RequisiteEnzymeToDigest = !string.IsNullOrEmpty(chunkType.DissolverEnzyme) ?
                    SimulationParameters.Instance.GetEnzyme(chunkType.DissolverEnzyme) :
                    null,
                DestroyIfPartiallyDigested = true,
            });
        }

        entity.Set<CurrentAffected>();
        entity.Set<ManualPhysicsControl>();

        // Despawn chunks when there are too many
        entity.Set(new CountLimited
        {
            Group = microbeDrop ? LimitGroup.Chunk : LimitGroup.ChunkSpawned,
        });

        entity.Set(new ReadableName(new LocalizedString(chunkType.Name)));

        return entity;
    }

    public static void SpawnMicrobe(IWorldSimulation worldSimulation, Species species, Vector3 location,
        bool aiControlled, MulticellularSpawnState multicellularSpawnState = MulticellularSpawnState.Bud)
    {
        SpawnMicrobe(worldSimulation, species, location, aiControlled, (null, 0), multicellularSpawnState);
    }

    public static void SpawnMicrobe(IWorldSimulation worldSimulation, Species species, Vector3 location,
        bool aiControlled, (CellType? MulticellularCellType, int CellBodyPlanIndex) multicellularData,
        MulticellularSpawnState multicellularSpawnState = MulticellularSpawnState.Bud)
    {
        var (recorder, _) = SpawnMicrobeWithoutFinalizing(worldSimulation, species, location, aiControlled,
            multicellularData, out _, multicellularSpawnState);

        FinalizeEntitySpawn(recorder, worldSimulation);
    }

    public static (EntityCommandRecorder Recorder, float Weight) SpawnMicrobeWithoutFinalizing(
        IWorldSimulation worldSimulation, Species species,
        Vector3 location, bool aiControlled, (CellType? MulticellularCellType, int CellBodyPlanIndex) multicellularData,
        out EntityRecord entity,
        MulticellularSpawnState multicellularSpawnState = MulticellularSpawnState.Bud, Random? random = null)
    {
        var recorder = worldSimulation.StartRecordingEntityCommands();
        return (recorder, SpawnMicrobeWithoutFinalizing(worldSimulation, species, location, aiControlled,
            multicellularData, recorder, out entity, multicellularSpawnState, true, random));
    }

    public static float SpawnMicrobeWithoutFinalizing(IWorldSimulation worldSimulation, Species species,
        Vector3 location, bool aiControlled, (CellType? MulticellularCellType, int CellBodyPlanIndex) multicellularData,
        EntityCommandRecorder recorder, out EntityRecord entity,
        MulticellularSpawnState multicellularSpawnState = MulticellularSpawnState.Bud,
        bool giveInitialCompounds = true, Random? random = null)
    {
        // If this method is modified it must be ensured that CellPropertiesHelpers.ReApplyCellTypeProperties and
        // MicrobeVisualOnlySimulation microbe update methods are also up to date

        var entityCreator = worldSimulation.GetRecorderWorld(recorder);

        entity = worldSimulation.CreateEntityDeferred(entityCreator);

        // Position
        entity.Set(new WorldPosition(location, Quat.Identity));

        entity.Set(new SpeciesMember(species));

        // Player vs. AI controlled microbe components
        if (aiControlled)
        {
            entity.Set<MicrobeAI>();

            // Darwinian evolution statistic tracking (these are the external effects that are passed to auto-evo)
            entity.Set<SurvivalStatistics>();

            entity.Set(new SoundEffectPlayer
            {
                AbsoluteMaxDistanceSquared = Constants.MICROBE_SOUND_MAX_DISTANCE_SQUARED,
            });
        }
        else
        {
            // We assume that if the cell is not AI controlled it is the player's cell
            entity.Set<PlayerMarker>();

            // The player's "ears" are placed at the player microbe
            entity.Set(new SoundListener
            {
                UseTopDownRotation = true,
            });

            entity.Set(new SoundEffectPlayer
            {
                AbsoluteMaxDistanceSquared = Constants.MICROBE_SOUND_MAX_DISTANCE_SQUARED,

                // As this takes a bit of extra performance this is just set for the player
                AutoDetectPlayer = true,
            });
        }

        // Base species-based data initialization
        ICellDefinition usedCellDefinition;
        MembraneType membraneType;

        EarlyMulticellularSpecies? multicellular = null;

        if (species is EarlyMulticellularSpecies earlyMulticellularSpecies)
        {
            multicellular = earlyMulticellularSpecies;
            CellType resolvedCellType;

            if (multicellularData.MulticellularCellType != null)
            {
                // Non-first cell in an early multicellular colony
                if (multicellularData.CellBodyPlanIndex == 0)
                {
                    throw new ArgumentException(
                        "Multicellular cell type needs to be accompanied by the body plan index");
                }

                resolvedCellType = multicellularData.MulticellularCellType;

                usedCellDefinition = multicellularData.MulticellularCellType;
                var properties = new CellProperties(multicellularData.MulticellularCellType);
                membraneType = properties.MembraneType;
                entity.Set(properties);

                // TODO: should this also be given MulticellularGrowth to allow this to grow fully if the colony splits
            }
            else
            {
                if (multicellularData.CellBodyPlanIndex != 0)
                {
                    throw new ArgumentException("First Multicellular cell must have body plan index of 0");
                }

                resolvedCellType = earlyMulticellularSpecies.Cells[0].CellType;

                usedCellDefinition = resolvedCellType;
                var properties = new CellProperties(usedCellDefinition);
                membraneType = properties.MembraneType;
                entity.Set(properties);

                entity.Set(new MulticellularGrowth(earlyMulticellularSpecies));
            }

#if DEBUG
            if (multicellularData.CellBodyPlanIndex >= earlyMulticellularSpecies.Cells.Count)
                throw new InvalidOperationException("Bad body plan index was generated for a cell");
#endif

            entity.Set(new EarlyMulticellularSpeciesMember(earlyMulticellularSpecies, resolvedCellType,
                multicellularData.CellBodyPlanIndex));
        }
        else if (species is MicrobeSpecies microbeSpecies)
        {
            entity.Set(new MicrobeSpeciesMember
            {
                Species = microbeSpecies,
            });

            usedCellDefinition = microbeSpecies;
            var properties = new CellProperties(microbeSpecies);
            membraneType = properties.MembraneType;
            entity.Set(properties);

            if (multicellularData.MulticellularCellType != null)
                GD.PrintErr("Multicellular cell type may not be set when spawning a MicrobeSpecies instance");
        }
        else
        {
            throw new NotSupportedException("Unknown species type to spawn a microbe from");
        }

        var bioProcesses = new BioProcesses
        {
            ProcessStatistics = aiControlled ? null : new ProcessStatistics(),
        };

        int organelleCount;

        // Initialize organelles for the cell type
        {
            var container = default(OrganelleContainer);
            container.CreateOrganelleLayout(usedCellDefinition);
            container.RecalculateOrganelleBioProcesses(ref bioProcesses);

            organelleCount = container.Organelles!.Count;

            // Compound storage
            var storage = new CompoundStorage
            {
                // 0 is used here as this is updated before adding the component anyway
                Compounds = new CompoundBag(0),
            };

            // Run the storage update logic for the first time (to ensure consistency with later updates)
            // This has to be called as CreateOrganelleLayout doesn't do this automatically
            container.UpdateCompoundBagStorageFromOrganelles(ref storage);

            var engulfable = new Engulfable
            {
                RequisiteEnzymeToDigest = SimulationParameters.Instance.GetEnzyme(membraneType.DissolverEnzyme),
            };

            var engulfer = default(Engulfer);

            container.UpdateEngulfingSizeData(ref engulfer, ref engulfable, usedCellDefinition.IsBacteria);

            entity.Set(engulfable);
            entity.Set(engulfer);

            // Finish setting up related components
            entity.Set(container);

            if (giveInitialCompounds)
                storage.Compounds.AddInitialCompounds(species.InitialCompounds);

            entity.Set(storage);
        }

        entity.Set(bioProcesses);

        entity.Set(new ReproductionStatus(species.BaseReproductionCost));

        // Visuals
        var scale = usedCellDefinition.IsBacteria ? new Vector3(0.5f, 0.5f, 0.5f) : new Vector3(1, 1, 1);

        entity.Set(new SpatialInstance
        {
            VisualScale = scale,
            ApplyVisualScale = true,
        });

        entity.Set(new RenderPriorityOverride(Constants.MICROBE_DEFAULT_RENDER_PRIORITY));

        entity.Set<EntityMaterial>();

        entity.Set(new ColourAnimation(Membrane.MembraneTintFromSpeciesColour(usedCellDefinition.Colour))
        {
            AnimateOnlyFirstMaterial = true,
        });

        entity.Set<MicrobeShaderParameters>();

        entity.Set(new CompoundAbsorber
        {
            // This gets set properly later once the membrane is ready by MicrobePhysicsCreationAndSizeSystem
            AbsorbRadius = Constants.MICROBE_MIN_ABSORB_RADIUS,

            // Microbes only want to grab stuff they want
            OnlyAbsorbUseful = true,

            AbsorptionRatio = usedCellDefinition.MembraneType.ResourceAbsorptionFactor,

            // AI requires this, player doesn't (or at least I can't remember right now that it would -hhyyrylainen)
            // but it isn't too big a problem to also specify this for the player
            TotalAbsorbedCompounds = new Dictionary<Compound, float>(),
        });

        entity.Set(new UnneededCompoundVenter
        {
            VentThreshold = Constants.DEFAULT_MICROBE_VENT_THRESHOLD,
        });

        // Physics
        entity.Set(PhysicsHelpers.CreatePhysicsForMicrobe());

        entity.Set<MicrobePhysicsExtraData>();

        // Used in certain damage types to apply a cooldown
        entity.Set<DamageCooldown>();

        entity.Set(new CollisionManagement
        {
            RecordActiveCollisions = Constants.MAX_SIMULTANEOUS_COLLISIONS_SMALL,
        });

        // The shape is created in the background (by MicrobePhysicsCreationAndSizeSystem) to reduce lag when
        // something spawns
        entity.Set<PhysicsShapeHolder>();

        // Movement
        entity.Set(new MicrobeControl(location));
        entity.Set<ManualPhysicsControl>();

        // Other cell features
        entity.Set(new MicrobeStatus
        {
            TimeUntilChemoreceptionUpdate = Constants.CHEMORECEPTOR_SEARCH_UPDATE_INTERVAL,
        });

        entity.Set(new Health(HealthHelpers.CalculateMicrobeHealth(usedCellDefinition.MembraneType,
            usedCellDefinition.MembraneRigidity)));

        entity.Set(new CommandSignaler
        {
            SignalingChannel = species.ID,
        });

        // Microbes are not affected by currents before they are visualized
        // entity.Set<CurrentAffected>();

        // Selecting is used to throw out specific colony members
        entity.Set<Selectable>();

        entity.Set(new ReadableName(new LocalizedString(species.FormattedName)));

        float spawnLimitWeight = OrganelleContainerHelpers.CalculateCellEntityWeight(organelleCount);

        if (multicellularSpawnState != MulticellularSpawnState.Bud && multicellular != null)
        {
            switch (multicellularSpawnState)
            {
                case MulticellularSpawnState.FullColony:
                    spawnLimitWeight +=
                        MicrobeColonyHelpers.SpawnAsFullyGrownMulticellularColony(entity, multicellular,
                            spawnLimitWeight);
                    break;

                case MulticellularSpawnState.ChanceForFullColony:
                {
                    random ??= new Random();

                    // Chance to spawn fully grown or partially grown
                    if (random.NextDouble() < Constants.CHANCE_MULTICELLULAR_SPAWNS_GROWN)
                    {
                        spawnLimitWeight += MicrobeColonyHelpers.SpawnAsFullyGrownMulticellularColony(entity,
                            multicellular, spawnLimitWeight);
                    }
                    else if (random.NextDouble() < Constants.CHANCE_MULTICELLULAR_SPAWNS_PARTLY_GROWN)
                    {
                        // -1 here as the bud is always spawned so the number of cells to add on top of that is the max
                        // count
                        var maxCount = multicellular.Cells.Count - 1;
                        int cellsToAdd = 0;

                        while (cellsToAdd < maxCount)
                        {
                            ++cellsToAdd;

                            if (random.NextDouble() > Constants.CHANCE_MULTICELLULAR_PARTLY_GROWN_CELL_CHANCE)
                                break;
                        }

                        if (cellsToAdd > 0)
                        {
                            spawnLimitWeight += MicrobeColonyHelpers.SpawnAsPartialMulticellularColony(entity,
                                spawnLimitWeight, cellsToAdd);
                        }
                    }

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(multicellularSpawnState), multicellularSpawnState,
                        null);
            }
        }

        return spawnLimitWeight;
    }

    /// <summary>
    ///   Calculates spaced out positions to spawn a bacteria swarm (to avoid them all overlapping)
    /// </summary>
    public static List<Vector3>? CalculateBacteriaSwarmPositions(Vector3 initialLocation, MicrobeSpecies species,
        Random random)
    {
        // +1 is used here as Next has a non-exclusive upper bound
        int maxSize = Constants.MAX_BACTERIAL_SWARM_SIZE + 1;

        if (species.BaseHexSize >= Constants.FURTHER_REDUCE_BACTERIAL_SWARM_AFTER_HEX_COUNT)
        {
            // This reduction by 2 gets the max swarm size spawn back to what it was in 0.6.3
            maxSize -= 2;
        }
        else if (species.BaseHexSize >= Constants.REDUCE_BACTERIAL_SWARM_AFTER_HEX_COUNT)
        {
            maxSize -= 1;
        }

        var clumpSize = random.Next(Constants.MIN_BACTERIAL_SWARM_SIZE, maxSize);

        if (clumpSize <= 0)
            return null;

        var result = new List<Vector3>(clumpSize);

        var currentPoint = new Vector3(random.Next(1, 8), 0, random.Next(1, 8));

        for (int i = 0; i < clumpSize; i++)
        {
            result.Add(initialLocation + currentPoint);

            currentPoint += new Vector3(random.Next(-7, 8), 0, random.Next(-7, 8));
        }

        return result;
    }

    public static (EntityCommandRecorder Recorder, float Weight) SpawnBacteriaSwarmMember(
        IWorldSimulation worldSimulation, Species species,
        Vector3 location, out EntityRecord entity)
    {
        return SpawnMicrobeWithoutFinalizing(worldSimulation, species, location, true, (null, 0), out entity,
            MulticellularSpawnState.Bud);
    }

    public static void SpawnCloud(CompoundCloudSystem clouds, Vector3 location, Compound compound, float amount,
        Random random)
    {
        int resolution = Settings.Instance.CloudResolution;

        // Randomise amount of compound in the cloud a bit
        amount *= random.Next(0.5f, 1);

        // This spreads out the cloud spawn a bit
        clouds.AddCloud(compound, amount, location + new Vector3(0 + resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0 - resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 + resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 - resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0));
    }

    public static MulticellularCreature SpawnCreature(Species species, Vector3 location,
        Node worldRoot, PackedScene multicellularScene, bool aiControlled, ISpawnSystem spawnSystem,
        GameProperties currentGame)
    {
        var creature = (MulticellularCreature)multicellularScene.Instance();

        // The second parameter is (isPlayer), and we assume that if the
        // cell is not AI controlled it is the player's cell
        creature.Init(spawnSystem, currentGame, !aiControlled);

        worldRoot.AddChild(creature);
        creature.Translation = location;

        creature.AddToGroup(Constants.ENTITY_TAG_CREATURE);
        creature.AddToGroup(Constants.PROGRESS_ENTITY_GROUP);

        if (aiControlled)
        {
            // TODO: AI
        }

        creature.ApplySpecies(species);
        creature.ApplyMovementModeFromSpecies();

        creature.SetInitialCompounds();
        return creature;
    }

    public static PackedScene LoadMulticellularScene()
    {
        return GD.Load<PackedScene>("res://src/late_multicellular_stage/MulticellularCreature.tscn");
    }

    public static ResourceEntity SpawnResourceEntity(WorldResource resourceType, Transform location, Node worldNode,
        PackedScene entityScene, bool randomizeRotation = false, Random? random = null)
    {
        var resourceEntity = CreateHarvestedResourceEntity(resourceType, entityScene, false);

        if (randomizeRotation)
        {
            random ??= new Random();

            // Randomize rotation by constructing a new Transform that has the basis rotated, note that this loses the
            // scale, but entities shouldn't anyway be allowed to have a root node scale
            location = new Transform(new Basis(location.basis.Quat() * RandomRotationForResourceEntity(random)),
                location.origin);
        }

        worldNode.AddChild(resourceEntity);

        resourceEntity.Transform = location;

        return resourceEntity;
    }

    /// <summary>
    ///   Creates a resource entity to be placed in the world later. Used for example to create items to drop.
    /// </summary>
    /// <returns>The entity ready to be placed in the world</returns>
    public static ResourceEntity CreateHarvestedResourceEntity(WorldResource resourceType, PackedScene entityScene,
        bool randomizeRotation = true, Random? random = null)
    {
        var resourceEntity = (ResourceEntity)entityScene.Instance();

        // Apply settings
        resourceEntity.SetResource(resourceType);

        if (randomizeRotation)
        {
            random ??= new Random();

            resourceEntity.Transform = new Transform(new Basis(RandomRotationForResourceEntity(random)), Vector3.Zero);
        }

        resourceEntity.AddToGroup(Constants.INTERACTABLE_GROUP);
        return resourceEntity;
    }

    public static PackedScene LoadResourceEntityScene()
    {
        return GD.Load<PackedScene>("res://src/awakening_stage/ResourceEntity.tscn");
    }

    public static IInteractableEntity CreateEquipmentEntity(EquipmentDefinition equipmentDefinition)
    {
        var entity = new Equipment(equipmentDefinition);

        entity.AddToGroup(Constants.INTERACTABLE_GROUP);
        return entity;
    }

    public static PlacedStructure SpawnStructure(StructureDefinition structureDefinition, Transform location,
        Node worldNode, PackedScene entityScene)
    {
        var structureEntity = entityScene.Instance<PlacedStructure>();

        worldNode.AddChild(structureEntity);
        structureEntity.Init(structureDefinition);

        structureEntity.AddToGroup(Constants.INTERACTABLE_GROUP);
        structureEntity.AddToGroup(Constants.STRUCTURE_ENTITY_GROUP);

        structureEntity.Transform = location;

        return structureEntity;
    }

    public static PackedScene LoadStructureScene()
    {
        return GD.Load<PackedScene>("res://src/awakening_stage/PlacedStructure.tscn");
    }

    public static SocietyCreature SpawnCitizen(Species species, Vector3 location, Node worldRoot,
        PackedScene citizenScene)
    {
        var creature = (SocietyCreature)citizenScene.Instance();

        creature.Init();

        worldRoot.AddChild(creature);
        creature.Translation = location;

        creature.AddToGroup(Constants.CITIZEN_GROUP);

        creature.ApplySpecies(species);

        return creature;
    }

    public static PackedScene LoadCitizenScene()
    {
        return GD.Load<PackedScene>("res://src/society_stage/SocietyCreature.tscn");
    }

    public static PlacedCity SpawnCity(Transform location, Node worldRoot, PackedScene cityScene, bool playerCity,
        TechWeb availableTechnology)
    {
        var city = (PlacedCity)cityScene.Instance();

        city.Init(playerCity, availableTechnology);

        worldRoot.AddChild(city);
        city.Transform = location;

        city.AddToGroup(Constants.CITY_ENTITY_GROUP);
        city.AddToGroup(Constants.NAME_LABEL_GROUP);

        return city;
    }

    public static PackedScene LoadCityScene()
    {
        return GD.Load<PackedScene>("res://src/industrial_stage/PlacedCity.tscn");
    }

    public static PlacedPlanet SpawnPlanet(Transform location, Node worldRoot, PackedScene planetScene,
        bool playerPlanet,
        TechWeb availableTechnology)
    {
        var planet = (PlacedPlanet)planetScene.Instance();

        planet.Init(playerPlanet, availableTechnology);

        worldRoot.AddChild(planet);
        planet.Transform = location;

        planet.AddToGroup(Constants.PLANET_ENTITY_GROUP);
        planet.AddToGroup(Constants.NAME_LABEL_GROUP);

        return planet;
    }

    public static PackedScene LoadPlanetScene()
    {
        return GD.Load<PackedScene>("res://src/space_stage/PlacedPlanet.tscn");
    }

    public static SpaceFleet SpawnFleet(Transform location, Node worldRoot, PackedScene fleetScene,
        bool playerFleet, UnitType initialShip)
    {
        var fleet = (SpaceFleet)fleetScene.Instance();

        fleet.Init(initialShip, playerFleet);

        worldRoot.AddChild(fleet);
        fleet.Transform = location;

        fleet.AddToGroup(Constants.SPACE_FLEET_ENTITY_GROUP);
        fleet.AddToGroup(Constants.NAME_LABEL_GROUP);

        return fleet;
    }

    public static PlacedSpaceStructure SpawnSpaceStructure(SpaceStructureDefinition structureDefinition,
        Transform location, Node worldNode, PackedScene structureScene, bool playerOwned)
    {
        var structureEntity = structureScene.Instance<PlacedSpaceStructure>();

        worldNode.AddChild(structureEntity);
        structureEntity.Init(structureDefinition, playerOwned);

        structureEntity.AddToGroup(Constants.NAME_LABEL_GROUP);
        structureEntity.AddToGroup(Constants.SPACE_STRUCTURE_ENTITY_GROUP);

        structureEntity.Transform = location;

        return structureEntity;
    }

    public static PackedScene LoadSpaceStructureScene()
    {
        return GD.Load<PackedScene>("res://src/space_stage/PlacedSpaceStructure.tscn");
    }

    public static PackedScene LoadFleetScene()
    {
        return GD.Load<PackedScene>("res://src/space_stage/SpaceFleet.tscn");
    }

    private static Quat RandomRotationForResourceEntity(Random random)
    {
        return new Quat(new Vector3(random.NextFloat() + 0.01f, random.NextFloat(), random.NextFloat()).Normalized(),
            random.NextFloat() * Mathf.Pi + 0.01f);
    }
}

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner : Spawner
{
    private readonly Random random = new();

    public MicrobeSpawner(Species species)
    {
        Species = species ?? throw new ArgumentException("species is null");
    }

    public override bool SpawnsEntities => true;

    public Species Species { get; }

    public override SpawnQueue Spawn(IWorldSimulation worldSimulation, Vector3 location, ISpawnSystem spawnSystem)
    {
        // This should no longer happen, but let's keep this print here to keep track of the situation
        if (Species.Obsolete)
            GD.PrintErr("Obsolete species microbe has spawned");

        var microbeSpecies = Species as MicrobeSpecies;

        bool bacteria = false;

        if (microbeSpecies != null)
            bacteria = microbeSpecies.IsBacteria;

        var firstSpawn = new SingleItemSpawnQueue((out EntityRecord entity) =>
        {
            // The true here is that this is AI controlled
            var (recorder, weight) = SpawnHelpers.SpawnMicrobeWithoutFinalizing(worldSimulation, Species,
                location, true, (null, 0), out entity, MulticellularSpawnState.ChanceForFullColony);

            ModLoader.ModInterface.TriggerOnMicrobeSpawned(entity);

            return (recorder, weight);
        }, this);

        if (!bacteria)
        {
            // Simple case of just spawning a single microbe
            return firstSpawn;
        }

        if (microbeSpecies == null)
            throw new Exception("Logic error in microbe species not being set");

        // More complex, first need to do a normal spawn, and then continue onto bacteria swarm ones so we use a
        // combined queue specifically written for this use case

        var stateData = SpawnHelpers.CalculateBacteriaSwarmPositions(location, microbeSpecies, random);

        // No swarm wants to spawn
        if (stateData == null)
            return firstSpawn;

        var swarmQueue = new CallbackSpawnQueue<List<Vector3>>((List<Vector3> positions, out EntityRecord entity) =>
        {
            var (recorder, weight) = SpawnHelpers.SpawnBacteriaSwarmMember(worldSimulation, Species,
                positions[0], out entity);

            positions.RemoveAt(0);

            return (recorder, weight, positions.Count < 1);
        }, stateData, SpawnQueue.PruneSpawnListPositions, this);

        return new CombinedSpawnQueue(firstSpawn, swarmQueue);
    }

    public override string ToString()
    {
        return $"MicrobeSpawner for {Species}";
    }
}

/// <summary>
///   Spawns compound clouds of a certain type
/// </summary>
public class CompoundCloudSpawner : Spawner
{
    private readonly Compound compound;
    private readonly CompoundCloudSystem clouds;
    private readonly float amount;
    private readonly Random random = new();

    public CompoundCloudSpawner(Compound compound, CompoundCloudSystem clouds, float amount)
    {
        this.compound = compound ?? throw new ArgumentException("compound is null");
        this.clouds = clouds ?? throw new ArgumentException("clouds is null");
        this.amount = amount;
    }

    public override bool SpawnsEntities => false;

    public override SpawnQueue? Spawn(IWorldSimulation worldSimulation, Vector3 location, ISpawnSystem spawnSystem)
    {
        SpawnHelpers.SpawnCloud(clouds, location, compound, amount, random);

        // We don't spawn entities
        return null;
    }

    public override string ToString()
    {
        return $"CloudSpawner for {compound}";
    }
}

/// <summary>
///   Spawns chunks of a specific type
/// </summary>
public class ChunkSpawner : Spawner
{
    private readonly ChunkConfiguration chunkType;
    private readonly Random random = new();

    public ChunkSpawner(ChunkConfiguration chunkType)
    {
        this.chunkType = chunkType;
    }

    public override bool SpawnsEntities => true;

    public override SpawnQueue Spawn(IWorldSimulation worldSimulation, Vector3 location, ISpawnSystem spawnSystem)
    {
        return new SingleItemSpawnQueue((out EntityRecord entity) =>
        {
            var recorder = SpawnHelpers.SpawnChunkWithoutFinalizing(worldSimulation,
                chunkType, location, random, false, out entity);

            ModLoader.ModInterface.TriggerOnChunkSpawned(entity, true);

            return (recorder, Constants.FLOATING_CHUNK_ENTITY_WEIGHT);
        }, this);
    }

    public override string ToString()
    {
        return $"ChunkSpawner for {chunkType.Name}";
    }
}
