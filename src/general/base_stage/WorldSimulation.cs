using System;
using System.Collections.Generic;
using DefaultEcs;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Any type of game world simulation where everything needed to run that simulation is collected under. Note that
///   <see cref="GameWorld"/> is an object holding the game world's information like species etc. These simulation
///   types implementing this interface are in charge of running the gameplay simulation side of things. For example
///   microbe moving around, processing compounds, colliding, rendering etc.
/// </summary>
public abstract class WorldSimulation : IDisposable
{
    // TODO: did these protected property loading work? Loading / saving for the entities
    protected readonly List<Entity> queuedForDelete = new();

    [JsonProperty]
    protected float minimumTimeBetweenLogicUpdates = 1 / 60.0f;

    protected float accumulatedLogicTime;

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
    public Vector3 PlayerPosition { get; set; }

    [JsonIgnore]
    public bool Initialized { get; private set; }

    /// <summary>
    ///   Perform per-frame logic. Should be only used for things where the additional precision matters for example
    ///   for GUI animation quality
    /// </summary>
    public abstract void ProcessFrameLogic(float delta);

    /// <summary>
    ///   Processes non-framerate dependent logic and steps the physics simulation once enough time has accumulated
    /// </summary>
    /// <param name="delta">
    ///   Time since previous call, used to determine when it is actually time to do something
    /// </param>
    public virtual void ProcessLogic(float delta)
    {
        ThrowIfNotInitialized();

        accumulatedLogicTime += delta;

        // TODO: is it a good idea to rate limit physics to not be able to run on update frames when the logic wasn't ran
        if (accumulatedLogicTime < minimumTimeBetweenLogicUpdates)
            return;

        OnCheckPhysicsBeforeProcessStart();

        OnProcessFixedLogic(accumulatedLogicTime);

        ProcessDestroyQueue();

        OnProcessPhysics(delta);

        accumulatedLogicTime = 0;
    }

    public Entity CreateEmptyEntity()
    {
        throw new NotImplementedException();
    }

    public bool DestroyEntity(Entity entity)
    {
        if (queuedForDelete.Contains(entity))
        {
            // Already queued for delete
            return true;
        }

        queuedForDelete.Add(entity);
        return true;
    }

    public void DestroyAllEntities(Entity? skip = null)
    {
        ProcessDestroyQueue();

        throw new NotImplementedException();

        // foreach (var entity in entities)
        // {
        //     if (entity == skip)
        //         continue;
        //
        //     entity.OnDestroyed();
        // }
        //
        // entities.RemoveAll(e => e != skip);
        queuedForDelete.Clear();
    }

    /// <summary>
    ///   Returns true when the given entity is queued for destruction
    /// </summary>
    public bool IsQueuedForDeletion(Entity entity)
    {
        return queuedForDelete.Contains(entity);
    }

    /// <summary>
    ///   Checks that the entity is in this world and is not being deleted
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True when the entity is in this world and is not queued for deletion</returns>
    public bool IsEntityInWorld(Entity entity)
    {
        throw new NotImplementedException();

        return /*entities.Contains(castedEntity) &&*/ !queuedForDelete.Contains(entity);
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

    protected void ProcessDestroyQueue()
    {
        foreach (var entity in queuedForDelete)
        {
            throw new NotImplementedException();
        }

        queuedForDelete.Clear();
    }

    protected void ThrowIfNotInitialized()
    {
        if (!Initialized)
            throw new InvalidOperationException("Init needs to be called first on this simulation before use");
    }

    protected virtual void Dispose(bool disposing)
    {
        DestroyAllEntities();

        if (disposing)
        {
        }
    }
}
