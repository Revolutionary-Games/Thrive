using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   Benchmarking tool for the microbe stage. Used for checking performance impact of changes or for players to see
///   how fast their computer is compared to other ones.
/// </summary>
public partial class MicrobeBenchmark : BenchmarkBase
{
    // Benchmark configuration, should only be changed if there's really important reasons as then older benchmark
    // results are no longer comparable
    private const int FIRST_PHASE_CELL_COUNT = 150;
    private const int SPECIES_COUNT = 10;
    private const float MAX_SPAWN_DISTANCE = 110;
    private const float SPAWN_DISTANCE_INCREMENT = 1.8f;

    // The starting spawn interval
    private const float SPAWN_INTERVAL = 0.121f;
    private const float SPAWN_INTERVAL_REDUCE_EVERY_N = 75;
    private const float SPAWN_INTERVAL_REDUCE_AMOUNT = 0.0005f;
    private const float MIN_SPAWN_INTERVAL = 0.005f;

    private const double SPAWN_ANGLE_INCREMENT = MathUtils.FULL_CIRCLE * 0.127f;
    private const float GLUCOSE_CLOUD_AMOUNT = 20000;
    private const float AMMONIA_PHOSPHATE_CLOUD_AMOUNT = 10000;
    private const float AI_FIGHT_TIME = 30;

    private const int TARGET_FPS_FOR_SPAWNING = 60;
    private const float STRESS_TEST_END_THRESHOLD = 9;
    private const float STRESS_TEST_THRESHOLD_REDUCE_EVERY_N = 120;
    private const float STRESS_TEST_THRESHOLD_REDUCE = 0.07f;
    private const float STRESS_TEST_END_THRESHOLD_MIN = 0.5f;

    private const int STRESS_TEST_ABSOLUTE_END = 2500;

    private const float MAX_WAIT_TIME_FOR_MICROBE_DEATH = 130;
    private const int REMAINING_MICROBES_THRESHOLD = 40;

    private readonly List<Entity> spawnedMicrobes = new();
    private readonly List<Species> generatedSpecies = new();

#pragma warning disable CA2213
    [Export]
    private Label microbesCountLabel = null!;

    [Export]
    private Node worldRoot = null!;

    [Export]
    private Node dynamicRoot = null!;

    [Export]
    private MicrobeCamera benchmarkCamera = null!;

    private CompoundCloudSystem? cloudSystem;
#pragma warning restore CA2213

    /// <summary>
    ///   Dummy environment to use. Can't be loaded here as it depends on simulation parameters.
    /// </summary>
    private IMicrobeSpawnEnvironment dummyEnvironment = null!;

    private GameWorld? world;
    private GameProperties? gameProperties;

    private MicrobeWorldSimulation? microbeSimulation;

    private EntitySet? microbeEntities;

    private int aiGroup1Seed;
    private int aiGroup2Seed;

    private bool preventDying;

    private int spawnCounter;
    private double spawnAngle;
    private float spawnDistance;
    private bool spawnedSomething;

    private float microbeStationaryResult;
    private float microbeAIResult;
    private float microbeStressTestResult;
    private float microbeStressTestAverageFPS;
    private float microbeStressTestMinFPS;
    private int aliveStressTestMicrobes;
    private float microbeDeathResult;
    private float microbeDeathMinFPS;
    private int remainingMicrobesAtEnd;

    // This used to be an int so hopefully this doesn't change things by being an int now
    protected override long RandomSeed => 256345461;

    // Quite good new seed with small cells
    // protected override long RandomSeed = 256345461;

    // Quite lively activity
    // protected override long RandomSeed = 986586944;

    // Quite good but a lot of early dying and then inactivity due to taking ATP damage
    // protected override long RandomSeed = 653564247;

    // Quite good but has slime jets
    // protected override long RandomSeed = 653564254;

    // Old seed, no longer good with the new mutation algorithm
    // protected override long RandomSeed = 256345464;

