using System;
using System.Collections.Generic;
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

    private const float SPAWN_INTERVAL = 0.201f;

    private const double SPAWN_ANGLE_INCREMENT = MathUtils.FULL_CIRCLE * 0.127f;
    private const float MAX_SPAWN_DISTANCE = 95;
    private const float SPAWN_DISTANCE_INCREMENT = 1.8f;

    private const float FIRST_PHASE_SPAWNS = 100;

    private const float TARGET_FPS_FOR_SPAWNING = 60;
    private const float STRESS_TEST_END_THRESHOLD = 5;
    private const float STRESS_TEST_THRESHOLD_REDUCE_EVERY_N = 15;
    private const float STRESS_TEST_THRESHOLD_REDUCE = 0.35f;
    private const float STRESS_TEST_END_THRESHOLD_MIN = 1.0f;
    private const int STRESS_TEST_SPAWN_INCREASE_EVERY_N = 8;
    private const float STRESS_TEST_SIMULATION_INCREASE_EVERY_N = 130;

    private const float ABSORBER_RADIUS = 8;
    private const float ABSORB_RATE = 0.99f;

    private const double EXTRA_SIMULATION_DELTA = 1 / 60.0;

    private const int STRESS_TEST_ABSOLUTE_END = 1000;

    private readonly CompoundBag absorbBag = new(float.MaxValue);
    private readonly Dictionary<Compound, float> absorbTracker = new();

