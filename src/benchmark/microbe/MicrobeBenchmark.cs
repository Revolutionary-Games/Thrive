using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   Benchmarking tool for the microbe stage. Used for checking performance impact of changes or for players to see
///   how fast their computer is compared to other ones.
/// </summary>
public class MicrobeBenchmark : Node
{
    [Export]
    public NodePath? GUIContainerPath;

    [Export]
    public NodePath FPSLabelPath = null!;

    [Export]
    public NodePath PhaseLabelPath = null!;

    [Export]
    public NodePath MicrobesCountLabelPath = null!;

    [Export]
    public NodePath BenchmarkResultsTextPath = null!;

    [Export]
    public NodePath BenchmarkFinishedTextPath = null!;

    [Export]
    public NodePath CopyResultsButtonPath = null!;

    [Export]
    public NodePath WorldRootPath = null!;

    [Export]
    public NodePath DynamicRootPath = null!;

    // Benchmark configuration, should only be changed if there's really important reasons as then older benchmark
    // results are no longer comparable
    private const int FIRST_PHASE_CELL_COUNT = 150;
    private const int SPECIES_COUNT = 10;
    private const int MUTATION_STEPS_MIN = 3;
    private const int MUTATION_STEPS_MAX = 8;
    private const float AI_MUTATION_MULTIPLIER = 1;
    private const float MAX_SPAWN_DISTANCE = 110;
    private const float SPAWN_DISTANCE_INCREMENT = 1.8f;
    private const float SPAWN_INTERVAL = 0.121f;
    private const double SPAWN_ANGLE_INCREMENT = MathUtils.FULL_CIRCLE * 0.127f;
    private const float GLUCOSE_CLOUD_AMOUNT = 20000;
    private const float AMMONIA_PHOSPHATE_CLOUD_AMOUNT = 10000;
    private const float AI_FIGHT_TIME = 30;
    private const int TARGET_FPS_FOR_SPAWNING = 60;
    private const float STRESS_TEST_END_THRESHOLD = 9;
    private const float MAX_WAIT_TIME_FOR_MICROBE_DEATH = 130;
    private const int REMAINING_MICROBES_THRESHOLD = 40;
    private const int RANDOM_SEED = 256345464;

    // Good other seeds:
    // Some big cells, no toxins, quite a lot of eating
    // private const int RANDOM_SEED = 256345461;

    // Big cells, toxins, quite slow to die at the end
    // private const int RANDOM_SEED = 256345463;

    /// <summary>
    ///   Increment this if the functionality of the benchmark changes considerably
    /// </summary>
    private const int VERSION = 1;

    private readonly BenchmarkHelpers.BenchmarkChangedSettingsStore storedSettings = new();

    // We don't register the tracking callback here as we disallow reproduction so cells can't divide and require
    // to be added
    private readonly DummySpawnSystem dummySpawnSystem = new();

    private readonly List<EntityReference<Microbe>> spawnedMicrobes = new();
    private readonly List<Species> generatedSpecies = new();

    private readonly List<float> fpsValues = new();

#pragma warning disable CA2213
    private CustomWindow guiContainer = null!;
    private Label fpsLabel = null!;
    private Label phaseLabel = null!;
    private Label microbesCountLabel = null!;
    private Label benchmarkResultsText = null!;
    private Control benchmarkFinishedText = null!;
    private Button copyResultsButton = null!;

    private Node worldRoot = null!;
    private Node dynamicRoot = null!;

    private PackedScene microbeScene = null!;

    private CompoundCloudSystem? cloudSystem;
#pragma warning restore CA2213

    private Compound glucose = null!;
    private Compound ammonia = null!;
    private Compound phosphates = null!;

    private GameWorld? world;
    private GameProperties? gameProperties;
    private FluidSystem? fluidSystem;
    private MicrobeSystem? microbeSystem;
    private ProcessSystem? processSystem;
    private MicrobeAISystem? microbeAI;
    private FloatingChunkSystem? chunkSystem;
    private TimedLifeSystem? timedLifeSystem;

    private Random random = new(RANDOM_SEED);
    private Random aiRandom = new();

    private int aiGroup1Seed;
    private int aiGroup2Seed;

    private bool preventDying;
    private bool runAI;

    private int internalPhaseCounter;
    private float timer;

    private int spawnCounter;
    private double spawnAngle;
    private float spawnDistance;
    private float timeSinceSpawn;

    private float microbeStationaryResult;
    private float microbeAIResult;
    private float microbeStressTestResult;
    private float microbeStressTestAverageFPS;
    private float microbeStressTestMinFPS;
    private int aliveStressTestMicrobes;
    private float microbeDeathResult;
    private float microbeDeathMinFPS;
    private int remainingMicrobesAtEnd;

    private DateTime startTime;
    private float totalDuration;

    private bool exiting;

