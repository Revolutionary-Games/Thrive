using System;
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
    private Random aiRandom = new();

    // TODO: allow saving / loading some system state somehow (hopefully without needing to add really hacky dummy
    // constructors)
    private FluidCurrentsSystem fluidCurrentsSystem = null!;
    private ProcessSystem processSystem = null!;
    private MicrobeAISystem microbeAI = null!;
    private TimedLifeSystem timedLifeSystem = null!;

    // need to merge  Keep AI from shooting while facing the wrong direction #4435  and update from master before continuing

    // TODO: re-add the spawn system
    // [JsonProperty]
    // [AssignOnlyChildItemsOnDeserialize]
    private SpawnSystem spawner = null!;

#pragma warning disable CA2213
    private Node visualsParent = null!;
#pragma warning restore CA2213

    [JsonIgnore]
    public CompoundCloudSystem CloudSystem { get; private set; } = null!;

    public void Init(Node visualDisplayRoot, CompoundCloudSystem cloudSystem)
    {
        visualsParent = visualDisplayRoot;

        // TODO: add threading
        var parallelRunner = new DefaultParallelRunner(1);
        fluidCurrentsSystem = new FluidCurrentsSystem(EntitySystem, parallelRunner);

        CloudSystem = cloudSystem;
        cloudSystem.Init(fluidCurrentsSystem);

        // microbeSystem = new MicrobeSystem(this);

        // TODO: this definitely needs to be (along with the process system) the first systems to be multithreaded
        microbeAI = new MicrobeAISystem(this, cloudSystem, EntitySystem, parallelRunner);

        processSystem = new ProcessSystem(EntitySystem, parallelRunner);

        timedLifeSystem = new TimedLifeSystem(this, EntitySystem, parallelRunner);

        spawner = new SpawnSystem(this);

        OnInitialized();
    }

    public override void ProcessFrameLogic(float delta)
    {
        ThrowIfNotInitialized();
    }

    public void SetSimulationBiome(BiomeConditions biomeConditions)
    {
        processSystem.SetBiome(biomeConditions);
    }

    internal void OverrideMicrobeAIRandomSeed(int seed)
    {
        microbeAI.OverrideAIRandomSeed(seed);
    }

    protected override void OnProcessFixedLogic(float delta)
    {
        fluidCurrentsSystem.Update(delta);

        processSystem.Update(delta);

        timedLifeSystem.Update(delta);

        if (RunAI)
        {
            // Update AI for the cells (note that the AI system itself can also be disabled, due to cheats)
            microbeAI.Update(delta);
        }

        spawner.Update(delta);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fluidCurrentsSystem.Dispose();
            processSystem.Dispose();
            timedLifeSystem.Dispose();
            spawner.Dispose();
        }

        base.Dispose(disposing);
    }
}
