using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using DefaultEcs.Command;
using Godot;
using Newtonsoft.Json;
using World = DefaultEcs.World;

/// <summary>
///   Any type of game world simulation where everything needed to run that simulation is collected under. Note that
///   <see cref="GameWorld"/> is an object holding the game world's information like species etc. These simulation
///   types implementing this interface are in charge of running the gameplay simulation side of things. For example
///   microbe moving around, processing compounds, colliding, rendering etc.
/// </summary>
public abstract class WorldSimulation : IWorldSimulation
{
    protected readonly World entities = new();

    // TODO: did these protected property loading work? Loading / saving for the entities
    protected readonly List<Entity> queuedForDelete = new();

    /// <summary>
    ///   Used to tell a few systems the approximate player position which might not always exist
    /// </summary>
    [JsonIgnore]
    protected Vector3? reportedPlayerPosition;

    [JsonProperty]
    protected float minimumTimeBetweenLogicUpdates = 1 / 60.0f;

    protected float accumulatedLogicTime;

    // TODO: implement saving
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<Entity> entitiesToNotSave = new();

    private readonly Queue<Action> queuedInvokes = new();

    private readonly Queue<EntityCommandRecorder> availableRecorders = new();
    private readonly HashSet<EntityCommandRecorder> nonEmptyRecorders = new();
    private int totalCreatedRecorders;

    /// <summary>
    ///   Access to this world's entity system directly.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that any component modification operations may not be   done while this simulation is currently doing
    ///     a simulation run.
    ///   </para>
    ///   <para>
    ///     Also looping all entities to find relevant ones is only allowed for one-off operations that don't occur
    ///     very often (for example each frame). Systems must be implemented for per-frame operations that act on
    ///     entities having specific components.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public World EntitySystem => entities;

    /// <summary>
    ///   Count of entities (with simulation heaviness weight) in the simulation.
    ///   Spawning can be limited when over some limit to ensure performance doesn't degrade too much.
    /// </summary>
    [JsonProperty]
    public float EntityCount { get; protected set; }

    /// <summary>
    ///   When set to false disables AI running
    /// </summary>
    [JsonProperty]
    public bool RunAI { get; set; } = true;

    /// <summary>
    ///   Player position used to control the simulation accuracy around the player (and despawn things too far away)
    /// </summary>
    [JsonProperty]
    public Vector3 PlayerPosition { get; private set; }

    [JsonIgnore]
    public bool Initialized { get; private set; }

    [JsonIgnore]
    public bool Processing { get; private set; }

    /// <summary>
    ///   Process everything that needs to be done in a neat single method call
    /// </summary>
    /// <param name="delta">Time since last time this was called</param>
    /// <remarks>
    ///   <para>
    ///     This is an alternative to calling <see cref="ProcessLogic"/> and <see cref="ProcessFrameLogic"/> separately
    ///   </para>
    /// </remarks>
    /// <returns>True when a game logic update happened. False if it wasn't time yet.</returns>
    public bool ProcessAll(float delta)
    {
        bool processed = ProcessLogic(delta);
        ProcessFrameLogic(delta);

        return processed;
    }

    /// <summary>
    ///   Processes non-framerate dependent logic and steps the physics simulation once enough time has accumulated
    /// </summary>
    /// <param name="delta">
    ///   Time since previous call, used to determine when it is actually time to do something
    /// </param>
    /// <returns>True when a game logic update happened. False if it wasn't time yet.</returns>
    public virtual bool ProcessLogic(float delta)
    {
        ThrowIfNotInitialized();

        accumulatedLogicTime += delta;

        // TODO: is it a good idea to rate limit physics to not be able to run on update frames when the logic
        // wasn't ran?
        if (accumulatedLogicTime < minimumTimeBetweenLogicUpdates)
            return false;

        if (accumulatedLogicTime > Constants.SIMULATION_MAX_DELTA_TIME)
        {
            // Prevent lag spikes from messing with game logic too bad. The downside here is that at extremely low
            // framerate the game will run in slow motion
            accumulatedLogicTime = Constants.SIMULATION_MAX_DELTA_TIME;
        }

        Processing = true;

        OnCheckPhysicsBeforeProcessStart();

        // Make sure all commands are flushed if someone added some in the time between updates
        ApplyRecordedCommands();

        OnProcessFixedLogic(accumulatedLogicTime);

        ApplyRecordedCommands();

        ProcessDestroyQueue();

        lock (queuedInvokes)
        {
            while (queuedInvokes.Count > 0)
            {
                queuedInvokes.Dequeue().Invoke();
            }
        }

        OnProcessPhysics(accumulatedLogicTime);

        accumulatedLogicTime = 0;
        Processing = false;

        // TODO: periodically run
        // EntitySystem.Optimize() and maybe TrimExcess

        return true;
    }

