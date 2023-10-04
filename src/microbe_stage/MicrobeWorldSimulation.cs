using DefaultEcs.Threading;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Contains all the parts needed to simulate a microbial world. Separate from (but used by) the
///   <see cref="MicrobeStage"/> to also allow other parts of the code to easily run a microbe simulation
/// </summary>
public class MicrobeWorldSimulation : WorldSimulationWithPhysics
{
    private readonly IParallelRunner nonParallelRunner = new DefaultParallelRunner(1);

    // TODO: remove if this doesn't turn out to be useful
    private GameProperties gameProperties = null!;

    // Base systems
    private AttachedEntityPositionSystem attachedEntityPositionSystem = null!;
    private ColourAnimationSystem colourAnimationSystem = null!;
    private CountLimitedDespawnSystem countLimitedDespawnSystem = null!;
    private DamageOnTouchSystem damageOnTouchSystem = null!;
    private DisallowPlayerBodySleepSystem disallowPlayerBodySleepSystem = null!;
    private EntityMaterialFetchSystem entityMaterialFetchSystem = null!;
    private FadeOutActionSystem fadeOutActionSystem = null!;
    private PathBasedSceneLoader pathBasedSceneLoader = null!;
    private PhysicsBodyControlSystem physicsBodyControlSystem = null!;
    private PhysicsBodyCreationSystem physicsBodyCreationSystem = null!;
    private PhysicsBodyDisablingSystem physicsBodyDisablingSystem = null!;
    private PhysicsCollisionManagementSystem physicsCollisionManagementSystem = null!;
    private PhysicsUpdateAndPositionSystem physicsUpdateAndPositionSystem = null!;
    private PredefinedVisualLoaderSystem predefinedVisualLoaderSystem = null!;

    // private RenderOrderSystem renderOrderSystem = null! = null!;

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
    private EntitySignalingSystem entitySignalingSystem = null!;
    private FluidCurrentsSystem fluidCurrentsSystem = null!;
    private MicrobeAISystem microbeAI = null!;
    private MicrobeCollisionSoundSystem microbeCollisionSoundSystem = null!;
    private MicrobeDeathSystem microbeDeathSystem = null!;
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
    private MicrobeReproductionSystem microbeReproductionSystem = null!;
    private TintColourAnimationSystem tintColourAnimationSystem = null!;
    private ToxinCollisionSystem toxinCollisionSystem = null!;

#pragma warning disable CA2213
    private Node visualsParent = null!;
#pragma warning restore CA2213

    // External system references

    [JsonIgnore]
    public CompoundCloudSystem CloudSystem { get; private set; } = null!;

    // Systems accessible to the outside as these have some very specific methods to be called on them
    [JsonIgnore]
    public CameraFollowSystem CameraFollowSystem { get; private set; } = null!;

    // TODO: check that
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
        visualsParent = visualDisplayRoot;

        // TODO: add threading
        var parallelRunner = new DefaultParallelRunner(1);

        // Systems stored in fields
        attachedEntityPositionSystem = new AttachedEntityPositionSystem(EntitySystem, parallelRunner);
        colourAnimationSystem = new ColourAnimationSystem(EntitySystem, parallelRunner);
        countLimitedDespawnSystem = new CountLimitedDespawnSystem(this, EntitySystem, parallelRunner);
        damageOnTouchSystem = new DamageOnTouchSystem(this, EntitySystem, parallelRunner);
        disallowPlayerBodySleepSystem = new DisallowPlayerBodySleepSystem(physics, EntitySystem);
        entityMaterialFetchSystem = new EntityMaterialFetchSystem(EntitySystem, nonParallelRunner);
        fadeOutActionSystem = new FadeOutActionSystem(EntitySystem, parallelRunner);
        pathBasedSceneLoader = new PathBasedSceneLoader(EntitySystem, nonParallelRunner);
        physicsBodyControlSystem = new PhysicsBodyControlSystem(physics, EntitySystem, parallelRunner);
        physicsBodyCreationSystem = new PhysicsBodyCreationSystem(this, null, EntitySystem, nonParallelRunner);
        physicsBodyDisablingSystem = new PhysicsBodyDisablingSystem(physics, EntitySystem);
        physicsCollisionManagementSystem = new PhysicsCollisionManagementSystem(physics, EntitySystem, parallelRunner);
        physicsUpdateAndPositionSystem = new PhysicsUpdateAndPositionSystem(physics, EntitySystem, parallelRunner);
        predefinedVisualLoaderSystem = new PredefinedVisualLoaderSystem(EntitySystem, nonParallelRunner);

        // TODO: different root for sounds?
        soundEffectSystem = new SoundEffectSystem(visualsParent, EntitySystem);
        soundListenerSystem = new SoundListenerSystem(visualsParent, EntitySystem, parallelRunner);
        spatialAttachSystem = new SpatialAttachSystem(visualsParent, EntitySystem);
        spatialPositionSystem = new SpatialPositionSystem(EntitySystem, parallelRunner);