    public override void _Ready()
    {
        guiContainer = GetNode<CustomWindow>(GUIContainerPath);
        fpsLabel = GetNode<Label>(FPSLabelPath);
        phaseLabel = GetNode<Label>(PhaseLabelPath);
        microbesCountLabel = GetNode<Label>(MicrobesCountLabelPath);
        benchmarkResultsText = GetNode<Label>(BenchmarkResultsTextPath);
        benchmarkFinishedText = GetNode<Control>(BenchmarkFinishedTextPath);
        copyResultsButton = GetNode<Button>(CopyResultsButtonPath);

        worldRoot = GetNode<Node>(WorldRootPath);
        dynamicRoot = GetNode<Node>(DynamicRootPath);

        microbeScene = SpawnHelpers.LoadMicrobeScene();

        guiContainer.Visible = true;
        benchmarkFinishedText.Visible = false;
        copyResultsButton.Visible = false;

        var simulationParameters = SimulationParameters.Instance;

        glucose = simulationParameters.GetCompound("glucose");
        ammonia = simulationParameters.GetCompound("ammonia");
        phosphates = simulationParameters.GetCompound("phosphates");

        StartBenchmark();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        BenchmarkHelpers.RestoreNormalSettings(storedSettings);
    }

    public override void _Process(float delta)
    {
        fpsLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
        microbesCountLabel.Text = spawnedMicrobes.Count.ToString(CultureInfo.CurrentCulture);

        timer += delta;
        timeSinceSpawn += delta;

        PruneDeadMicrobes();

        if (preventDying)
        {
            // Force health back up for cells to prevent them from dying
            foreach (var entityReference in spawnedMicrobes)
            {
                var microbe = entityReference.Value;

                microbe?.TestOverrideHitpoints(microbe.MaxHitpoints);
            }
        }

        processSystem?.Process(delta);

        // Run the microbe system to process microbes
        microbeSystem?.Process(delta);

        chunkSystem?.Process(delta, new Vector3(0, 0, 0));
        timedLifeSystem?.Process(delta);

        if (runAI)
        {
            // The AI thinking randomly adds some randomness
            // Update AI for the cells
            microbeAI?.Process(delta, aiRandom);
        }

        switch (internalPhaseCounter)
        {
            case 0:
            {
                // TODO: if the benchmark ever supports restarting, then cleanup of existing objects needs to be
                // performed here
                BenchmarkHelpers.PerformBenchmarkSetup(storedSettings);

                GenerateWorldAndSpecies();
                SetupEnoughGameSystemsToRun();

                spawnAngle = 0;
                spawnDistance = 1;
                runAI = false;
                preventDying = true;

                IncrementPhase();
                break;
            }

            case 1:
            {
                // TODO: remove debug code
                // break;

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
                runAI = true;
                aiRandom = new Random(aiGroup1Seed);

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

                dynamicRoot.FreeChildren();
                spawnedMicrobes.Clear();
                cloudSystem!.EmptyAllClouds();

                spawnAngle = 0;
                spawnDistance = 1;
                aiRandom = new Random(aiGroup2Seed);

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
                if (timer < SPAWN_INTERVAL)
                    break;

                timer = 0;

                if (Engine.GetFramesPerSecond() >= TARGET_FPS_FOR_SPAWNING)
                {
                    SpawnAndUpdatePositionState();
                }

                fpsValues.Add(Engine.GetFramesPerSecond());

                // Quit if it has been a while since the last spawn or there's been way too much data already
                if ((timeSinceSpawn > STRESS_TEST_END_THRESHOLD && fpsValues.Count > 0) || fpsValues.Count > 3000)
                {
                    microbeStressTestResult = spawnCounter;
                    microbeStressTestMinFPS = fpsValues.Min();
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
                microbeDeathMinFPS = fpsValues.Min();
                remainingMicrobesAtEnd = spawnedMicrobes.Count;

                IncrementPhase();
                break;
            }

            case 11:
            {
                // Benchmark ended
                totalDuration = (float)(DateTime.Now - startTime).TotalSeconds;

                IncrementPhase();
                break;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (GUIContainerPath != null)
            {
                GUIContainerPath.Dispose();
                FPSLabelPath.Dispose();
                PhaseLabelPath.Dispose();
                MicrobesCountLabelPath.Dispose();
                BenchmarkResultsTextPath.Dispose();
                BenchmarkFinishedTextPath.Dispose();
                CopyResultsButtonPath.Dispose();
                WorldRootPath.Dispose();
                DynamicRootPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void StartBenchmark()
    {
        internalPhaseCounter = 0;
        random = new Random(RANDOM_SEED);

        microbeStationaryResult = 0;
        microbeAIResult = 0;
        microbeStressTestResult = 0;
        microbeStressTestAverageFPS = 0;
        microbeStressTestMinFPS = 0;
        aliveStressTestMicrobes = 0;
        microbeDeathResult = 0;
        microbeDeathMinFPS = 0;
        remainingMicrobesAtEnd = 0;

        startTime = DateTime.Now;
        totalDuration = 0;

        UpdatePhaseLabel();
    }

    private void GenerateWorldAndSpecies()
    {
        gameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
        world = new GameWorld();

        generatedSpecies.Clear();

        aiGroup1Seed = random.Next();
        aiGroup2Seed = random.Next();

        var nameGenerator = SimulationParameters.Instance.NameGenerator;
        var mutator = new Mutations(random);

        for (int i = 0; i < SPECIES_COUNT; ++i)
        {
            var species = mutator.CreateRandomSpecies(world.NewMicrobeSpecies(nameGenerator.GenerateNameSection(random),
                    nameGenerator.GenerateNameSection(random, true)), AI_MUTATION_MULTIPLIER, false,
                random.Next(MUTATION_STEPS_MIN, MUTATION_STEPS_MAX));

            generatedSpecies.Add(species);
        }
    }

    private void SetupEnoughGameSystemsToRun()
    {
        fluidSystem = new FluidSystem(worldRoot);

        cloudSystem = new CompoundCloudSystem();
        worldRoot.AddChild(cloudSystem);

        cloudSystem.Init(fluidSystem);

        microbeSystem = new MicrobeSystem(dynamicRoot);
        microbeAI = new MicrobeAISystem(dynamicRoot, cloudSystem);

        processSystem = new ProcessSystem(dynamicRoot);

        // ReSharper disable once StringLiteralTypo
        processSystem.SetBiome(SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions);

        chunkSystem = new FloatingChunkSystem(dynamicRoot, cloudSystem);

        timedLifeSystem = new TimedLifeSystem(dynamicRoot);
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
        var microbe = SpawnHelpers.SpawnMicrobe(generatedSpecies[spawnCounter % generatedSpecies.Count], position,
            dynamicRoot, microbeScene, true, cloudSystem!, dummySpawnSystem, gameProperties!);

        spawnedMicrobes.Add(new EntityReference<Microbe>(microbe));
        ++spawnCounter;

        // Spawning also gives a glucose cloud to ensure the spawned microbe doesn't instantly just die
        cloudSystem!.AddCloud(glucose, GLUCOSE_CLOUD_AMOUNT, position);

        // And a bit of phosphate or ammonia
        cloudSystem!.AddCloud(random.Next(0, 1) == 1 ? phosphates : ammonia, AMMONIA_PHOSPHATE_CLOUD_AMOUNT, position);
    }

    private void PruneDeadMicrobes()
    {
        spawnedMicrobes.RemoveAll(r => !r.IsAlive);
    }

    private void WaitForStableFPS()
    {
        // TODO: a more fancy method
        if (timer > 3.5f)
            IncrementPhase();
    }

    private bool MeasureFPS()
    {
        // Sample up to 20 times per second
        if (timer < 0.05f)
            return false;

        timer = 0;

        fpsValues.Add(Engine.GetFramesPerSecond());

        // TODO: could check for standard deviation or something to determine when we have enough samples
        if (fpsValues.Count > 50)
            return true;

        return false;
    }

    private float ScoreFromMeasuredFPS()
    {
        if (fpsValues.Count < 1)
            throw new InvalidOperationException("No values recorded");

        // For now just take the average
        // TODO: would be nice to have also min and 1% lows
        return fpsValues.Average();
    }

    private void UpdatePhaseLabel()
    {
        benchmarkResultsText.Text = GenerateResultsText();

        benchmarkFinishedText.Visible = false;
        copyResultsButton.Visible = false;

        if (internalPhaseCounter <= 0)
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(0, 5);
        }
        else if (internalPhaseCounter <= 3)
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(1, 5);
        }
        else if (internalPhaseCounter <= 6)
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(2, 5);
        }
        else if (internalPhaseCounter <= 9)
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(3, 5);
        }
        else if (internalPhaseCounter <= 10)
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(4, 5);
        }
        else
        {
            phaseLabel.Text = StringUtils.SlashSeparatedNumbersFormat(5, 5);

            benchmarkFinishedText.Visible = true;
            copyResultsButton.Visible = true;
        }
    }

    private void IncrementPhase()
    {
        ++internalPhaseCounter;
        timer = 0;
        fpsValues.Clear();

        UpdatePhaseLabel();
    }

    private string GenerateResultsText(int scoreDecimals = 2)
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

        if (totalDuration != 0)
        {
            builder.Append("Total test duration: ");
            builder.Append(Math.Round(totalDuration, 1).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (builder.Length > 0 || Constants.BENCHMARKS_SHOW_HARDWARE_INFO_IMMEDIATELY)
            builder.Append(BenchmarkHelpers.GetGeneralHardwareInfo());

        // TODO: overall score?

        return builder.ToString();
    }

    private void CopyResultsToClipboard()
    {
        var builder = new StringBuilder();

        builder.Append($"Benchmark results for {nameof(MicrobeBenchmark)} v{VERSION}\n");
        builder.Append(GenerateResultsText(3));

        OS.Clipboard = builder.ToString();
    }

    private void ExitBenchmark()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (exiting)
            return;

        exiting = true;
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, OnSwitchToMenuScene, false);
    }

    private void OnSwitchToMenuScene()
    {
        SceneManager.Instance.ReturnToMenu();
    }
}
