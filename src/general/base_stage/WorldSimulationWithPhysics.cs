using System;
using System.Collections.Generic;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   World simulation that uses the external physics engine in the native code module
/// </summary>
public abstract class WorldSimulationWithPhysics : WorldSimulation, IWorldSimulationWithPhysics
{
    protected readonly PhysicalWorld physics = PhysicalWorld.Create();

    /// <summary>
    ///   All created physics bodies. Must be tracked to correctly destroy them all
    /// </summary>
    protected readonly List<NativePhysicsBody> createdBodies = new();

    /// <summary>
    ///   Set to true to force physics on the main thread (well, at least to wait the main thread while physics runs).
    ///   Note that causes the game to slow down a bit.
    /// </summary>
    protected bool usePhysicsOnMainThread;

    /// <summary>
    ///   To avoid jitter of constantly changing physics steps, we evaluate performance over some time period.
    /// </summary>
    private const float PhysicsEvaluationPeriod = 1;

    /// <summary>
    ///   In case the performance gets better, we can increase physics fidelity again.
    /// </summary>
    private const float PhysicsRecoveryInterval = 90;

    // Physics performance adjusting variables
    private PhysicsSteppingState physicsSteppingState;
    private float physicsTimeSinceLastRun;
    private float physicsEvaluationTime;
    private float physicsEvaluationDuration;
    private float physicsTimeSinceStateChange;
    private bool physicsPerformanceForCurrentRunConsumed;

    public WorldSimulationWithPhysics()
    {
    }

    protected WorldSimulationWithPhysics(World entities) : base(entities)
    {
    }

    ~WorldSimulationWithPhysics()
    {
        Dispose(false);
    }

    public enum PhysicsSteppingState
    {
        FullSpeed,
        ThirtyUpdatesPerSecond,
        TenUpdatesPerSecond,
        HalfSimulationTime,
        QuarterSimulationTime,
    }

    public PhysicalWorld PhysicalWorld => physics;

    public PhysicsSteppingState CurrentPhysicsSteppingState => physicsSteppingState;

    /// <summary>
    ///   Set to allow the world to send some status messages
    /// </summary>
    public IHUDMessageReceiver? MessageReceiver { get; set; }