    protected override int Version => 1;

    protected override int TotalSteps => 5;

    public override void _Ready()
    {
        dummyEnvironment = new DummyMicrobeSpawnEnvironment();

        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        microbesCountLabel.Text = spawnedMicrobes.Count.ToString(CultureInfo.CurrentCulture);

        benchmarkCamera.UpdateCameraPosition(delta, Vector3.Zero);

        if (spawnedSomething)
            CheckSpawnedMicrobes();

        PruneDeadMicrobes();

        if (preventDying)
        {
            // Force health back up for cells to prevent them from dying
            foreach (var entityReference in spawnedMicrobes)
            {
                ref var health = ref entityReference.Get<Health>();

                health.CurrentHealth = health.MaxHealth;
            }
        }

        microbeEntities?.Complete();

        microbeSimulation?.ProcessAll((float)delta);

        switch (internalPhaseCounter)
        {
            case 1:
            {
                // Need to pass some time between each spawn
                if (timer < SPAWN_INTERVAL)
                    break;

                timer = 0;

                // Spawn cells up to FIRST_PHASE_CELL_COUNT and then measure FPS
                if (spawnedMicrobes.Count < FIRST_PHASE_CELL_COUNT)
                {
                    SpawnAndUpdatePositionState();
                }
                else
                {
                    // Done spawning
                    IncrementPhase();
                }

                break;
            }

            case 2:
            {
                WaitForStableFPS();
                break;
            }

            case 3:
            {
                if (MeasureFPS())
                {
                    microbeStationaryResult = ScoreFromMeasuredFPS();
                    IncrementPhase();
                }

                break;
            }

            case 4:
            {
                // Enable AI
                microbeSimulation!.RunAI = true;
                microbeSimulation.OverrideMicrobeAIRandomSeed(aiGroup1Seed);

                IncrementPhase();
                break;
            }

            case 5:
            {
                if (timer > AI_FIGHT_TIME)
                    IncrementPhase();
                break;
            }

            case 6:
            {
                // And measure FPS with AI enabled
                if (MeasureFPS())
                {
                    microbeAIResult = ScoreFromMeasuredFPS();
                    IncrementPhase();
                }

                break;
            }

            case 7:
            {
                // New phase: spawn cells until FPS is no longer 60
                preventDying = false;
                spawnCounter = 0;

                microbeSimulation!.DestroyAllEntities();
                spawnedMicrobes.Clear();
                cloudSystem!.EmptyAllClouds();

                spawnAngle = 0;
                spawnDistance = 1;
                microbeSimulation.OverrideMicrobeAIRandomSeed(aiGroup2Seed);

                IncrementPhase();
                break;
            }

            case 8:
            {
                // Just to stabilize after the world clear
                WaitForStableFPS();
                break;
            }

            case 9:
            {
                // Spawn cells and measure FPS constantly
                float interval = Math.Max(MIN_SPAWN_INTERVAL,
                    SPAWN_INTERVAL - spawnCounter / SPAWN_INTERVAL_REDUCE_EVERY_N * SPAWN_INTERVAL_REDUCE_AMOUNT);
                if (timer < interval)
                    break;

                timer = 0;

                if (Engine.GetFramesPerSecond() >= TARGET_FPS_FOR_SPAWNING)
                {
                    SpawnAndUpdatePositionState();
                }

                fpsValues.Add(Engine.GetFramesPerSecond());

                float endThreshold = Math.Max(STRESS_TEST_END_THRESHOLD_MIN,
                    STRESS_TEST_END_THRESHOLD - spawnCounter / STRESS_TEST_THRESHOLD_REDUCE_EVERY_N *
                    STRESS_TEST_THRESHOLD_REDUCE);

                // Quit if it has been a while since the last spawn or there's been way too much data already
                if ((timeSinceSpawn > endThreshold && fpsValues.Count > 0) ||
                    fpsValues.Count > STRESS_TEST_ABSOLUTE_END)
                {
                    microbeStressTestResult = spawnCounter;
                    microbeStressTestMinFPS = (float)fpsValues.Min();
                    aliveStressTestMicrobes = spawnedMicrobes.Count;
                    microbeStressTestAverageFPS = ScoreFromMeasuredFPS();
                    IncrementPhase();
                }

                break;
            }

            case 10:
            {
                // Waiting for microbes to die phase
                if (timer < 0.5f)
                    break;

                timer = 0;
                fpsValues.Add(Engine.GetFramesPerSecond());

                // End this phase when either there aren't many microbes left or a few minutes have elapsed
                if (spawnedMicrobes.Count > REMAINING_MICROBES_THRESHOLD &&
                    fpsValues.Count * 0.5f < MAX_WAIT_TIME_FOR_MICROBE_DEATH)
                {
                    break;
                }

                // Don't allow ending before at least some FPS samples are captured
                if (fpsValues.Count < 50)
                    break;

                // Microbes are basically dead now

                microbeDeathResult = ScoreFromMeasuredFPS();
                microbeDeathMinFPS = (float)fpsValues.Min();
                remainingMicrobesAtEnd = spawnedMicrobes.Count;

                IncrementPhase();
                break;
            }

            case 11:
            {
                // Benchmark ended
                OnBenchmarkEnded();
                break;
            }
        }
    }