        allCompoundsVentingSystem = new AllCompoundsVentingSystem(cloudSystem, this, EntitySystem, parallelRunner);
        cellBurstEffectSystem = new CellBurstEffectSystem(EntitySystem);

        colonyBindingSystem = new ColonyBindingSystem(this, EntitySystem, parallelRunner);
        colonyCompoundDistributionSystem = new ColonyCompoundDistributionSystem(EntitySystem, parallelRunner);
        colonyStatsUpdateSystem = new ColonyStatsUpdateSystem(EntitySystem, parallelRunner);

        // TODO: clouds currently only allow 2 thread to absorb at once
        compoundAbsorptionSystem = new CompoundAbsorptionSystem(cloudSystem, EntitySystem, parallelRunner);

        damageSoundSystem = new DamageSoundSystem(EntitySystem, parallelRunner);
        engulfedDigestionSystem = new EngulfedDigestionSystem(cloudSystem, EntitySystem, parallelRunner);
        engulfedHandlingSystem = new EngulfedHandlingSystem(EntitySystem, parallelRunner);
        engulfingSystem = new EngulfingSystem(this, EntitySystem);
        entitySignalingSystem = new EntitySignalingSystem(EntitySystem, parallelRunner);

        fluidCurrentsSystem = new FluidCurrentsSystem(EntitySystem, parallelRunner);

        microbeMovementSystem = new MicrobeMovementSystem(PhysicalWorld, EntitySystem, parallelRunner);

        // TODO: this definitely needs to be (along with the process system) the first systems to be multithreaded
        microbeAI = new MicrobeAISystem(cloudSystem, EntitySystem, parallelRunner);
        microbeCollisionSoundSystem = new MicrobeCollisionSoundSystem(EntitySystem, parallelRunner);
        microbeDeathSystem = new MicrobeDeathSystem(EntitySystem, parallelRunner);
        microbeEventCallbackSystem = new MicrobeEventCallbackSystem(cloudSystem, EntitySystem, parallelRunner);
        microbeFlashingSystem = new MicrobeFlashingSystem(EntitySystem, parallelRunner);
        microbeMovementSoundSystem = new MicrobeMovementSoundSystem(EntitySystem, parallelRunner);
        microbeShaderSystem = new MicrobeShaderSystem(EntitySystem);

        microbeVisualsSystem = new MicrobeVisualsSystem(EntitySystem);
        organelleComponentFetchSystem = new OrganelleComponentFetchSystem(EntitySystem, parallelRunner);
        organelleTickSystem = new OrganelleTickSystem(EntitySystem, parallelRunner);
        osmoregulationAndHealingSystem = new OsmoregulationAndHealingSystem(EntitySystem, parallelRunner);
        pilusDamageSystem = new PilusDamageSystem(EntitySystem, parallelRunner);
        slimeSlowdownSystem = new SlimeSlowdownSystem(cloudSystem, EntitySystem, parallelRunner);
        microbePhysicsCreationAndSizeSystem = new MicrobePhysicsCreationAndSizeSystem(EntitySystem, parallelRunner);
        tintColourAnimationSystem = new TintColourAnimationSystem(EntitySystem);

        toxinCollisionSystem = new ToxinCollisionSystem(EntitySystem, parallelRunner);

        // Systems stored in properties
        CameraFollowSystem = new CameraFollowSystem(EntitySystem);

        ProcessSystem = new ProcessSystem(EntitySystem, parallelRunner);

        TimedLifeSystem = new TimedLifeSystem(this, EntitySystem, parallelRunner);

        SpawnSystem = new SpawnSystem(this);

        microbeReproductionSystem = new MicrobeReproductionSystem(this, SpawnSystem, EntitySystem, parallelRunner);

        CloudSystem = cloudSystem;
        cloudSystem.Init(fluidCurrentsSystem);

