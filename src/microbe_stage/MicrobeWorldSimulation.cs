using Components;
using DefaultEcs;
using DefaultEcs.Threading;
using Godot;
using Newtonsoft.Json;
using Systems;
using World = DefaultEcs.World;

/// <summary>
///   Contains all the parts needed to simulate a microbial world. Separate from (but used by) the
///   <see cref="MicrobeStage"/> to also allow other parts of the code to easily run a microbe simulation
/// </summary>
public partial class MicrobeWorldSimulation : WorldSimulationWithPhysics
{
    private readonly IParallelRunner nonParallelRunner = new DefaultParallelRunner(1);

    // Base systems
    private AnimationControlSystem animationControlSystem = null!;
    private AttachedEntityPositionSystem attachedEntityPositionSystem = null!;
    private ColourAnimationSystem colourAnimationSystem = null!;
    private CountLimitedDespawnSystem countLimitedDespawnSystem = null!;
    private DamageCooldownSystem damageCooldownSystem = null!;
    private DamageOnTouchSystem damageOnTouchSystem = null!;
    private DisallowPlayerBodySleepSystem disallowPlayerBodySleepSystem = null!;
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
    private EngulfedDigestionSystem engulfedDigestionSystem = null!;
    private EngulfedHandlingSystem engulfedHandlingSystem = null!;
    private EngulfingSystem engulfingSystem = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private EntitySignalingSystem entitySignalingSystem = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private FluidCurrentsSystem fluidCurrentsSystem = null!;

    private MicrobeAISystem microbeAI = null!;
    private MicrobeCollisionSoundSystem microbeCollisionSoundSystem = null!;
    private MicrobeDeathSystem microbeDeathSystem = null!;
    private MicrobeEmissionSystem microbeEmissionSystem = null!;
    private MicrobeEventCallbackSystem microbeEventCallbackSystem = null!;
    private MicrobeFlashingSystem microbeFlashingSystem = null!;
    private MicrobeMovementSoundSystem microbeMovementSoundSystem = null!;
    private MicrobeMovementSystem microbeMovementSystem = null!;
    private MicrobeShaderSystem microbeShaderSystem = null!;
    private MicrobeVisualsSystem microbeVisualsSystem = null!;
    private OrganelleComponentFetchSystem organelleComponentFetchSystem = null!;
    private OrganelleTickSystem organelleTickSystem = null!;
    private OsmoregulationAndHealingSystem osmoregulationAndHealingSystem = null!;
    private PilusDamageSystem pilusDamageSystem = null!;
    private SlimeSlowdownSystem slimeSlowdownSystem = null!;
    private MicrobePhysicsCreationAndSizeSystem microbePhysicsCreationAndSizeSystem = null!;
    private MicrobeRenderPrioritySystem microbeRenderPrioritySystem = null!;
    private MicrobeReproductionSystem microbeReproductionSystem = null!;
    private TintColourApplyingSystem tintColourApplyingSystem = null!;
    private ToxinCollisionSystem toxinCollisionSystem = null!;
    private UnneededCompoundVentingSystem unneededCompoundVentingSystem = null!;

    // Multicellular systems
    private DelayedColonyOperationSystem delayedColonyOperationSystem = null!;
    private MulticellularGrowthSystem multicellularGrowthSystem = null!;

    private EntitySet cellCountingEntitySet = null!;

#pragma warning disable CA2213
    private Node visualsParent = null!;
#pragma warning restore CA2213

    public MicrobeWorldSimulation()
    {
    }

    [JsonConstructor]
    public MicrobeWorldSimulation(World entities) : base(entities)
    {
    }

    // External system references

    [JsonIgnore]
    public CompoundCloudSystem CloudSystem { get; private set; } = null!;

    // Systems accessible to the outside as these have some very specific methods to be called on them
    [JsonIgnore]
    public CameraFollowSystem CameraFollowSystem { get; private set; } = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public SpawnSystem SpawnSystem { get; private set; } = null!;

    [JsonIgnore]
    public ProcessSystem ProcessSystem { get; private set; } = null!;

    // TODO: could replace this reference in PatchManager by it just calling ClearPlayerLocationDependentCaches
    [JsonIgnore]
    public TimedLifeSystem TimedLifeSystem { get; private set; } = null!;

