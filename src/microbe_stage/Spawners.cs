// This file contains all the different microbe stage spawner types
// just so that they are in one place.

using System;
using System.Collections.Generic;
using Components;
using DefaultEcs;
using DefaultEcs.Command;
using Godot;

/// <summary>
///   Helpers for making different types of spawners
/// </summary>
public static class Spawners
{
    public static MicrobeSpawner MakeMicrobeSpawner(Species species,
        CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        return new MicrobeSpawner(species, cloudSystem, currentGame);
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
    public static EntityRecord SpawnCellBurstEffect(IWorldSimulation worldSimulation, Vector3 location)
    {
        // Support spawning this at any time during an update cycle
        var recorder = worldSimulation.StartRecordingEntityCommands();
        var entityCreator = worldSimulation.GetRecorderWorld(recorder);

        var entity = worldSimulation.CreateEntityDeferred(entityCreator);

        entity.Set(new WorldPosition(location));

        entity.Set(new PredefinedVisuals
        {
            VisualIdentifier = VisualResourceIdentifier.CellBurstEffect,
        });

        entity.Set<SpatialInstance>();
        entity.Set<TimedLife>();
        entity.Set<CellBurstEffect>();

        worldSimulation.FinishRecordingEntityCommands(recorder);

        return entity;
    }

    /// <summary>
    ///   Spawns an agent projectile
    /// </summary>
    public static EntityRecord SpawnAgentProjectile(IWorldSimulation worldSimulation, AgentProperties properties,
        float amount, float lifetime, Vector3 location, Vector3 direction, float scale, Entity emitter)
    {
        var normalizedDirection = direction.Normalized();

        var recorder = worldSimulation.StartRecordingEntityCommands();
        var entityCreator = worldSimulation.GetRecorderWorld(recorder);

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
            AxisLock = Physics.AxisLockType.YAxis,
        });
        entity.Set(new PhysicsShapeHolder
        {
            Shape = PhysicsShape.CreateSphere(Constants.TOXIN_PROJECTILE_PHYSICS_SIZE),
        });
        entity.Set(new CollisionManagement
        {
            IgnoredCollisionsWith = new List<Entity> { emitter },

            // Callbacks are initialized by ToxinCollisionSystem
        });

        entity.Set(new ReadableName
        {
            Name = properties.Name,
        });

        worldSimulation.FinishRecordingEntityCommands(recorder);

