﻿using System;
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
[UseThriveSerializer]
public abstract class WorldSimulation : IWorldSimulation, IGodotEarlyNodeResolve
{
    /// <summary>
    ///   Stores entities that are ignored on save. This field must be before <see cref="entities"/> for saving
    ///   to work correctly.
    /// </summary>
    [JsonProperty]
    protected readonly UnsavedEntities entitiesToNotSave;

    [JsonProperty]
    protected readonly World entities;

    protected readonly Queue<Entity> queuedForDelete = new();

    /// <summary>
    ///   Used to tell a few systems the approximate player position which might not always exist
    /// </summary>
    [JsonIgnore]
    protected Vector3? reportedPlayerPosition;

    [JsonProperty]
    protected float minimumTimeBetweenLogicUpdates = 1 / 60.0f;

    protected float accumulatedLogicTime;

    // TODO: are there situations where invokes not having run yet but a save being made could cause problems?
    private readonly Queue<Action> queuedInvokes = new();

    private readonly Queue<EntityCommandRecorder> availableRecorders = new();
    private readonly HashSet<EntityCommandRecorder> nonEmptyRecorders = new();
    private int totalCreatedRecorders;

    private float timeSinceLastEntityEstimate = 1;
    private int ecsThreadsToUse = 1;

    public WorldSimulation()
    {
        entities = new World();
        entitiesToNotSave = new UnsavedEntities(queuedForDelete);
    }

    [JsonConstructor]
    public WorldSimulation(World entities)
    {
        this.entities = entities;
        entitiesToNotSave = new UnsavedEntities(queuedForDelete);
    }

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

    // TODO: if required add a property that exposes the spawn system total entity weight here

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

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;
        InitSystemsEarly();
    }

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

        // Allow this time to actually reflect realtime
        timeSinceLastEntityEstimate += accumulatedLogicTime;

        if (accumulatedLogicTime > Constants.SIMULATION_MAX_DELTA_TIME)
        {
            // Prevent lag spikes from messing with game logic too bad. The downside here is that at extremely low
            // framerate the game will run in slow motion
            accumulatedLogicTime = Constants.SIMULATION_MAX_DELTA_TIME;
        }

        Processing = true;

        // Make sure all commands are flushed if someone added some in the time between updates
        ApplyRecordedCommands();

        if (timeSinceLastEntityEstimate > Constants.SIMULATION_OPTIMIZE_THREADS_INTERVAL)
        {
            timeSinceLastEntityEstimate = 0;
            ecsThreadsToUse = EstimateThreadsUtilizedBySystems();
        }

        ApplyECSThreadCount(ecsThreadsToUse);

        // Make sure physics is not running while the systems are
        WaitForStartedPhysicsRun();

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
    /// <remarks>
    ///   <para>
    ///     Note that this may not read any physics state as the next physics run may already be in progress on a
    ///     different thread. Fixed process must be used to read / write physics data, and copy it in case any would be
    ///     needed here.
    ///   </para>
    /// </remarks>
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

            queuedForDelete.Enqueue(entity);
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

    public void ReportPlayerPosition(Vector3 position)
    {
        PlayerPosition = position;
        reportedPlayerPosition = position;

        OnPlayerPositionSet(PlayerPosition);
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

    public abstract bool HasSystemsWithPendingOperations();

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
    ///   Called just after resolving node references to allow the earliest systems to be created that for example need
    ///   to have save properties applied to them.
    /// </summary>
    protected abstract void InitSystemsEarly();

    protected virtual void OnProcessPhysics(float delta)
    {
        WaitForStartedPhysicsRun();
        OnStartPhysicsRunIfTime(delta);
    }

    /// <summary>
    ///   Needs to be called by a derived class when its init method is called
    /// </summary>
    protected void OnInitialized()
    {
        if (!NodeReferencesResolved)
            throw new InvalidOperationException("Node reference resolve as not called");

        if (Initialized)
            throw new InvalidOperationException("This simulation was already initialized");

        Initialized = true;

        // This is only really needed on load, but doesn't hurt to be here always
        OnPlayerPositionSet(PlayerPosition);
    }

    /// <summary>
    ///   Checks that previously started (on previous update) physics runs are complete before running this update.
    /// </summary>
    protected abstract void WaitForStartedPhysicsRun();

    protected abstract void OnStartPhysicsRunIfTime(float delta);

    protected abstract void OnProcessFixedLogic(float delta);

    protected virtual void OnPlayerPositionSet(Vector3 playerPosition)
    {
    }

    /// <summary>
    ///   Provides an estimate based on the number of entities (that are the most prevalent type) how many threads the
    ///   ECS system should use when processing entities. This needs to be a bit lower than what maximally would give
    ///   more performance to ensure systems with more thread switching overhead don't suffer from lowered performance.
    /// </summary>
    /// <returns>The number of simultaneous single entity system tasks there should be processed</returns>
    protected virtual int EstimateThreadsUtilizedBySystems()
    {
        // By default no multithreading, just use main thread
        return 1;
    }

    /// <summary>
    ///   Sets the number of ECS threads to use by <see cref="TaskExecutor"/>. It's not the best to have this be
    ///   a global property in the executor, but this works well enough with the worlds setting the number of threads
    ///   to use just before running.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is overridable so that simulations that don't use threading can skip this operation to not mess with
    ///     simulations that do use this (for example pure visuals simulations don't use threading).
    ///   </para>
    /// </remarks>
    /// <param name="ecsThreadsToUse">Number of threads to use</param>
    protected virtual void ApplyECSThreadCount(int ecsThreadsToUse)
    {
        TaskExecutor.Instance.ECSThrottling = ecsThreadsToUse;
    }

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
            // This uses a while loop to allow entity destroy callbacks to queue more entities to be destroyed
            while (queuedForDelete.Count > 0)
            {
                PerformEntityDestroy(queuedForDelete.Dequeue());
            }

            // TODO: would it make sense to switch entity count reporting to this class?
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