    public NativePhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quaternion rotation)
    {
        var body = physics.CreateMovingBody(shape, position, rotation);
        createdBodies.Add(body);
        return body;
    }

    public NativePhysicsBody CreateMovingBodyWithAxisLock(PhysicsShape shape, Vector3 position, Quaternion rotation,
        Vector3 lockedAxis, bool lockRotation)
    {
        var body = physics.CreateMovingBodyWithAxisLock(shape, position, rotation, lockedAxis, lockRotation);
        createdBodies.Add(body);
        return body;
    }

    public NativePhysicsBody CreateStaticBody(PhysicsShape shape, Vector3 position, Quaternion rotation)
    {
        var body = physics.CreateStaticBody(shape, position, rotation);
        createdBodies.Add(body);
        return body;
    }

    public NativePhysicsBody CreateSensor(PhysicsShape sensorShape, Vector3 position, Quaternion rotation,
        bool detectSleepingBodies = false, bool detectStaticBodies = false)
    {
        var body = physics.CreateSensor(sensorShape, position, rotation, detectSleepingBodies, detectStaticBodies);
        createdBodies.Add(body);
        return body;
    }

    public void DestroyBody(NativePhysicsBody body)
    {
        if (!createdBodies.Remove(body))
        {
            GD.PrintErr("Can't destroy body not in simulation");
            return;
        }

        // Stop collision recording if it is active to make sure the memory for that is returned to the pool
        if (body.ActiveCollisions != null)
            physics.BodyStopCollisionRecording(body);

        physics.DestroyBody(body);

        // Other code is not allowed to hold on to physics bodies on entities that are destroyed, so we dispose this
        // here to get the native side wrapper released as well
        body.Dispose();
    }

    protected abstract override void InitSystemsEarly();

    protected override void WaitForStartedPhysicsRun()
    {
        if (physics.WaitUntilPhysicsRunEnds() && !physicsPerformanceForCurrentRunConsumed)
        {
            RecordPhysicsPerformance();
            physicsPerformanceForCurrentRunConsumed = true;
        }
    }

    protected override void OnStartPhysicsRunIfTime(float delta)
    {
        // Delta is in simulation time, while the physics duration is measured using wall-clock time. Convert it back
        // to real time for the performance comparison. The scaled delta is still used for stepping physics below.
        physicsTimeSinceLastRun += delta / WorldTimeScale;

        var physicsDelta = physicsSteppingState switch
        {
            PhysicsSteppingState.HalfSimulationTime => delta * 0.5f,
            PhysicsSteppingState.QuarterSimulationTime => delta * 0.25f,
            _ => delta,
        };

        if (usePhysicsOnMainThread)
        {
            if (physics.ProcessPhysics(physicsDelta))
            {
                physicsPerformanceForCurrentRunConsumed = false;
                RecordPhysicsPerformance();
                physicsPerformanceForCurrentRunConsumed = true;
            }
        }
        else
        {
            // A background call is a new run from the point of view of the completion result. It may not actually
            // step physics yet, in which case the following wait returns false and no measurement is recorded.
            physicsPerformanceForCurrentRunConsumed = false;
            physics.ProcessPhysicsOnBackgroundThread(physicsDelta);
        }
    }

    protected virtual float GetGameFPS()
    {
        return (float)Engine.GetFramesPerSecond();
    }

    protected override void Dispose(bool disposing)
    {
        // Derived classes should also wait for this before destroying things (and set metrics reporting off)
        physics.DisablePhysicsTimeRecording = true;
        WaitForStartedPhysicsRun();

        ReleaseUnmanagedResources();

        // if (disposing)
        // {
        //
        // }

        base.Dispose(disposing);
    }

    private void ReleaseUnmanagedResources()
    {
        while (createdBodies.Count > 0)
        {
            var body = createdBodies[^1];

            // This should never happen, but this is here in case this does happen to give a better error message
            if (body.IsDisposed)
                throw new Exception("World physics body was disposed by someone else");

            DestroyBody(body);
        }

        physics.Dispose();
    }

    private void RecordPhysicsPerformance()
    {
        physicsEvaluationTime += physicsTimeSinceLastRun;
        physicsEvaluationDuration += physics.LatestPhysicsDuration;
        physicsTimeSinceLastRun = 0;

        if (physicsEvaluationTime < PhysicsEvaluationPeriod)
            return;

        var physicsIsTooSlow = physicsEvaluationDuration > physicsEvaluationTime;
        var completedEvaluationTime = physicsEvaluationTime;
        physicsEvaluationTime = 0;
        physicsEvaluationDuration = 0;

        if (physicsIsTooSlow)
        {
            physicsTimeSinceStateChange = 0;

            if (physicsSteppingState != PhysicsSteppingState.QuarterSimulationTime)
            {
                // Slow down if we are too slow
                physicsSteppingState = (PhysicsSteppingState)((int)physicsSteppingState + 1);
                GD.Print("Physical world is detecting low performance, slowing down to: ", physicsSteppingState);

                if (physicsSteppingState >= PhysicsSteppingState.TenUpdatesPerSecond)
                {
                    MessageReceiver?.ShowMessage(Localization.Translate("GAME_PHYSICS_PERFORMANCE_LOW_WARNING"));
                }

                ApplyPhysicsSteppingState();
            }

            return;
        }

        physicsTimeSinceStateChange += completedEvaluationTime;

        // If we suffered temporary very low FPS but have now recovered, recover the simulation speed fast
        if (physicsSteppingState > PhysicsSteppingState.ThirtyUpdatesPerSecond && physicsTimeSinceStateChange > 0.2f
            && GetGameFPS() >= 60)
        {
            physicsTimeSinceStateChange = 0;
            physicsSteppingState = (PhysicsSteppingState)((int)physicsSteppingState - 1);
            GD.Print("FPS has recovered a lot, speeding up world simulation");
            ApplyPhysicsSteppingState();
            return;
        }

        if (physicsSteppingState != PhysicsSteppingState.FullSpeed &&
            physicsTimeSinceStateChange >= PhysicsRecoveryInterval)
        {
            // Try to speed up to see if we have potentially recovered from slowness
            physicsTimeSinceStateChange = 0;
            physicsSteppingState = (PhysicsSteppingState)((int)physicsSteppingState - 1);
            GD.Print("Checking physical world physics performance recovery");
            ApplyPhysicsSteppingState();
        }
    }

    private void ApplyPhysicsSteppingState()
    {
        var timestep = physicsSteppingState switch
        {
            PhysicsSteppingState.FullSpeed => 1.0f / 60,
            PhysicsSteppingState.ThirtyUpdatesPerSecond => 1.0f / 30,

            // These don't lower the timestep as it would make physics way less accurate
            PhysicsSteppingState.TenUpdatesPerSecond => 1.0f / 10,
            PhysicsSteppingState.HalfSimulationTime => 1.0f / 10,
            PhysicsSteppingState.QuarterSimulationTime => 1.0f / 10,
            _ => throw new ArgumentOutOfRangeException(),
        };

        physics.SetPhysicsTimestep(timestep);
    }
}