    protected override void OnBenchmarkStarted()
    {
        // TODO: if the benchmark ever supports restarting, then cleanup of existing objects needs to be
        // performed here

        GenerateWorldAndSpecies();
        SetupSimulation();

        if (microbeSimulation == null)
            throw new InvalidOperationException("Microbe sim not setup");

        spawnAngle = 0;
        spawnDistance = 1;
        microbeSimulation.RunAI = false;
        preventDying = true;
    }

    protected override void OnBenchmarkStateReset()
    {
        microbeStationaryResult = 0;
        microbeAIResult = 0;
        microbeStressTestResult = 0;
        microbeStressTestAverageFPS = 0;
        microbeStressTestMinFPS = 0;
        aliveStressTestMicrobes = 0;
        microbeDeathResult = 0;
        microbeDeathMinFPS = 0;
        remainingMicrobesAtEnd = 0;
    }

    protected override int GetDisplayStep(int internalStepNumber)
    {
        return internalStepNumber switch
        {
            <= 3 => 1,
            <= 6 => 2,
            <= 9 => 3,
            <= 10 => 4,
            _ => 5,
        };
    }

    protected override string GenerateResultsText(int scoreDecimals = 2)
    {
        var builder = new StringBuilder();

        // This is intentionally not translatable to make it easier for developers to request this info from players and
        // then understand it
        if (microbeStationaryResult != 0)
        {
            builder.Append("Stationary microbes score: ");
            builder.Append(Math.Round(microbeStationaryResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (microbeAIResult != 0)
        {
            builder.Append("AI microbes score: ");
            builder.Append(Math.Round(microbeAIResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (microbeStressTestResult != 0)
        {
            builder.Append($"Spawns until no {TARGET_FPS_FOR_SPAWNING} FPS: ");
            builder.Append(Math.Round(microbeStressTestResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (microbeStressTestAverageFPS != 0)
        {
            builder.Append("Microbe stress average FPS: ");
            builder.Append(
                Math.Round(microbeStressTestAverageFPS, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (microbeStressTestMinFPS != 0)
        {
            builder.Append("Microbe stress min FPS: ");
            builder.Append(Math.Round(microbeStressTestMinFPS, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (aliveStressTestMicrobes != 0)
        {
            builder.Append("Alive microbes: ");
            builder.Append(aliveStressTestMicrobes.ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (microbeDeathResult != 0)
        {
            builder.Append("Waiting for microbes to die: ");
            builder.Append(Math.Round(microbeDeathResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (microbeDeathMinFPS != 0)
        {
            builder.Append("Microbe deaths minimum FPS: ");
            builder.Append(Math.Round(microbeDeathMinFPS, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (remainingMicrobesAtEnd != 0)
        {
            builder.Append("Remaining microbes: ");
            builder.Append(remainingMicrobesAtEnd.ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        AddTestDurationToResult(builder);

        AddResultHardwareInfo(builder);

        // TODO: overall score?

        return builder.ToString();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            microbeSimulation?.Dispose();
            microbeEntities?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void GenerateWorldAndSpecies()
    {
        gameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
        world = new GameWorld();

        generatedSpecies.Clear();

        aiGroup1Seed = random.Next();
        aiGroup2Seed = random.Next();

        var nameGenerator = SimulationParameters.Instance.NameGenerator;
        var workMemory = new MutationWorkMemory();

        for (int i = 0; i < SPECIES_COUNT; ++i)
        {
            var species = CommonMutationFunctions.GenerateRandomSpecies(world.NewMicrobeSpecies(
                    nameGenerator.GenerateNameSection(random),
                    nameGenerator.GenerateNameSection(random, true)),
                workMemory, random, random.Next(200, 500));

            generatedSpecies.Add(species);
        }
    }

    private void SetupSimulation()
    {
        cloudSystem = new CompoundCloudSystem();
        worldRoot.AddChild(cloudSystem);

        microbeSimulation = new MicrobeWorldSimulation();
        microbeSimulation.Init(dynamicRoot, cloudSystem, dummyEnvironment);
        microbeSimulation.InitForCurrentGame(gameProperties ?? throw new Exception("game properties not set"));

        microbeEntities = microbeSimulation.EntitySystem.GetEntities().With<MicrobeSpeciesMember>().With<Health>()
            .AsSet();

        microbeSimulation.SetSimulationBiome(dummyEnvironment.CurrentBiome);
    }

    private void SpawnAndUpdatePositionState()
    {
        var position = new Vector3((float)Math.Cos(spawnAngle), 0, (float)-Math.Sin(spawnAngle)) *
            spawnDistance;

        SpawnMicrobe(position);

        // Spawn in a kind of repeating spiral pattern
        spawnAngle += SPAWN_ANGLE_INCREMENT;
        spawnDistance += SPAWN_DISTANCE_INCREMENT;

        while (spawnDistance > MAX_SPAWN_DISTANCE)
        {
            spawnDistance -= MAX_SPAWN_DISTANCE;
        }

        timeSinceSpawn = 0;
    }

    private void SpawnMicrobe(Vector3 position)
    {
        SpawnHelpers.SpawnMicrobe(microbeSimulation!, dummyEnvironment,
            generatedSpecies[spawnCounter % generatedSpecies.Count], position, true);

        spawnedSomething = true;
        ++spawnCounter;

        // Spawning also gives a glucose cloud to ensure the spawned microbe doesn't instantly just die
        cloudSystem!.AddCloud(Compound.Glucose, GLUCOSE_CLOUD_AMOUNT, position);

        // And a bit of phosphate or ammonia
        cloudSystem!.AddCloud(random.Next(0, 2) == 1 ? Compound.Phosphates : Compound.Ammonia,
            AMMONIA_PHOSPHATE_CLOUD_AMOUNT, position);
    }

    private void CheckSpawnedMicrobes()
    {
        // Find the spawned microbes. This needs to be done separately from SpawnMicrobe because they are only queued
        // spawns at that point
        foreach (var existingMicrobe in microbeEntities!.GetEntities())
        {
            if (existingMicrobe.Get<Health>().Dead)
                continue;

            if (spawnedMicrobes.Any(m => m == existingMicrobe))
                continue;

            spawnedMicrobes.Add(existingMicrobe);
        }

        spawnedSomething = false;
    }

    private void PruneDeadMicrobes()
    {
        spawnedMicrobes.RemoveAll(r => !r.IsAlive || r.Get<Health>().Dead);
    }
}