    /// <summary>
    ///   Perform per-frame logic. Should be only used for things where the additional precision matters for example
    ///   for GUI animation quality. Needs to be called after <see cref="ProcessLogic"/> for a frame when this occurs
    ///   (if a logic update was also performed this frame).
    /// </summary>
    public abstract void ProcessFrameLogic(float delta);

    public Entity CreateEmptyEntity()
    {
        // Ensure thread unsafe operation doesn't happen
        if (Processing)
        {
            throw new InvalidOperationException(
                "Can't use entity create at this time, use deferred create");
        }

        return entities.CreateEntity();
    }

    public EntityRecord CreateEntityDeferred(WorldRecord activeRecording)
    {
        return activeRecording.CreateEntity();
    }

    public bool DestroyEntity(Entity entity)
    {
        lock (queuedForDelete)
        {
            if (queuedForDelete.Contains(entity))
            {
                // Already queued for delete
                return true;
            }

            queuedForDelete.Add(entity);
        }

        return true;
    }

    public void DestroyAllEntities(Entity? skip = null)
    {
        if (Processing)
            throw new InvalidOperationException("Cannot destroy all entities while processing");

        ProcessDestroyQueue();

        // If destroy all is used a lot then this temporary memory use (ToList) here should be solved
        foreach (var entity in entities.ToList())
        {
            if (entity == skip)
                continue;

            PerformEntityDestroy(entity);
        }

        lock (queuedForDelete)
        {
            queuedForDelete.Clear();
        }
    }

    public void ReportEntityDyingSoon(in Entity entity)
    {
        lock (entitiesToNotSave)
        {
            entitiesToNotSave.Add(entity);
        }
    }

    /// <summary>
    ///   Returns true when the given entity is queued for destruction
    /// </summary>
    public bool IsQueuedForDeletion(Entity entity)
    {
        lock (queuedForDelete)
        {
            return queuedForDelete.Contains(entity);
        }
    }

    public EntityCommandRecorder StartRecordingEntityCommands()
    {
        lock (availableRecorders)
        {
            if (availableRecorders.Count > 0)
                return availableRecorders.Dequeue();

            ++totalCreatedRecorders;
            return new EntityCommandRecorder();
        }
    }

    public WorldRecord GetRecorderWorld(EntityCommandRecorder recorder)
    {
        return recorder.Record(entities);
    }

    public void FinishRecordingEntityCommands(EntityCommandRecorder recorder)
    {
        lock (availableRecorders)
        {
#if DEBUG
            if (availableRecorders.Contains(recorder))
                throw new ArgumentException("Entity command recorder already returned");
#endif

            availableRecorders.Enqueue(recorder);

            // This check is here to allow "failed" recording code to simply return the recorders with this same method
            // even if they didn't record anything
            if (recorder.Size > 0)
                nonEmptyRecorders.Add(recorder);
        }
    }

    /// <summary>
    ///   Checks that the entity is in this world and is not being deleted
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True when the entity is in this world and is not queued for deletion</returns>
    public bool IsEntityInWorld(Entity entity)
    {
        // TODO: check WorldId first somehow to ensure this doesn't access things out of bounds in the list of worlds?

        if (!entity.IsAlive)
            return false;

        lock (queuedForDelete)
        {
            return !queuedForDelete.Contains(entity);
        }
    }

    public virtual void ReportPlayerPosition(Vector3 position)
    {
        PlayerPosition = position;
        reportedPlayerPosition = position;
    }

    /// <summary>
    ///   Queue a function to run after the next world logic update cycle
    /// </summary>
    /// <param name="action">Callable to invoke</param>
    public void Invoke(Action action)
    {
        lock (queuedInvokes)
        {
            queuedInvokes.Enqueue(action);
        }
    }

    /// <summary>
    ///   Immediately perform any delayed / queued entity spawns. This can only be used outside the normal update cycle
    ///   to get immediate access to a created entity. For example used when spawning the player.
    /// </summary>
    /// <exception cref="InvalidOperationException">If an update is currently running</exception>
    public void ProcessDelaySpawnedEntitiesImmediately()
    {
        if (Processing)
            throw new InvalidOperationException("Do not call this while world is being processed");

        ApplyRecordedCommands();
    }

    /// <summary>
    ///   Used in conjunction with <see cref="ProcessDelaySpawnedEntitiesImmediately"/> to find the player after spawn
    /// </summary>
    /// <typeparam name="T">Type of component to look for</typeparam>
    /// <returns>The first found entity or an invalid entity</returns>
    public Entity FindFirstEntityWithComponent<T>()
    {
        foreach (var entity in EntitySystem)
        {
            if (entity.Has<T>())
                return entity;
        }

        return default;
    }

