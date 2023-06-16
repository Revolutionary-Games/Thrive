using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Contains all the parts needed to simulate a microbial world. Separate from (but used by) the
///   <see cref="MicrobeStage"/> to also allow other parts of the code to easily run a microbe simulation
/// </summary>
public class MicrobeWorldSimulation : WorldSimulationWithPhysics
{
    private Random aiRandom = new();

    // TODO: investigate using an ECS to make these systems easier to run concurrently with more clearly separated
    // components
    private FluidSystem fluidSystem = null!;
    private MicrobeSystem microbeSystem = null!;
    private ProcessSystem processSystem = null!;
    private MicrobeAISystem microbeAI = null!;
    private FloatingChunkSystem chunkSystem = null!;
    private TimedLifeSystem timedLifeSystem = null!;

    private Node visualsParent = null!;

    [JsonIgnore]
    public CompoundCloudSystem CloudSystem { get; private set; } = null!;

    public void Init(Node visualDisplayRoot, CompoundCloudSystem cloudSystem)
    {
        visualsParent = visualDisplayRoot;

        fluidSystem = new FluidSystem(this);

        CloudSystem = cloudSystem;
        cloudSystem.Init(fluidSystem);

        microbeSystem = new MicrobeSystem(this);
        microbeAI = new MicrobeAISystem(this, cloudSystem);

        processSystem = new ProcessSystem(this);

        chunkSystem = new FloatingChunkSystem(this, cloudSystem);

        timedLifeSystem = new TimedLifeSystem(this);

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
        aiRandom = new Random(seed);
    }

    protected override void OnProcessFixedLogic(float delta)
    {
        fluidSystem.Process(delta);
        fluidSystem.PhysicsProcess(delta);

        processSystem.Process(delta);

        microbeSystem.Process(delta);

        chunkSystem.Process(delta, PlayerPosition);
        timedLifeSystem.Process(delta);

        if (RunAI)
        {
            // Update AI for the cells
            microbeAI.Process(delta, aiRandom);
        }
    }
}