    /// <summary>
    ///   First initialization step which creates all the system objects. When loading from a save objects of this
    ///   type should have <see cref="AssignOnlyChildItemsOnDeserializeAttribute"/> and this method should be called
    ///   before those child properties are loaded.
    /// </summary>
    /// <param name="visualDisplayRoot">Godot Node to place all simulation graphics underneath</param>
    /// <param name="cloudSystem">
    ///   Compound cloud simulation system. This method will call <see cref="CompoundCloudSystem.Init"/>
    /// </param>
    public void Init(Node visualDisplayRoot, CompoundCloudSystem cloudSystem)
    {
        ResolveNodeReferences();

        visualsParent = visualDisplayRoot;

        // Threading using our task system
        var parallelRunner = TaskExecutor.Instance;

        // Set on systems that can be ran in parallel but aren't currently as there's no real performance improvement
        // / the system entity count per thread needs tweaking before there's any benefit
        var couldParallelize = new DefaultParallelRunner(1);

        // Systems stored in fields
        animationControlSystem = new AnimationControlSystem(EntitySystem);
        attachedEntityPositionSystem = new AttachedEntityPositionSystem(this, EntitySystem, couldParallelize);
        colourAnimationSystem = new ColourAnimationSystem(EntitySystem, parallelRunner);
        countLimitedDespawnSystem = new CountLimitedDespawnSystem(this, EntitySystem);
        damageCooldownSystem = new DamageCooldownSystem(EntitySystem, couldParallelize);
        damageOnTouchSystem = new DamageOnTouchSystem(this, EntitySystem, couldParallelize);
        disallowPlayerBodySleepSystem = new DisallowPlayerBodySleepSystem(physics, EntitySystem);
        entityMaterialFetchSystem = new EntityMaterialFetchSystem(EntitySystem);
        fadeOutActionSystem = new FadeOutActionSystem(this, cloudSystem, EntitySystem, couldParallelize);
        pathBasedSceneLoader = new PathBasedSceneLoader(EntitySystem, nonParallelRunner);
        physicsBodyControlSystem = new PhysicsBodyControlSystem(physics, EntitySystem, couldParallelize);
        physicsBodyDisablingSystem = new PhysicsBodyDisablingSystem(physics, EntitySystem);
        physicsBodyCreationSystem =
            new PhysicsBodyCreationSystem(this, physicsBodyDisablingSystem, EntitySystem);
        physicsCollisionManagementSystem =
            new PhysicsCollisionManagementSystem(physics, EntitySystem, couldParallelize);
        physicsSensorSystem = new PhysicsSensorSystem(this, EntitySystem);
        physicsUpdateAndPositionSystem = new PhysicsUpdateAndPositionSystem(physics, EntitySystem, couldParallelize);
        collisionShapeLoaderSystem = new CollisionShapeLoaderSystem(EntitySystem, couldParallelize);
        predefinedVisualLoaderSystem = new PredefinedVisualLoaderSystem(EntitySystem);

        simpleShapeCreatorSystem = new SimpleShapeCreatorSystem(EntitySystem, couldParallelize);

        // TODO: different root for sounds?
        soundEffectSystem = new SoundEffectSystem(visualsParent, EntitySystem);
        soundListenerSystem = new SoundListenerSystem(visualsParent, EntitySystem, nonParallelRunner);
        spatialAttachSystem = new SpatialAttachSystem(visualsParent, EntitySystem);
        spatialPositionSystem = new SpatialPositionSystem(EntitySystem);

        allCompoundsVentingSystem = new AllCompoundsVentingSystem(cloudSystem, this, EntitySystem);
        cellBurstEffectSystem = new CellBurstEffectSystem(EntitySystem);

        colonyBindingSystem = new ColonyBindingSystem(this, EntitySystem, couldParallelize);
        colonyCompoundDistributionSystem = new ColonyCompoundDistributionSystem(EntitySystem, couldParallelize);
        colonyStatsUpdateSystem = new ColonyStatsUpdateSystem(this, EntitySystem, couldParallelize);

        // TODO: clouds currently only allow 2 thread to absorb at once
        compoundAbsorptionSystem = new CompoundAbsorptionSystem(cloudSystem, EntitySystem, parallelRunner);

        damageSoundSystem = new DamageSoundSystem(EntitySystem, couldParallelize);
        engulfedDigestionSystem = new EngulfedDigestionSystem(cloudSystem, EntitySystem, parallelRunner);
        engulfedHandlingSystem = new EngulfedHandlingSystem(this, SpawnSystem, EntitySystem, couldParallelize);

        microbeMovementSystem = new MicrobeMovementSystem(PhysicalWorld, EntitySystem, parallelRunner);

        microbeAI = new MicrobeAISystem(cloudSystem, EntitySystem, parallelRunner);
        microbeCollisionSoundSystem = new MicrobeCollisionSoundSystem(EntitySystem, couldParallelize);
        microbeEmissionSystem = new MicrobeEmissionSystem(this, cloudSystem, EntitySystem, couldParallelize);

        microbeEventCallbackSystem = new MicrobeEventCallbackSystem(cloudSystem, microbeAI, EntitySystem);
        microbeFlashingSystem = new MicrobeFlashingSystem(EntitySystem, couldParallelize);
        microbeMovementSoundSystem = new MicrobeMovementSoundSystem(EntitySystem, couldParallelize);
        microbeShaderSystem = new MicrobeShaderSystem(EntitySystem);

        microbeVisualsSystem = new MicrobeVisualsSystem(EntitySystem);
        organelleComponentFetchSystem = new OrganelleComponentFetchSystem(EntitySystem, couldParallelize);
        organelleTickSystem = new OrganelleTickSystem(this, EntitySystem, parallelRunner);
        osmoregulationAndHealingSystem = new OsmoregulationAndHealingSystem(EntitySystem, couldParallelize);
        pilusDamageSystem = new PilusDamageSystem(EntitySystem, couldParallelize);
        slimeSlowdownSystem = new SlimeSlowdownSystem(cloudSystem, EntitySystem, couldParallelize);
        microbePhysicsCreationAndSizeSystem = new MicrobePhysicsCreationAndSizeSystem(EntitySystem, couldParallelize);
        microbeRenderPrioritySystem = new MicrobeRenderPrioritySystem(EntitySystem);
        tintColourApplyingSystem = new TintColourApplyingSystem(EntitySystem);

        toxinCollisionSystem = new ToxinCollisionSystem(EntitySystem, couldParallelize);
        unneededCompoundVentingSystem = new UnneededCompoundVentingSystem(cloudSystem, EntitySystem, parallelRunner);

        // Systems stored in properties
        CameraFollowSystem = new CameraFollowSystem(EntitySystem);

        ProcessSystem = new ProcessSystem(EntitySystem, parallelRunner);

        TimedLifeSystem = new TimedLifeSystem(this, EntitySystem, couldParallelize);

        microbeReproductionSystem = new MicrobeReproductionSystem(this, SpawnSystem, EntitySystem, parallelRunner);
        microbeDeathSystem = new MicrobeDeathSystem(this, SpawnSystem, EntitySystem, couldParallelize);
        engulfingSystem = new EngulfingSystem(this, SpawnSystem, EntitySystem);

        delayedColonyOperationSystem =
            new DelayedColonyOperationSystem(this, SpawnSystem, EntitySystem, couldParallelize);
        multicellularGrowthSystem = new MulticellularGrowthSystem(this, SpawnSystem, EntitySystem, parallelRunner);

        CloudSystem = cloudSystem;

        cellCountingEntitySet = EntitySystem.GetEntities().With<CellProperties>().AsSet();

        physics.RemoveGravity();

        OnInitialized();

        // In case this is loaded from a save ensure the next save has correct ignore entities
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
        microbeReproductionSystem.SetWorld(currentGame.GameWorld);
        microbeDeathSystem.SetWorld(currentGame.GameWorld);
        multicellularGrowthSystem.SetWorld(currentGame.GameWorld);
        engulfingSystem.SetWorld(currentGame.GameWorld);
        engulfedDigestionSystem.SetWorld(currentGame.GameWorld);

        CloudSystem.Init(fluidCurrentsSystem);
    }

