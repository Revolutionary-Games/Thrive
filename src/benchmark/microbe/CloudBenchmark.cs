using System;
using System.Globalization;
using System.Linq;
using System.Text;
using DefaultEcs;
using DefaultEcs.Threading;
using Godot;
using Systems;

/// <summary>
///   Benchmark for the performance of the compound clouds feature
/// </summary>
public partial class CloudBenchmark : BenchmarkBase
{
    private const float PHOSPHATE_AMOUNT = 5000;

    private const float SPAWN_INTERVAL = 0.221f;

    private const double SPAWN_ANGLE_INCREMENT = MathUtils.FULL_CIRCLE * 0.127f;
    private const float MAX_SPAWN_DISTANCE = 95;
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

#pragma warning restore CA2213

    private World? dummyEntityWorld;
    private CompoundCloudSystem? cloudSystem;

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

    protected override bool StressTestClouds => true;

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
                    manySpawnersResult = ScoreFromMeasuredFPS();
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
        if (simpleSpawningResult != 0)
        {
            builder.Append("Cloud spawn score: ");
            builder.Append(Math.Round(simpleSpawningResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (alsoAbsorbingResult != 0)
        {
            builder.Append("Absorber score: ");
            builder.Append(Math.Round(alsoAbsorbingResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (manySpawnersResult != 0)
        {
            builder.Append("Many spawners score: ");
            builder.Append(Math.Round(manySpawnersResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (stressTestResult != 0)
        {
            builder.Append($"Spawners until under {TARGET_FPS_FOR_SPAWNING} FPS: ");
            builder.Append(Math.Round(stressTestResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (stressTestAverageFPS != 0)
        {
            builder.Append("Stress test average FPS: ");
            builder.Append(Math.Round(stressTestAverageFPS, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (stressTestMinFPS != 0)
        {
            builder.Append("Stress test min FPS: ");
            builder.Append(Math.Round(stressTestMinFPS, scoreDecimals).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        AddTestDurationToResult(builder);

        AddResultHardwareInfo(builder);

        return builder.ToString();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            dummyEntityWorld?.Dispose();
            cloudSystem?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void SetupSimulation()
    {
        cloudSystem = new CompoundCloudSystem();
        worldRoot.AddChild(cloudSystem);

        // Dummy currents that doesn't need to run on any entities has to be created for the cloud system
        dummyEntityWorld = new World();
        var dummyCurrents = new FluidCurrentsSystem(dummyEntityWorld, new DefaultParallelRunner(1));

        cloudSystem.Init(dummyCurrents);
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
