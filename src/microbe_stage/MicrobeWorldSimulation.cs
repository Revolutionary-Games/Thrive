using Arch.Core;
using Components;
using Godot;
using SharedBase.Archive;
using Systems;
using World = Arch.Core.World;

/// <summary>
///   Contains all the parts needed to simulate a microbial world. Separate from (but used by) the
///   <see cref="MicrobeStage"/> to also allow other parts of the code to easily run a microbe simulation
/// </summary>
public partial class MicrobeWorldSimulation : WorldSimulationWithPhysics
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Base systems
    private AnimationControlSystem animationControlSystem = null!;
    private AttachedEntityPositionSystem attachedEntityPositionSystem = null!;
    private ColourAnimationSystem colourAnimationSystem = null!;
    private CountLimitedDespawnSystem countLimitedDespawnSystem = null!;
    private DamageCooldownSystem damageCooldownSystem = null!;
    private DamageOnTouchSystem damageOnTouchSystem = null!;
    private DisallowPlayerBodySleepSystem disallowPlayerBodySleepSystem = null!;
    private EntityLightSystem entityLightSystem = null!;
    private EntityMaterialFetchSystem entityMaterialFetchSystem = null!;
    private FadeOutActionSystem fadeOutActionSystem = null!;
    private PathBasedSceneLoader pathBasedSceneLoader = null!;
    private PhysicsBodyControlSystem physicsBodyControlSystem = null!;
    private PhysicsBodyCreationSystem physicsBodyCreationSystem = null!;
    private PhysicsBodyDisablingSystem physicsBodyDisablingSystem = null!;
    private PhysicsCollisionManagementSystem physicsCollisionManagementSystem = null!;
    private PhysicsSensorSystem physicsSensorSystem = null!;
    private CollisionShapeLoaderSystem collisionShapeLoaderSystem = null!;
    private PhysicsUpdateAndPositionSystem physicsUpdateAndPositionSystem = null!;
    private PredefinedVisualLoaderSystem predefinedVisualLoaderSystem = null!;

    private SimpleShapeCreatorSystem simpleShapeCreatorSystem = null!;
    private SoundEffectSystem soundEffectSystem = null!;
    private SoundListenerSystem soundListenerSystem = null!;
    private SpatialAttachSystem spatialAttachSystem = null!;
    private SpatialPositionSystem spatialPositionSystem = null!;

    // Microbe systems
    private AllCompoundsVentingSystem allCompoundsVentingSystem = null!;
    private CellBurstEffectSystem cellBurstEffectSystem = null!;
    private ColonyBindingSystem colonyBindingSystem = null!;
    private ColonyCompoundDistributionSystem colonyCompoundDistributionSystem = null!;
    private ColonyStatsUpdateSystem colonyStatsUpdateSystem = null!;
    private CompoundAbsorptionSystem compoundAbsorptionSystem = null!;
    private DamageSoundSystem damageSoundSystem = null!;
    private EndosymbiontOrganelleSystem endosymbiontOrganelleSystem = null!;
    private EngulfedDigestionSystem engulfedDigestionSystem = null!;
    private EngulfedHandlingSystem engulfedHandlingSystem = null!;
    private EngulfingSystem engulfingSystem = null!;

    private EntitySignalingSystem entitySignalingSystem = null!;

    private IrradiationSystem irradiationSystem = null!;
    private MicrobeAISystem microbeAI = null!;
    private MicrobeCollisionSoundSystem microbeCollisionSoundSystem = null!;
    private MicrobeDeathSystem microbeDeathSystem = null!;
    private MicrobeEmissionSystem microbeEmissionSystem = null!;
    private MicrobeEventCallbackSystem microbeEventCallbackSystem = null!;
    private MicrobeFlashingSystem microbeFlashingSystem = null!;
    private MicrobeHeatAccumulationSystem microbeHeatAccumulationSystem = null!;
    private MicrobeMovementSoundSystem microbeMovementSoundSystem = null!;
    private MicrobeMovementSystem microbeMovementSystem = null!;
    private StrainSystem strainSystem = null!;
    private MicrobeShaderSystem microbeShaderSystem = null!;
    private MicrobeTemporaryEffectsSystem microbeTemporaryEffectsSystem = null!;
    private MicrobeVisualsSystem microbeVisualsSystem = null!;
    private OrganelleComponentFetchSystem organelleComponentFetchSystem = null!;
    private OrganelleTickSystem organelleTickSystem = null!;
    private OsmoregulationAndHealingSystem osmoregulationAndHealingSystem = null!;
    private PilusDamageSystem pilusDamageSystem = null!;
    private RadiationDamageSystem radiationDamageSystem = null!;
    private SlimeSlowdownSystem slimeSlowdownSystem = null!;
    private MucocystSystem mucocystSystem = null!;
    private MicrobeDivisionClippingSystem microbeDivisionClippingSystem = null!;

    private MicrobePhysicsCreationAndSizeSystem microbePhysicsCreationAndSizeSystem = null!;
    private MicrobeRenderPrioritySystem microbeRenderPrioritySystem = null!;
    private MicrobeReproductionSystem microbeReproductionSystem = null!;
    private TintColourApplyingSystem tintColourApplyingSystem = null!;
    private ToxinCollisionSystem toxinCollisionSystem = null!;
    private SiderophoreSystem siderophoreSystem = null!;
    private UnneededCompoundVentingSystem unneededCompoundVentingSystem = null!;

    // Multicellular systems
    private DelayedColonyOperationSystem delayedColonyOperationSystem = null!;
    private MulticellularGrowthSystem multicellularGrowthSystem = null!;
    private IntercellularMatrixSystem intercellularMatrixSystem = null!;