#pragma warning disable CA2213
    [Export]
    private Label emittersCountLabel = null!;

    [Export]
    private Label absorbersCountLabel = null!;

    [Export]
    private Label multipliedSimulationsLabel = null!;

    [Export]
    private Control multipliedSimulationsContainer = null!;

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

    private double absorbAngle;
    private float absorbDistance;

    private float extraSimulations;
    private double accumulatedExtraDelta;

    private float simpleSpawningResult;
    private float alsoAbsorbingResult;
    private float manySpawnersResult;
    private float stressTestResult;
    private float stressTestEmittersResult;
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

        if (extraSimulations > 0)
        {
            multipliedSimulationsContainer.Visible = true;
            multipliedSimulationsLabel.Text = Math.Round(extraSimulations + 1, 2).ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            multipliedSimulationsContainer.Visible = false;
        }

        benchmarkCamera.UpdateCameraPosition(delta, Vector3.Zero);

        // Extra cloud simulations per frame to make things heavier. Though 2x is very high so this starts low

        accumulatedExtraDelta = delta * extraSimulations;

        while (accumulatedExtraDelta > EXTRA_SIMULATION_DELTA)
        {
            accumulatedExtraDelta -= EXTRA_SIMULATION_DELTA;
            cloudSystem!._Process(EXTRA_SIMULATION_DELTA);
        }

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
                    spawnCounter = 0;
                    IncrementPhase();
                }

                break;
            }

            case 2:
            {
                // This shouldn't actually clear here so that the absorbing can find some stuff
                // cloudSystem!.EmptyAllClouds();
                IncrementPhase();
                break;
            }

            case 3:
            {
                if (timer < SPAWN_INTERVAL)
                    break;

                absorbersCount = 1;

                SpawnAndUpdatePositionState();
                AbsorbAndUpdatePositionState(delta);

                // Wait a bit before measuring the FPS
                if (spawnCounter > FIRST_PHASE_SPAWNS * 0.5f)
                {
                    // Ensure this always samples the FPS after a spawn as this reuses the timer value
                    timer = 1;
                    if (MeasureFPS())
                    {
                        alsoAbsorbingResult = ScoreFromMeasuredFPS();

                        spawnCounter = 0;
                        spawnAngle = 0;
                        spawnDistance = 0;
                        absorbAngle = 0;
                        absorbDistance = 0;

                        IncrementPhase();
                    }
                }

                timer = 0;

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
                if (timer < SPAWN_INTERVAL)
                    break;

                emittersCount = 16;
                absorbersCount = 16;

                SpawnAndUpdatePositionState();
                AbsorbAndUpdatePositionState(delta);

                if (spawnCounter > FIRST_PHASE_SPAWNS)
                {
                    timer = 1;
                    if (MeasureFPS())
                    {
                        manySpawnersResult = ScoreFromMeasuredFPS();
                        emittersCount = 1;
                        absorbersCount = 1;
                        spawnCounter = 0;
                        IncrementPhase();
                    }
                }

                timer = 0;
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

                emittersCount = 2 + spawnCounter / STRESS_TEST_SPAWN_INCREASE_EVERY_N;
                absorbersCount = 1 + spawnCounter / STRESS_TEST_SPAWN_INCREASE_EVERY_N;
                extraSimulations = spawnCounter / STRESS_TEST_SIMULATION_INCREASE_EVERY_N;

                if (Engine.GetFramesPerSecond() >= TARGET_FPS_FOR_SPAWNING)
                {
                    SpawnAndUpdatePositionState();
                    AbsorbAndUpdatePositionState(delta);
                }

                fpsValues.Add(Engine.GetFramesPerSecond());

                float endThreshold = Math.Max(STRESS_TEST_END_THRESHOLD_MIN,
                    STRESS_TEST_END_THRESHOLD - spawnCounter / STRESS_TEST_THRESHOLD_REDUCE_EVERY_N *
                    STRESS_TEST_THRESHOLD_REDUCE);

                // Quit if it has been a while since the last spawn or there's been way too much data already
                if ((timeSinceSpawn > endThreshold && fpsValues.Count > 0) ||
                    fpsValues.Count > STRESS_TEST_ABSOLUTE_END)
                {
                    stressTestResult = extraSimulations + 1;
                    stressTestEmittersResult = emittersCount;
                    stressTestMinFPS = (float)fpsValues.Min();
                    stressTestAverageFPS = ScoreFromMeasuredFPS();
                    IncrementPhase();
                }

                break;
            }

            case 8:
            {
                OnBenchmarkEnded();
                extraSimulations = 0;
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
        stressTestEmittersResult = 0;
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
            builder.Append($"Cloud sim multiplier before under {TARGET_FPS_FOR_SPAWNING} FPS: ");
            builder.Append(Math.Round(stressTestResult, scoreDecimals + 1).ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        if (stressTestEmittersResult != 0)
        {
            builder.Append("Stress test spawners: ");
            builder.Append(Math.Round(stressTestEmittersResult, scoreDecimals).ToString(CultureInfo.InvariantCulture));
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

        // Ensure absorption will absorb everything
        for (var i = Compound.ATP; i < Compound.MaxInbuiltCompound; ++i)
        {
            absorbBag.SetUseful(i);
        }
    }

    private void SpawnAndUpdatePositionState()
    {
        ++spawnCounter;

        for (int i = 0; i < emittersCount; ++i)
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
    }

    private void AbsorbAndUpdatePositionState(double delta)
    {
        // Reset bag contents to ensure it can never overflow
        absorbBag.ClearCompounds();
        absorbTracker.Clear();

        for (int i = 0; i < absorbersCount; ++i)
        {
            var position = new Vector3((float)Math.Cos(absorbAngle), 0, (float)-Math.Sin(absorbAngle)) *
                absorbDistance;

            AbsorbCloud(position, delta);

            absorbAngle += SPAWN_ANGLE_INCREMENT;
            absorbDistance += SPAWN_DISTANCE_INCREMENT;

            while (absorbDistance > MAX_SPAWN_DISTANCE)
            {
                absorbDistance -= MAX_SPAWN_DISTANCE;
            }
        }
    }

    private void SpawnCloud(Vector3 position)
    {
        timeSinceSpawn = 0;

        cloudSystem!.AddCloud(Compound.Phosphates, PHOSPHATE_AMOUNT, position);
    }

    private void AbsorbCloud(Vector3 position, double delta)
    {
        cloudSystem!.AbsorbCompounds(position, ABSORBER_RADIUS, absorbBag, absorbTracker, (float)delta, ABSORB_RATE);
    }
}
