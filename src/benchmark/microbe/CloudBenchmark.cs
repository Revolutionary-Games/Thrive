using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   Benchmark for the performance of the compound clouds feature
/// </summary>
public partial class CloudBenchmark : BenchmarkBase
{
    private const float PHOSPHATE_AMOUNT = 5000;

    private const float SPAWN_INTERVAL = 0.221f;

    private const double SPAWN_ANGLE_INCREMENT = MathUtils.FULL_CIRCLE * 0.127f;
    private const float MAX_SPAWN_DISTANCE = 110;
    private const float SPAWN_DISTANCE_INCREMENT = 1.8f;

    private const float FIRST_PHASE_SPAWNS = 100;

    private const float TARGET_FPS_FOR_SPAWNING = 60;
    private const float STRESS_TEST_END_THRESHOLD = 5;
    private const float STRESS_TEST_THRESHOLD_REDUCE_EVERY_N = 10;
    private const float STRESS_TEST_THRESHOLD_REDUCE = 0.35f;
    private const float STRESS_TEST_END_THRESHOLD_MIN = 1.0f;

    private const int STRESS_TEST_ABSOLUTE_END = 1000;

#pragma warning disable CA2213
    [Export]
    private Label emittersCountLabel = null!;

    [Export]
    private Label absorbersCountLabel = null!;

    [Export]
    private Node worldRoot = null!;

    [Export]
    private MicrobeCamera benchmarkCamera = null!;

    private CompoundCloudSystem? cloudSystem;
#pragma warning restore CA2213

    private int emittersCount;
    private int absorbersCount;

    private int spawnCounter;
    private double spawnAngle;
    private float spawnDistance;

    private float simpleSpawningResult;
    private float alsoAbsorbingResult;
    private float manySpawnersResult;
    private float stressTestResult;
    private float stressTestMinFPS;
    private float stressTestAverageFPS;

    protected override long RandomSeed => 849321;

    protected override int Version => 1;

    protected override int TotalSteps => 4;

    public override void _Process(double delta)
    {
        base._Process(delta);

        emittersCountLabel.Text = emittersCount.ToString(CultureInfo.CurrentCulture);
        absorbersCountLabel.Text = absorbersCount.ToString(CultureInfo.CurrentCulture);

        benchmarkCamera.UpdateCameraPosition(delta, Vector3.Zero);

        switch (internalPhaseCounter)
        {
            case 1:
            {
                // Need to pass some time between each spawn
                if (timer < SPAWN_INTERVAL)
                    break;

                timer = 0;

                if (spawnCounter < FIRST_PHASE_SPAWNS)
                {
                    SpawnAndUpdatePositionState();
                }
                else
                {
                    simpleSpawningResult = (float)Engine.GetFramesPerSecond();
                    IncrementPhase();
                }

                break;
            }

            case 2:
            {
                cloudSystem!.EmptyAllClouds();
                IncrementPhase();
                break;
            }

            case 3:
            {
                // TODO: further phases properly

                if (MeasureFPS())
                {
                    alsoAbsorbingResult = ScoreFromMeasuredFPS();
                    IncrementPhase();
                }

                break;
            }

            case 4:
            {
                cloudSystem!.EmptyAllClouds();
                IncrementPhase();
                break;
            }

            case 5:
            {
                if (MeasureFPS())
                {
                    alsoAbsorbingResult = ScoreFromMeasuredFPS();
                    IncrementPhase();
                }

                break;
            }

            case 6:
            {
                cloudSystem!.EmptyAllClouds();
                IncrementPhase();
                break;
            }

            case 7:
            {
                if (timer < SPAWN_INTERVAL)
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
                    stressTestResult = spawnCounter;
                    stressTestMinFPS = (float)fpsValues.Min();
                    stressTestAverageFPS = ScoreFromMeasuredFPS();
                    IncrementPhase();
                }

                break;
            }

            case 8:
            {
                OnBenchmarkEnded();
                break;
            }
        }
    }

    protected override void OnBenchmarkStarted()
    {
        SetupSimulation();

        spawnAngle = 0;
        spawnDistance = 1;
        emittersCount = 1;
        absorbersCount = 0;
    }

    protected override void OnBenchmarkStateReset()
    {
        simpleSpawningResult = 0;
        alsoAbsorbingResult = 0;
        manySpawnersResult = 0;
        stressTestResult = 0;
        stressTestMinFPS = 0;
        stressTestAverageFPS = 0;
    }

    protected override int GetDisplayStep(int internalStepNumber)
    {
        return internalStepNumber switch
        {
            <= 2 => 1,
            <= 4 => 2,
            <= 6 => 3,
            _ => 4,
        };
    }

    protected override string GenerateResultsText(int scoreDecimals = 2)
    {
        var builder = new StringBuilder();

        // This is intentionally not translatable to make it easier for developers to request this info from players and
        // then understand it
        builder.Append("TODO: results\n");

        /*if (microbeStationaryResult != 0)
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
        }*/

        AddTestDurationToResult(builder);

        AddResultHardwareInfo(builder);

        return builder.ToString();
    }

    private void SetupSimulation()
    {
        cloudSystem = new CompoundCloudSystem();
        worldRoot.AddChild(cloudSystem);
    }

    private void SpawnAndUpdatePositionState()
    {
        var position = new Vector3((float)Math.Cos(spawnAngle), 0, (float)-Math.Sin(spawnAngle)) *
            spawnDistance;

        SpawnCloud(position);

        spawnAngle += SPAWN_ANGLE_INCREMENT;
        spawnDistance += SPAWN_DISTANCE_INCREMENT;

        while (spawnDistance > MAX_SPAWN_DISTANCE)
        {
            spawnDistance -= MAX_SPAWN_DISTANCE;
        }
    }

    private void SpawnCloud(Vector3 position)
    {
        timeSinceSpawn = 0;

        ++spawnCounter;

        cloudSystem!.AddCloud(Compound.Phosphates, PHOSPHATE_AMOUNT, position);
    }
}