    /// <summary>
    ///   Sets maximum rate at which <see cref="ProcessLogic"/> runs the logic. Note that this also constraints the
    ///   physics update rate (though internally consistent steps are guaranteed)
    /// </summary>
    /// <param name="logicFPS">The log framerate (recommended to be always 60)</param>
    public void SetLogicMaxUpdateRate(float logicFPS)
    {
        minimumTimeBetweenLogicUpdates = 1 / logicFPS;
    }

    public virtual void FreeNodeResources()
    {
    }

    /// <summary>
    ///   Note that often when this is disposed, the Nodes are already disposed so this has to skip releasing them.
    ///   If that is not the case it is required to call <see cref="FreeNodeResources"/> before calling Dispose.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///   Checks that previously started (on previous update) physics runs are complete before running this update.
    ///   Also if the physics simulation is behind by too much then this steps the simulation extra times.
    /// </summary>
    protected virtual void OnCheckPhysicsBeforeProcessStart()
    {
        WaitForStartedPhysicsRun();

        while (RunPhysicsIfBehind())
        {
        }
    }

    protected virtual void OnProcessPhysics(float delta)
    {
        OnCheckPhysicsBeforeProcessStart();
        OnStartPhysicsRunIfTime(delta);
    }

    /// <summary>
    ///   Needs to be called by a derived class when its init method is called
    /// </summary>
    protected void OnInitialized()
    {
        if (Initialized)
            throw new InvalidOperationException("This simulation was already initialized");

        Initialized = true;
    }

    protected abstract void WaitForStartedPhysicsRun();
    protected abstract void OnStartPhysicsRunIfTime(float delta);

    /// <summary>
    ///   Should run the physics simulation if it is falling behind
    /// </summary>
    /// <returns>
    ///   Should return true when behind and a step was run, this will be executed until this returns false
    /// </returns>
    protected abstract bool RunPhysicsIfBehind();

    protected abstract void OnProcessFixedLogic(float delta);

    protected void PerformEntityDestroy(Entity entity)
    {
        lock (entitiesToNotSave)
        {
            entitiesToNotSave.Remove(entity);
        }

        OnEntityDestroyed(entity);

        // Destroy the entity from the ECS system
        entity.Dispose();
    }

    /// <summary>
    ///   Called when an entity is being destroyed (but before it is). Used by derived worlds to for example delete
    ///   physics data.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that when the entire world is disposed this is not called for each entity.
    ///   </para>
    /// </remarks>
    /// <param name="entity">The entity that is being destroyed</param>
    protected virtual void OnEntityDestroyed(in Entity entity)
    {
    }

    protected void ThrowIfNotInitialized()
    {
        if (!Initialized)
            throw new InvalidOperationException("Init needs to be called first on this simulation before use");
    }

    protected virtual void Dispose(bool disposing)
    {
        // TODO: decide if destroying all entities on world destroy is needed. It seems that disposing systems can
        // release their allocated resources so this all can just be let to go for memory to be garbage collected later
        // for faster world destroy
        // DestroyAllEntities();

        if (disposing)
        {
            entities.Dispose();
        }
    }

    private void ProcessDestroyQueue()
    {
        // We ensure that no processing operation can be in progress while this is called so this should be completely
        // fine to use without a lock here
        lock (queuedForDelete)
        {
            foreach (var entity in queuedForDelete)
            {
                PerformEntityDestroy(entity);
            }

            // TODO: would it make sense to switch entity count reporting to this class?

            queuedForDelete.Clear();
        }
    }

    private void ApplyRecordedCommands()
    {
        // availableRecorders is not locked here as things are going very wrong already if some system update thread is
        // still running at this time
        if (nonEmptyRecorders.Count < 1)
            return;

        if (availableRecorders.Count != totalCreatedRecorders)
        {
            GD.PrintErr("Not all world entity command recorders were returned, some has leaked a recorder (",
                availableRecorders.Count, " != ", totalCreatedRecorders, " expected)");

#if DEBUG
            throw new Exception("Leaked command recorder detected");
#endif
        }

        foreach (var recorder in nonEmptyRecorders)
        {
            try
            {
                recorder.Execute();
            }
            catch (Exception e)
            {
                GD.PrintErr("Deferred entity command applying caused an exception: ", e);
                recorder.Clear();

#if DEBUG
                throw;
#endif
            }
        }

        nonEmptyRecorders.Clear();
    }
}