    public override void ProcessFrameLogic(float delta)
    {
        ThrowIfNotInitialized();
        OnProcessFrameLogic(delta);
    }

    public void SetSimulationBiome(BiomeConditions biomeConditions)
    {
        ProcessSystem.SetBiome(biomeConditions);
    }

    /// <summary>
    ///   Clears system data that has been stored based on the player location. Call this when the player changes
    ///   locations a lot by respawning or by moving patches
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
        var taskExecutor = TaskExecutor.Instance;

        entitySignalingSystem = new EntitySignalingSystem(EntitySystem, taskExecutor);
        fluidCurrentsSystem = new FluidCurrentsSystem(EntitySystem, taskExecutor);

        SpawnSystem = new SpawnSystem(this);
    }

    protected override void OnEntityDestroyed(in Entity entity)
    {
        base.OnEntityDestroyed(in entity);

        // This doesn't seem to be required as the bodies are fully destroyed anyway so they can't record anything
        // physicsCollisionManagementSystem.OnEntityDestroyed(entity);
        physicsBodyDisablingSystem.OnEntityDestroyed(entity);
        physicsBodyCreationSystem.OnEntityDestroyed(entity);
        physicsSensorSystem.OnEntityDestroyed(entity);

        engulfingSystem.OnEntityDestroyed(entity);
        colonyStatsUpdateSystem.OnEntityDestroyed(entity);
    }

    protected override void OnPlayerPositionSet(Vector3 playerPosition)
    {
        // Immediately report to some systems
        countLimitedDespawnSystem.ReportPlayerPosition(playerPosition);
        soundEffectSystem.ReportPlayerPosition(playerPosition);
        SpawnSystem.ReportPlayerPosition(playerPosition);

        // Report to the kind of external clouds system as this simplifies code using the simulation
        CloudSystem.ReportPlayerPosition(playerPosition);
    }

    protected override int EstimateThreadsUtilizedBySystems()
    {
        var estimateCellCount = cellCountingEntitySet.Count;

        return 1 + estimateCellCount / Constants.SIMULATION_CELLS_PER_THREAD_ESTIMATE;
    }

    protected override void Dispose(bool disposing)
    {
        WaitForStartedPhysicsRun();

        if (disposing)
        {
            nonParallelRunner.Dispose();

            // If disposed before Init is called problems will happen without this check. This happens for example if
            // loading a save made in the editor and quitting the game without exiting the editor first to the microbe
            // stage.
            if (animationControlSystem != null!)
            {
                animationControlSystem.Dispose();
                attachedEntityPositionSystem.Dispose();
                colourAnimationSystem.Dispose();
                countLimitedDespawnSystem.Dispose();
                damageCooldownSystem.Dispose();
                damageOnTouchSystem.Dispose();
                disallowPlayerBodySleepSystem.Dispose();
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
                engulfedDigestionSystem.Dispose();
                engulfedHandlingSystem.Dispose();
                engulfingSystem.Dispose();
                entitySignalingSystem.Dispose();
                fluidCurrentsSystem.Dispose();
                microbeAI.Dispose();
                microbeCollisionSoundSystem.Dispose();
                microbeDeathSystem.Dispose();
                microbeEmissionSystem.Dispose();
                microbeEventCallbackSystem.Dispose();
                microbeFlashingSystem.Dispose();
                microbeMovementSoundSystem.Dispose();
                microbeMovementSystem.Dispose();
                microbeShaderSystem.Dispose();
                microbeVisualsSystem.Dispose();
                organelleComponentFetchSystem.Dispose();
                organelleTickSystem.Dispose();
                osmoregulationAndHealingSystem.Dispose();
                pilusDamageSystem.Dispose();
                slimeSlowdownSystem.Dispose();
                microbePhysicsCreationAndSizeSystem.Dispose();
                microbeRenderPrioritySystem.Dispose();
                microbeReproductionSystem.Dispose();
                tintColourApplyingSystem.Dispose();
                toxinCollisionSystem.Dispose();
                unneededCompoundVentingSystem.Dispose();
                delayedColonyOperationSystem.Dispose();
                multicellularGrowthSystem.Dispose();

                CameraFollowSystem.Dispose();
                ProcessSystem.Dispose();
                TimedLifeSystem.Dispose();

                cellCountingEntitySet.Dispose();
            }

            if (SpawnSystem != null!)
                SpawnSystem.Dispose();

            DisposeGenerated();
        }

        base.Dispose(disposing);
    }
}