        return entity;
    }

    /// <summary>
    ///   Spawn a floating chunk (cell parts floating around, rocks, hazards)
    /// </summary>
    public static void SpawnChunk(IWorldSimulation worldSimulation, ChunkConfiguration chunkType,
        Vector3 location, Random random, bool microbeDrop)
    {
        // Resolve the final chunk settings as the chunk configuration is a group of potential things
        var selectedMesh = chunkType.Meshes.Random(random);

        // TODO: do something with these properties:
        // selectedMesh.SceneModelPath,

        // Chunk is spawned with random rotation (in the 2D plane if it's an Easter egg)
        var rotationAxis = chunkType.EasterEgg ? new Vector3(0, 1, 0) : new Vector3(0, 1, 1);

        var recorder = worldSimulation.StartRecordingEntityCommands();
        var entityCreator = worldSimulation.GetRecorderWorld(recorder);

        var entity = worldSimulation.CreateEntityDeferred(entityCreator);

        entity.Set(new WorldPosition(location, new Quat(
            rotationAxis.Normalized(), 2 * Mathf.Pi * (float)random.NextDouble())));

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

        // TODO: this probably needs to be skipped for particle type chunks
        if (true)
        {
            entity.Set(new EntityMaterial
            {
                AutoRetrieveFromSpatial = true,
                AutoRetrieveModelPath = selectedMesh.SceneModelPath,
            });

            entity.Set<MicrobeShaderParameters>();
        }

        if (!string.IsNullOrEmpty(selectedMesh.SceneAnimationPath))
        {
            // TODO: stop the animation somehow for a dropped chunk (as that's the old behaviour if I remember right)
            throw new NotImplementedException();
        }

        // Setup compounds to vent
        // TODO: do something about this variable (I can't remember anymore why I added this -hhyyrylainen)
        bool hasCompounds = false;
        if (chunkType.Compounds?.Count > 0)
        {
            hasCompounds = true;

            // Capacity is 0 to disallow adding any more compounds to the compound bag
            var compounds = new CompoundBag(0);

            foreach (var entry in chunkType.Compounds)
            {
                // Directly write compounds to avoid the capacity limit
                compounds.Compounds.Add(entry.Key, entry.Value.Amount);
            }

            entity.Set(new CompoundStorage
            {
                Compounds = compounds,
            });

            entity.Set(new CompoundVenter
            {
                VentEachCompoundPerSecond = chunkType.VentAmount,
                DestroyOnEmpty = chunkType.Dissolves,
                UsesMicrobialDissolveEffect = true,
            });
        }

        // Chunks that don't dissolve naturally when running out of compounds, are despawned with a timer
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
                DisableParticles = true,
                UsesMicrobialDissolveEffect = true,
                VentCompounds = true,
            });
        }

        entity.Set(new Physics
        {
            AxisLock = Physics.AxisLockType.YAxis,
        });
        entity.Set(new PhysicsShapeHolder
        {
            Shape = selectedMesh.ConvexShapePath != null ?
                PhysicsShape.CreateShapeFromGodotResource(selectedMesh.ConvexShapePath, chunkType.PhysicsDensity) :
                PhysicsShape.CreateSphere(chunkType.Radius, chunkType.PhysicsDensity),
        });

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

        entity.Set(new ReadableName
        {
            Name = new LocalizedString(chunkType.Name),
        });

        worldSimulation.FinishRecordingEntityCommands(recorder);

        // return entity;
    }

    // TODO: remove this old variant
    public static Microbe SpawnMicrobe(Species species, Vector3 location,
        Node worldRoot, PackedScene microbeScene, bool aiControlled,
        CompoundCloudSystem cloudSystem, ISpawnSystem spawnSystem, GameProperties currentGame,
        CellType? multicellularCellType = null)
    {
        var microbe = (Microbe)microbeScene.Instance();

        // The second parameter is (isPlayer), and we assume that if the
        // cell is not AI controlled it is the player's cell
        microbe.Init(cloudSystem, spawnSystem, currentGame, !aiControlled);

        worldRoot.AddChild(microbe);
        microbe.Translation = location;

        if (multicellularCellType != null)
        {
            microbe.ApplyMulticellularNonFirstCellSpecies((EarlyMulticellularSpecies)species, multicellularCellType);
        }
        else
        {
            microbe.ApplySpecies(species);
        }

        microbe.SetInitialCompounds();
        return microbe;
    }

    public static void SpawnMicrobe(IWorldSimulation worldSimulation, Species species, Vector3 location,
        bool aiControlled, CellType? multicellularCellType = null)
    {
        var recorder = SpawnMicrobeWithoutFinalizing(worldSimulation, species, location, aiControlled,
            multicellularCellType, out _);

        worldSimulation.FinishRecordingEntityCommands(recorder);
    }

    public static EntityCommandRecorder SpawnMicrobeWithoutFinalizing(IWorldSimulation worldSimulation, Species species,
        Vector3 location, bool aiControlled, CellType? multicellularCellType, out EntityRecord entity)
    {
        var recorder = worldSimulation.StartRecordingEntityCommands();
        var entityCreator = worldSimulation.GetRecorderWorld(recorder);

        entity = worldSimulation.CreateEntityDeferred(entityCreator);

        // Position
        entity.Set(new WorldPosition(location, Quat.Identity));

        // Player vs. AI controlled microbe components
        if (aiControlled)
        {
            entity.Set<MicrobeAI>();

            // Darwinian evolution statistic tracking (these are the external effects that are passed to auto-evo)
            entity.Set<SurvivalStatistics>();
        }
        else
        {
            // We assume that if the cell is not AI controlled it is the player's cell
            entity.Set<PlayerMarker>();

            // The player's "ears" are placed at the player microbe
            entity.Set<SoundListener>();
        }

        // Base species-based data initialization
        ICellProperties usedCellProperties;
        float engulfSize;
        MembraneType membraneType;

        if (species is EarlyMulticellularSpecies earlyMulticellularSpecies)
        {
            entity.Set(new EarlyMulticellularSpeciesMember
            {
                Species = earlyMulticellularSpecies,
            });

            if (multicellularCellType != null)
            {
                // Non-first cell in an early multicellular colony

                usedCellProperties = multicellularCellType;
                var properties = new CellProperties(multicellularCellType);
                engulfSize = properties.EngulfSize;
                membraneType = properties.MembraneType;
                entity.Set(properties);

                entity.Set(new ColourAnimation(Membrane.MembraneTintFromSpeciesColour(multicellularCellType.Colour)));
            }
            else
            {
                // TODO: should other cells also get this component to allow them to start regrowing after a colony
                // is split apart?
                entity.Set<MulticellularGrowth>();

                usedCellProperties = earlyMulticellularSpecies.Cells[0];
                var properties = new CellProperties(usedCellProperties);
                engulfSize = properties.EngulfSize;
                membraneType = properties.MembraneType;
                entity.Set(properties);

                entity.Set(new ColourAnimation(Membrane.MembraneTintFromSpeciesColour(usedCellProperties.Colour)));
            }
        }
        else if (species is MicrobeSpecies microbeSpecies)
        {
            entity.Set(new MicrobeSpeciesMember
            {
                Species = microbeSpecies,
            });

            usedCellProperties = microbeSpecies;
            var properties = new CellProperties(microbeSpecies);
            engulfSize = properties.EngulfSize;
            membraneType = properties.MembraneType;
            entity.Set(properties);

            entity.Set(new ColourAnimation(Membrane.MembraneTintFromSpeciesColour(usedCellProperties.Colour)));

            if (multicellularCellType != null)
                GD.PrintErr("Multicellular cell type may not be set when spawning a MicrobeSpecies instance");
        }
        else
        {
            throw new NotImplementedException("Unknown species type to spawn a microbe from");
        }

        float storageCapacity;

        // Initialize organelles for the cell type
        {
            var container = default(OrganelleContainer);

            container.CreateOrganelleLayout(usedCellProperties);

            storageCapacity = container.OrganellesCapacity;

            entity.Set(container);
        }

        // Visuals
        var scale = usedCellProperties.IsBacteria ? new Vector3(0.5f, 0.5f, 0.5f) : new Vector3(1, 1, 1);

        entity.Set(new SpatialInstance
        {
            VisualScale = scale,
            ApplyVisualScale = true,
        });

        entity.Set(new EntityMaterial
        {
            AutoRetrieveFromSpatial = true,
        });

        entity.Set<MicrobeShaderParameters>();

        // Compounds
        var compounds = new CompoundBag(storageCapacity);
        compounds.AddInitialCompounds(species.InitialCompounds);

        entity.Set(new CompoundStorage
        {
            Compounds = compounds,
        });

        entity.Set(new CompoundAbsorber
        {
            // This gets set properly later once the membrane is ready
            AbsorbRadius = 0.5f,
        });

        entity.Set(new UnneededCompoundVenter
        {
            VentThreshold = 2,
        });

        // Physics
        entity.Set(new Physics
        {
            AxisLock = Physics.AxisLockType.YAxisWithRotation,
            LinearDamping = 0.2f,
        });

        entity.Set(new CollisionManagement
        {
            RecordActiveCollisions = Constants.MAX_SIMULTANEOUS_DAMAGE_COLLISIONS,
        });

        // The shape is created in the background to reduce lag when something spawns
        entity.Set(new PhysicsShapeHolder
        {
            Shape = null,
        });

        // Movement
        entity.Set(new MicrobeControl(location));
        entity.Set<ManualPhysicsControl>();

        // Other cell features
        entity.Set(new MicrobeStatus
        {
            TimeUntilChemoreceptionUpdate = Constants.CHEMORECEPTOR_COMPOUND_UPDATE_INTERVAL,
            TimeUntilDigestionUpdate = Constants.MICROBE_DIGESTION_UPDATE_INTERVAL,
        });

        entity.Set(new Health(Constants.DEFAULT_HEALTH));

        entity.Set<CommandSignaler>();

        entity.Set(new Engulfable
        {
            BaseEngulfSize = engulfSize,
            RequisiteEnzymeToDigest = SimulationParameters.Instance.GetEnzyme(membraneType.DissolverEnzyme),
        });

        entity.Set(new Engulfer
        {
            EngulfingSize = engulfSize,
            EngulfStorageSize = engulfSize,
        });

        // Microbes are not affected by currents before they are visualized
        // entity.Set<CurrentAffected>();

        // Selecting is used to throw out specific colony members
        entity.Set<Selectable>();

        entity.Set(new ReadableName
        {
            Name = new LocalizedString(species.FormattedName),
        });

        return recorder;
    }

    /// <summary>
    ///   Gives a random chance for a multicellular cell colony to spawn partially or fully grown
    /// </summary>
    /// <param name="microbe">The multicellular microbe</param>
    /// <param name="random">Random to use for the randomness</param>
    /// <exception cref="ArgumentException">If the microbe is not multicellular</exception>
    public static void GiveFullyGrownChanceForMulticellular(Microbe microbe, Random random)
    {
        if (!microbe.IsMulticellular)
            throw new ArgumentException("must be multicellular");

        // Chance to spawn fully grown or partially grown
        if (random.NextDouble() < Constants.CHANCE_MULTICELLULAR_SPAWNS_GROWN)
        {
            microbe.BecomeFullyGrownMulticellularColony();
        }
        else if (random.NextDouble() < Constants.CHANCE_MULTICELLULAR_SPAWNS_PARTLY_GROWN)
        {
            while (!microbe.IsFullyGrownMulticellular)
            {
                microbe.AddMulticellularGrowthCell(true);

                if (random.NextDouble() > Constants.CHANCE_MULTICELLULAR_PARTLY_GROWN_CELL_CHANCE)
                    break;
            }
        }
    }

    public static IEnumerable<Microbe> SpawnBacteriaColony(Species species, Vector3 location,
        Node worldRoot, PackedScene microbeScene, CompoundCloudSystem cloudSystem, ISpawnSystem spawnSystem,
        GameProperties currentGame, Random random)
    {
        var curSpawn = new Vector3(random.Next(1, 8), 0, random.Next(1, 8));

        var clumpSize = random.Next(Constants.MIN_BACTERIAL_COLONY_SIZE,
            Constants.MAX_BACTERIAL_COLONY_SIZE + 1);
        for (int i = 0; i < clumpSize; i++)
        {
            // Dont spawn them on top of each other because it
            // causes them to bounce around and lag
            yield return SpawnMicrobe(species, location + curSpawn, worldRoot, microbeScene, true,
                cloudSystem, spawnSystem, currentGame);

            curSpawn += new Vector3(random.Next(-7, 8), 0, random.Next(-7, 8));
        }
    }

    public static PackedScene LoadMicrobeScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
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
            location = new Transform(
                new Basis(location.basis.Quat() * RandomRotationForResourceEntity(random)), location.origin);
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
    private readonly PackedScene microbeScene;
    private readonly CompoundCloudSystem cloudSystem;
    private readonly GameProperties currentGame;
    private readonly Random random = new();

    public MicrobeSpawner(Species species, CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        Species = species ?? throw new ArgumentException("species is null");

        microbeScene = SpawnHelpers.LoadMicrobeScene();
        this.cloudSystem = cloudSystem;
        this.currentGame = currentGame;
    }

    public override bool SpawnsEntities => true;

    public Species Species { get; }

    public override IEnumerable<Entity>? Spawn(Node worldNode, Vector3 location, ISpawnSystem spawnSystem)
    {
        // This should no longer happen, but let's keep this print here to keep track of the situation
        if (Species.Obsolete)
            GD.PrintErr("Obsolete species microbe has spawned");

        // The true here is that this is AI controlled
        var first = SpawnHelpers.SpawnMicrobe(Species, location, worldNode, microbeScene, true, cloudSystem,
            spawnSystem, currentGame);

        if (first.IsMulticellular)
        {
            SpawnHelpers.GiveFullyGrownChanceForMulticellular(first, random);
        }

        throw new NotImplementedException();

        // yield return first;

        // TODO: redo
        throw new NotImplementedException();

        // ModLoader.ModInterface.TriggerOnMicrobeSpawned(first);

        // Just in case the is bacteria flag is not correct in a multicellular cell type, here's an extra safety check
        if (first.CellTypeProperties.IsBacteria && !first.IsMulticellular)
        {
            foreach (var colonyMember in SpawnHelpers.SpawnBacteriaColony(Species, location, worldNode,
                         microbeScene, cloudSystem, spawnSystem, currentGame, random))
            {
                throw new NotImplementedException();

                // yield return colonyMember;

                // ModLoader.ModInterface.TriggerOnMicrobeSpawned(colonyMember);
            }
        }
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

    public override IEnumerable<Entity>? Spawn(Node worldNode, Vector3 location, ISpawnSystem spawnSystem)
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

    public override IEnumerable<Entity>? Spawn(Node worldNode, Vector3 location, ISpawnSystem spawnSystem)
    {
        throw new NotImplementedException();

        // var chunk = SpawnHelpers.SpawnChunk(chunkType, location, worldNode, chunkScene,
        //     random);
        //
        // yield return chunk;
        //
        // ModLoader.ModInterface.TriggerOnChunkSpawned(chunk, true);
    }

    public override string ToString()
    {
        return $"ChunkSpawner for {chunkType.Name}";
    }
}