#pragma warning disable CA2213
    private Node visualsParent = null!;
#pragma warning restore CA2213

    public MicrobeWorldSimulation()
    {
    }

    protected MicrobeWorldSimulation(World entities) : base(entities)
    {
    }

    // External system references

    public CompoundCloudSystem CloudSystem { get; private set; } = null!;

    // Systems accessible to the outside as these have some very specific methods to be called on them
    public CameraFollowSystem CameraFollowSystem { get; private set; } = null!;

    public SpawnSystem SpawnSystem { get; private set; } = null!;

    public MicrobeTerrainSystem MicrobeTerrainSystem { get; private set; } = null!;

    public ProcessSystem ProcessSystem { get; private set; } = null!;

    // TODO: could replace this reference in PatchManager by it just calling ClearPlayerLocationDependentCaches
    public TimedLifeSystem TimedLifeSystem { get; private set; } = null!;

    public FluidCurrentsSystem FluidCurrentsSystem { get; private set; } = null!;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MicrobeWorldSimulation;

    public static MicrobeWorldSimulation ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Read this first from the archive and discard as we need the next thing for the constructor
        UnsavedEntities temp = new([]);
        reader.ReadObjectProperties(temp);

        var instance = new MicrobeWorldSimulation(reader.ReadObject<World>());

        reader.ReportObjectConstructorDone(instance, referenceId);

        // The base version is different from ours
        instance.ReadBasePropertiesFromArchive(reader, 1);

        instance.ResolveNodeReferences();

        reader.ReadObjectProperties(instance.entitySignalingSystem);
        instance.MicrobeTerrainSystem = reader.ReadObject<MicrobeTerrainSystem>();
        instance.SpawnSystem = reader.ReadObject<SpawnSystem>();
        reader.ReadObjectProperties(instance.FluidCurrentsSystem);

        instance.DeactivateWorldOnReadContext(reader);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        WriteBasePropertiesToArchive(writer);

        writer.WriteObjectProperties(entitySignalingSystem);
        writer.WriteObject(MicrobeTerrainSystem);
        writer.WriteObject(SpawnSystem);
        writer.WriteObjectProperties(FluidCurrentsSystem);
    }

    /// <summary>
    ///   First initialization step which creates all the system objects. When loading from a save objects of this
    ///   type should have <see cref="AssignOnlyChildItemsOnDeserializeAttribute"/> and this method should be called
    ///   before those child properties are loaded.
    /// </summary>
    /// <param name="visualDisplayRoot">Godot Node to place all simulation graphics underneath</param>
    /// <param name="cloudSystem">
    ///   Compound cloud simulation system. This method will call <see cref="CompoundCloudSystem.Init"/>
    /// </param>
    /// <param name="spawnEnvironment">Spawn environment data to give to microbes spawned by systems</param>
    public void Init(Node visualDisplayRoot, CompoundCloudSystem cloudSystem, IMicrobeSpawnEnvironment spawnEnvironment)
    {
        InitGenerated();
        ResolveNodeReferences();

        visualsParent = visualDisplayRoot;

        if (GenerateThreadedSystems.UseCheckedComponentAccess)
        {
            GD.Print("Disallowing threaded execution to allow strict component thread checks to work");
            World.SharedJobScheduler = null;
        }

        // Systems stored in fields
        animationControlSystem = new AnimationControlSystem(EntitySystem);
        attachedEntityPositionSystem = new AttachedEntityPositionSystem(this, EntitySystem);
        colourAnimationSystem = new ColourAnimationSystem(EntitySystem);
        countLimitedDespawnSystem = new CountLimitedDespawnSystem(this, EntitySystem);
        damageCooldownSystem = new DamageCooldownSystem(EntitySystem);
        damageOnTouchSystem = new DamageOnTouchSystem(this, EntitySystem);
        disallowPlayerBodySleepSystem = new DisallowPlayerBodySleepSystem(physics, EntitySystem);
        entityLightSystem = new EntityLightSystem(EntitySystem);
        entityMaterialFetchSystem = new EntityMaterialFetchSystem(EntitySystem);
        fadeOutActionSystem = new FadeOutActionSystem(this, cloudSystem, EntitySystem);
        pathBasedSceneLoader = new PathBasedSceneLoader(EntitySystem);
        physicsBodyControlSystem = new PhysicsBodyControlSystem(physics, EntitySystem);
        physicsBodyDisablingSystem = new PhysicsBodyDisablingSystem(physics, EntitySystem);
        physicsBodyCreationSystem =
            new PhysicsBodyCreationSystem(this, physicsBodyDisablingSystem, EntitySystem);
        physicsCollisionManagementSystem =
            new PhysicsCollisionManagementSystem(physics, EntitySystem);
        physicsSensorSystem = new PhysicsSensorSystem(this, EntitySystem);
        physicsUpdateAndPositionSystem = new PhysicsUpdateAndPositionSystem(physics, EntitySystem);
        collisionShapeLoaderSystem = new CollisionShapeLoaderSystem(EntitySystem);
        predefinedVisualLoaderSystem = new PredefinedVisualLoaderSystem(EntitySystem);

        simpleShapeCreatorSystem = new SimpleShapeCreatorSystem(EntitySystem);

        // TODO: different root for sounds?
        soundEffectSystem = new SoundEffectSystem(visualsParent, EntitySystem);
        soundListenerSystem = new SoundListenerSystem(visualsParent, EntitySystem);
        spatialAttachSystem = new SpatialAttachSystem(visualsParent, EntitySystem);
        spatialPositionSystem = new SpatialPositionSystem(EntitySystem);

        allCompoundsVentingSystem = new AllCompoundsVentingSystem(cloudSystem, this, EntitySystem);
        cellBurstEffectSystem = new CellBurstEffectSystem(EntitySystem);

        colonyBindingSystem = new ColonyBindingSystem(this, EntitySystem);
        colonyCompoundDistributionSystem = new ColonyCompoundDistributionSystem(EntitySystem);
        colonyStatsUpdateSystem = new ColonyStatsUpdateSystem(this, EntitySystem);

        // TODO: clouds currently only allow 2 thread to absorb at once
        compoundAbsorptionSystem = new CompoundAbsorptionSystem(cloudSystem, EntitySystem);

        damageSoundSystem = new DamageSoundSystem(EntitySystem);
        endosymbiontOrganelleSystem = new EndosymbiontOrganelleSystem(EntitySystem);
        engulfedDigestionSystem = new EngulfedDigestionSystem(cloudSystem, EntitySystem);
        engulfedHandlingSystem = new EngulfedHandlingSystem(this, SpawnSystem, EntitySystem);

        strainSystem = new StrainSystem(EntitySystem);
        microbeMovementSystem = new MicrobeMovementSystem(this, PhysicalWorld, EntitySystem);

        irradiationSystem = new IrradiationSystem(EntitySystem);
        microbeAI = new MicrobeAISystem(cloudSystem, spawnEnvironment.DaylightInfo, EntitySystem);
        microbeCollisionSoundSystem = new MicrobeCollisionSoundSystem(EntitySystem);
        microbeEmissionSystem = new MicrobeEmissionSystem(this, cloudSystem, EntitySystem);

        microbeEventCallbackSystem = new MicrobeEventCallbackSystem(cloudSystem, microbeAI, EntitySystem);
        microbeFlashingSystem = new MicrobeFlashingSystem(EntitySystem);
        microbeHeatAccumulationSystem = new MicrobeHeatAccumulationSystem(EntitySystem);
        microbeMovementSoundSystem = new MicrobeMovementSoundSystem(EntitySystem);
        microbeShaderSystem = new MicrobeShaderSystem(EntitySystem);
        microbeTemporaryEffectsSystem = new MicrobeTemporaryEffectsSystem(EntitySystem);

        microbeVisualsSystem = new MicrobeVisualsSystem(EntitySystem);
        organelleComponentFetchSystem = new OrganelleComponentFetchSystem(EntitySystem);
        organelleTickSystem = new OrganelleTickSystem(this, EntitySystem);
        osmoregulationAndHealingSystem = new OsmoregulationAndHealingSystem(EntitySystem);
        pilusDamageSystem = new PilusDamageSystem(EntitySystem);
        radiationDamageSystem = new RadiationDamageSystem(EntitySystem);
        slimeSlowdownSystem = new SlimeSlowdownSystem(cloudSystem, EntitySystem);
        mucocystSystem = new MucocystSystem(EntitySystem);
        microbeDivisionClippingSystem = new MicrobeDivisionClippingSystem(this, physics, EntitySystem);
        microbePhysicsCreationAndSizeSystem = new MicrobePhysicsCreationAndSizeSystem(EntitySystem);
        microbeRenderPrioritySystem = new MicrobeRenderPrioritySystem(EntitySystem);
        tintColourApplyingSystem = new TintColourApplyingSystem(EntitySystem);

        toxinCollisionSystem = new ToxinCollisionSystem(EntitySystem);
        siderophoreSystem = new SiderophoreSystem(EntitySystem, this);

        unneededCompoundVentingSystem = new UnneededCompoundVentingSystem(cloudSystem, EntitySystem);

        // Systems stored in properties
        CameraFollowSystem = new CameraFollowSystem(EntitySystem);

        ProcessSystem = new ProcessSystem(EntitySystem);

        TimedLifeSystem = new TimedLifeSystem(this, EntitySystem);

        microbeReproductionSystem =
            new MicrobeReproductionSystem(this, spawnEnvironment, SpawnSystem, EntitySystem);
        microbeDeathSystem = new MicrobeDeathSystem(this, SpawnSystem, EntitySystem);
        engulfingSystem = new EngulfingSystem(this, SpawnSystem, EntitySystem);

        delayedColonyOperationSystem =
            new DelayedColonyOperationSystem(this, spawnEnvironment, SpawnSystem, EntitySystem);
        multicellularGrowthSystem =
            new MulticellularGrowthSystem(this, spawnEnvironment, SpawnSystem, EntitySystem);
        intercellularMatrixSystem = new IntercellularMatrixSystem(EntitySystem);

        CloudSystem = cloudSystem;

        physics.RemoveGravity();

        RunSystemInits();

        OnInitialized();

        // In case this is loaded from a save, ensure the next save has correct ignore entities
        entitiesToNotSave.SetExtraIgnoreSource(queuedForDelete);
    }

    /// <summary>
    ///   Second phase initialization that requires access to the current game info. Must also be performed, otherwise
    ///   this class won't function correctly.
    /// </summary>
    /// <param name="currentGame">Currently started game</param>
    public void InitForCurrentGame(GameProperties currentGame)
    {
        osmoregulationAndHealingSystem.SetWorld(currentGame.GameWorld);
        HealthHelpers.SetWorld(currentGame.GameWorld);
        microbeReproductionSystem.SetWorld(currentGame.GameWorld);
        microbeDeathSystem.SetWorld(currentGame.GameWorld);
        microbeHeatAccumulationSystem.SetWorld(currentGame.GameWorld);
        multicellularGrowthSystem.SetWorld(currentGame.GameWorld);
        engulfingSystem.SetWorld(currentGame.GameWorld);
        engulfedDigestionSystem.SetWorld(currentGame.GameWorld);
        microbeAI.SetWorld(currentGame.GameWorld);
        damageSoundSystem.SetWorld(currentGame.GameWorld);
        FluidCurrentsSystem.SetWorld(currentGame.GameWorld);

        CloudSystem.Init(FluidCurrentsSystem);
    }

    public void SetSimulationBiome(BiomeConditions biomeConditions)
    {
        ProcessSystem.SetBiome(biomeConditions);
    }

    public float SampleTemperatureAt(Vector3 worldPosition)
    {
        return microbeHeatAccumulationSystem.SampleTemperatureAt(worldPosition);
    }

    /// <summary>
    ///   Clears system data that has been stored based on the player location. Call this when the player changes
    ///   location by a lot by respawning or by moving patches
    /// </summary>
    public void ClearPlayerLocationDependentCaches()
    {
        SpawnSystem.ClearSpawnCoordinates();
    }

    public override bool HasSystemsWithPendingOperations()
    {
        return microbeVisualsSystem.HasPendingOperations();
    }

    public override void FreeNodeResources()
    {
        base.FreeNodeResources();

        soundEffectSystem.FreeNodeResources();
        spatialAttachSystem.FreeNodeResources();
    }

    internal void OverrideMicrobeAIRandomSeed(int seed)
    {
        microbeAI.OverrideAIRandomSeed(seed);
    }

    protected override void InitSystemsEarly()
    {
        entitySignalingSystem = new EntitySignalingSystem(EntitySystem);

        // TODO: load time from save
        FluidCurrentsSystem = new FluidCurrentsSystem(EntitySystem, 0);

        // These two get overwritten on a save load
        MicrobeTerrainSystem = new MicrobeTerrainSystem(this, EntitySystem);
        SpawnSystem = new SpawnSystem(this, EntitySystem, MicrobeTerrainSystem.IsPositionBlocked);
    }

    protected override void OnProcessFixedLogic(float delta)
    {
        int availableThreads = TaskExecutor.Instance.ParallelTasks;

        var settings = Settings.Instance;
        if (settings.RunAutoEvoDuringGamePlay)
            --availableThreads;

        if (!settings.RunGameSimulationMultithreaded || GenerateThreadedSystems.UseCheckedComponentAccess)
        {
            availableThreads = 1;
        }

        // For single-threaded testing uncomment the next line:
        // availableThreads = 1;

        // This now does a plain count compare as no system itself currently runs threaded as there should be no
        // threat of deadlock with Arch ECS.
        // But there is still a +1 as the thread-using version doesn't really give significant performance improvement.
        if (availableThreads > GenerateThreadedSystems.TargetThreadCount + 1)
        {
            OnProcessFixedWithThreads(delta);
        }
        else
        {
            OnProcessFixedWithoutThreads(delta);
        }
    }

    protected override void OnProcessFrameLogic(float delta)
    {
        OnProcessFrameLogicGenerated(delta);
    }

    protected override void OnEntityDestroyed(in Entity entity)
    {
        base.OnEntityDestroyed(in entity);

        physicsCollisionManagementSystem.OnEntityDestroyed(entity);
        physicsBodyDisablingSystem.OnEntityDestroyed(entity);
        physicsBodyCreationSystem.OnEntityDestroyed(entity);
        physicsSensorSystem.OnEntityDestroyed(entity);

        engulfingSystem.OnEntityDestroyed(entity);
        colonyStatsUpdateSystem.OnEntityDestroyed(entity);
        entitySignalingSystem.OnEntityDestroyed(entity);
    }

    protected override void OnPlayerPositionSet(Vector3 playerPosition)
    {
        // Immediately report to some systems
        countLimitedDespawnSystem.ReportPlayerPosition(playerPosition);
        soundEffectSystem.ReportPlayerPosition(playerPosition);
        SpawnSystem.ReportPlayerPosition(playerPosition);
        MicrobeTerrainSystem.ReportPlayerPosition(playerPosition);

        // Report to the kind of external clouds system as this simplifies code using the simulation
        CloudSystem.ReportPlayerPosition(playerPosition);
    }

    protected override void Dispose(bool disposing)
    {
        // Must disable recording to avoid disposing exceptions from metrics reporting
        physics.DisablePhysicsTimeRecording = true;
        WaitForStartedPhysicsRun();

        if (disposing)
        {
            // If disposed before Init is called, problems will happen without this check. This happens, for example,
            // if loading a save made in the editor and quitting the game without exiting the editor first to the
            // microbe stage.
            if (animationControlSystem != null!)
            {
                animationControlSystem.Dispose();
                attachedEntityPositionSystem.Dispose();
                colourAnimationSystem.Dispose();
                countLimitedDespawnSystem.Dispose();
                damageCooldownSystem.Dispose();
                damageOnTouchSystem.Dispose();
                disallowPlayerBodySleepSystem.Dispose();
                entityLightSystem.Dispose();
                entityMaterialFetchSystem.Dispose();
                fadeOutActionSystem.Dispose();
                pathBasedSceneLoader.Dispose();
                physicsBodyControlSystem.Dispose();
                physicsBodyCreationSystem.Dispose();
                physicsBodyDisablingSystem.Dispose();
                physicsCollisionManagementSystem.Dispose();
                physicsSensorSystem.Dispose();
                physicsUpdateAndPositionSystem.Dispose();
                collisionShapeLoaderSystem.Dispose();
                predefinedVisualLoaderSystem.Dispose();
                simpleShapeCreatorSystem.Dispose();
                soundEffectSystem.Dispose();
                soundListenerSystem.Dispose();
                spatialAttachSystem.Dispose();
                spatialPositionSystem.Dispose();

                allCompoundsVentingSystem.Dispose();
                cellBurstEffectSystem.Dispose();
                colonyBindingSystem.Dispose();
                colonyCompoundDistributionSystem.Dispose();
                colonyStatsUpdateSystem.Dispose();
                compoundAbsorptionSystem.Dispose();
                damageSoundSystem.Dispose();
                endosymbiontOrganelleSystem.Dispose();
                engulfedDigestionSystem.Dispose();
                engulfedHandlingSystem.Dispose();
                engulfingSystem.Dispose();
                entitySignalingSystem.Dispose();
                FluidCurrentsSystem.Dispose();
                irradiationSystem.Dispose();
                microbeAI.Dispose();
                microbeCollisionSoundSystem.Dispose();
                microbeDeathSystem.Dispose();
                microbeEmissionSystem.Dispose();
                microbeEventCallbackSystem.Dispose();
                microbeFlashingSystem.Dispose();
                microbeHeatAccumulationSystem.Dispose();
                microbeMovementSoundSystem.Dispose();
                microbeMovementSystem.Dispose();
                strainSystem.Dispose();
                microbeShaderSystem.Dispose();
                microbeTemporaryEffectsSystem.Dispose();
                microbeVisualsSystem.Dispose();
                organelleComponentFetchSystem.Dispose();
                organelleTickSystem.Dispose();
                osmoregulationAndHealingSystem.Dispose();
                pilusDamageSystem.Dispose();
                radiationDamageSystem.Dispose();
                slimeSlowdownSystem.Dispose();
                mucocystSystem.Dispose();
                microbeDivisionClippingSystem.Dispose();
                microbePhysicsCreationAndSizeSystem.Dispose();
                microbeRenderPrioritySystem.Dispose();
                microbeReproductionSystem.Dispose();
                tintColourApplyingSystem.Dispose();
                toxinCollisionSystem.Dispose();
                siderophoreSystem.Dispose();
                unneededCompoundVentingSystem.Dispose();
                delayedColonyOperationSystem.Dispose();
                multicellularGrowthSystem.Dispose();
                intercellularMatrixSystem.Dispose();

                CameraFollowSystem.Dispose();
                ProcessSystem.Dispose();
                TimedLifeSystem.Dispose();
            }

            if (SpawnSystem != null!)
                SpawnSystem.Dispose();

            if (MicrobeTerrainSystem != null!)
                MicrobeTerrainSystem.Dispose();

            DisposeGenerated();
        }

        base.Dispose(disposing);
    }
}