        OnInitialized();
    }

    /// <summary>
    ///   Second phase initialization that requires access to the current game info
    /// </summary>
    /// <param name="currentGame">Currently started game</param>
    public void InitForCurrentGame(GameProperties currentGame)
    {
        gameProperties = currentGame;

        osmoregulationAndHealingSystem.SetWorld(gameProperties.GameWorld);
        microbeReproductionSystem.SetWorld(gameProperties.GameWorld);
    }

    public override void ProcessFrameLogic(float delta)
    {
        ThrowIfNotInitialized();

        colourAnimationSystem.Update(delta);
        microbeShaderSystem.Update(delta);
        tintColourAnimationSystem.Update(delta);
    }

    public void SetSimulationBiome(BiomeConditions biomeConditions)
    {
        ProcessSystem.SetBiome(biomeConditions);
    }

    public override void ReportPlayerPosition(Vector3 position)
    {
        // TODO: reporting the player position to all systems on game load

        base.ReportPlayerPosition(position);

        // Immediately report to some systems
        countLimitedDespawnSystem.ReportPlayerPosition(position);
        soundEffectSystem.ReportPlayerPosition(position);
        SpawnSystem.ReportPlayerPosition(position);

        // Report to the kind of external clouds system as this simplifies code using the simulation
        CloudSystem.ReportPlayerPosition(position);
    }

    /// <summary>
    ///   Clears system data that has been stored based on the player location. Call this when the player changes
    ///   locations a lot by respawning or by moving patches
    /// </summary>
    public void ClearPlayerLocationDependentCaches()
    {
        SpawnSystem.ClearSpawnCoordinates();
    }

    internal void OverrideMicrobeAIRandomSeed(int seed)
    {
        microbeAI.OverrideAIRandomSeed(seed);
    }

    protected override void OnProcessFixedLogic(float delta)
    {
        TimedLifeSystem.Update(delta);

        microbeVisualsSystem.Update(delta);
        pathBasedSceneLoader.Update(delta);
        predefinedVisualLoaderSystem.Update(delta);
        entityMaterialFetchSystem.Update(delta);

        microbePhysicsCreationAndSizeSystem.Update(delta);
        physicsBodyCreationSystem.Update(delta);
        physicsBodyDisablingSystem.Update(delta);

        physicsCollisionManagementSystem.Update(delta);

        physicsUpdateAndPositionSystem.Update(delta);
        attachedEntityPositionSystem.Update(delta);

        fluidCurrentsSystem.Update(delta);

        engulfingSystem.Update(delta);
        engulfedDigestionSystem.Update(delta);
        engulfedHandlingSystem.Update(delta);

        colonyBindingSystem.Update(delta);

        spatialAttachSystem.Update(delta);
        spatialPositionSystem.Update(delta);

        allCompoundsVentingSystem.Update(delta);
        compoundAbsorptionSystem.Update(delta);
        entitySignalingSystem.Update(delta);

        toxinCollisionSystem.Update(delta);
        damageOnTouchSystem.Update(delta);
        pilusDamageSystem.Update(delta);

        ProcessSystem.Update(delta);

        colonyCompoundDistributionSystem.Update(delta);

        osmoregulationAndHealingSystem.Update(delta);

        microbeReproductionSystem.Update(delta);
        organelleComponentFetchSystem.Update(delta);

        if (RunAI)
        {
            // Update AI for the cells (note that the AI system itself can also be disabled, due to cheats)
            microbeAI.ReportPotentialPlayerPosition(reportedPlayerPosition);
            microbeAI.Update(delta);
        }

        countLimitedDespawnSystem.Update(delta);

        SpawnSystem.Update(delta);

        colonyStatsUpdateSystem.Update(delta);

        microbeEventCallbackSystem.Update(delta);

        microbeDeathSystem.Update(delta);

        disallowPlayerBodySleepSystem.Update(delta);

        slimeSlowdownSystem.Update(delta);
        microbeMovementSystem.Update(delta);
        microbeMovementSoundSystem.Update(delta);

        organelleTickSystem.Update(delta);

        fadeOutActionSystem.Update(delta);
        physicsBodyControlSystem.Update(delta);

        // renderOrderSystem.Update(delta);

        cellBurstEffectSystem.Update(delta);

        microbeFlashingSystem.Update(delta);

        damageSoundSystem.Update(delta);
        microbeCollisionSoundSystem.Update(delta);
        soundEffectSystem.Update(delta);

        soundListenerSystem.Update(delta);

        // This needs to be here to not visually jitter the player position
        CameraFollowSystem.Update(delta);

        reportedPlayerPosition = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            nonParallelRunner.Dispose();

            attachedEntityPositionSystem.Dispose();
            colourAnimationSystem.Dispose();
            countLimitedDespawnSystem.Dispose();
            damageOnTouchSystem.Dispose();
            disallowPlayerBodySleepSystem.Dispose();
            entityMaterialFetchSystem.Dispose();
            fadeOutActionSystem.Dispose();
            pathBasedSceneLoader.Dispose();
            physicsBodyControlSystem.Dispose();
            physicsBodyCreationSystem.Dispose();
            physicsBodyDisablingSystem.Dispose();
            physicsCollisionManagementSystem.Dispose();
            physicsUpdateAndPositionSystem.Dispose();
            predefinedVisualLoaderSystem.Dispose();
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
            microbeReproductionSystem.Dispose();
            tintColourAnimationSystem.Dispose();
            toxinCollisionSystem.Dispose();

            CameraFollowSystem.Dispose();
            ProcessSystem.Dispose();
            TimedLifeSystem.Dispose();
            SpawnSystem.Dispose();
        }

        base.Dispose(disposing);
    }
}
