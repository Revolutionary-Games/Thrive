﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using SharedBase.Archive;
using World = Arch.Core.World;

/// <summary>
///   Any type of game world simulation where everything needed to run that simulation is collected under. Note that
///   <see cref="GameWorld"/> is an object holding the game world's information like species etc. These simulation
///   types implementing this interface are in charge of running the gameplay simulation side of things. For example,
///   microbe moving around, processing compounds, colliding, rendering etc.
/// </summary>
public abstract class WorldSimulation : IWorldSimulation, IGodotEarlyNodeResolve
{
    public const ushort SERIALIZATION_VERSION_BASE = 1;

    /// <summary>
    ///   Stores entities that are ignored on save. This field must be before <see cref="entities"/> for saving
    ///   to work correctly.
    /// </summary>
    protected readonly UnsavedEntities entitiesToNotSave;

    protected readonly World entities;

    protected readonly Queue<Entity> queuedForDelete = new();

    /// <summary>
    ///   Used to tell a few systems the approximate player position which might not always exist
    /// </summary>
    protected Vector3? reportedPlayerPosition;

    protected float minimumTimeBetweenLogicUpdates = 1 / 60.0f;

    protected float accumulatedLogicTime;

    /// <summary>
    ///   Set this to true for worlds that do not use multithreading / aren't setup for component checks to be enabled
    /// </summary>
    protected bool disableComponentChecking;

    // TODO: are there situations where invokes not having run yet but a save being made could cause problems?
    private readonly Queue<Action> queuedInvokes = new();

    private readonly Queue<CommandBuffer> availableRecorders = new();
    private readonly HashSet<CommandBuffer> nonEmptyRecorders = new();
    private int totalCreatedRecorders;

    private int missedUpdates;
    private int successfulUpdates;

    /// <summary>
    ///   Used to trigger warnings about <see cref="WorldTimeScale"/> being so high we can't process the game fast
    ///   enough
    /// </summary>
    private int timeScaleMissedUpdates;

    public WorldSimulation()
    {
        entities = World.Create();
        entitiesToNotSave = new UnsavedEntities(queuedForDelete);
    }

    protected WorldSimulation(World entities)
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
    ///     Also, looping all entities to find relevant ones is only allowed for one-off operations that don't occur
    ///     very often (for example, each frame). Systems must be implemented for per-frame operations that act on
    ///     entities having specific components.
    ///   </para>
    /// </remarks>
    public World EntitySystem => entities;

    // TODO: if required add a property that exposes the spawn system total entity weight here

    /// <summary>
    ///   When set to false disables AI running
    /// </summary>
    public bool RunAI { get; set; } = true;

    /// <summary>
    ///   Player position used to control the simulation accuracy around the player (and despawn things too far away)
    /// </summary>
    public Vector3 PlayerPosition { get; private set; }

    public bool Initialized { get; private set; }

    public bool Processing { get; private set; }

    /// <summary>
    ///   Timescale of the non-framerate dependent simulation. Saved with the hope that any GUIs can properly detect
    ///   this being changed.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     As physics step size is not currently adjusted, values below 1 can make the game feel a bit jittery, so for
    ///     now values in the range 1-3 are the best working (though the upper range depends on performance).
    ///   </para>
    /// </remarks>
    public float WorldTimeScale { get; set; } = 1;

    public bool NodeReferencesResolved { get; private set; }

    public abstract ushort CurrentArchiveVersion { get; }
    public abstract ArchiveObjectType ArchiveObjectType { get; }
    public bool CanBeReferencedInArchive => true;

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
    /// <returns>True, when a game logic update happened. False if it wasn't time yet.</returns>
    public bool ProcessAll(float delta)
    {
        bool useSpecialPhysicsMode = !disableComponentChecking && GenerateThreadedSystems.UseCheckedComponentAccess;

        // See the comment below about this about special physics
        if (useSpecialPhysicsMode)
            WaitForStartedPhysicsRun();

        bool processed = ProcessLogic(delta);
        ProcessFrameLogic(delta);

        if (useSpecialPhysicsMode)
        {
            // Physics only runs after the frame systems to ensure physics callbacks triggered during frame systems
            // are not detected incorrectly. This slightly changes the characteristics of the physics interactions
            // with other systems but is fine for this debugging purpose
            OnProcessPhysics(delta * WorldTimeScale);
        }

        return processed;
    }

    /// <summary>
    ///   Processes non-framerate dependent logic and steps the physics simulation once enough time has accumulated
    /// </summary>
    /// <param name="delta">
    ///   Time since the previous call, used to determine when it is actually time to do something
    /// </param>
    /// <returns>True, when a game logic update happened. False if it wasn't time yet.</returns>
    public virtual bool ProcessLogic(float delta)
    {
        ThrowIfNotInitialized();

        // This is a safety check about the timescale somehow being uninitialised / accidentally set to 0
        if (WorldTimeScale <= 0)
        {
            GD.PrintErr("World timescale should be above 0, forcing it to 1");
            WorldTimeScale = 1;
        }

        accumulatedLogicTime += delta * WorldTimeScale;

        // TODO: is it a good idea to rate limit physics to not be able to run on update frames when the logic
        // wasn't ran?
        if (accumulatedLogicTime < minimumTimeBetweenLogicUpdates)
            return false;

        if (accumulatedLogicTime > Constants.SIMULATION_MAX_DELTA_TIME)
        {
            // Prevent lag spikes from messing with game logic too badly. The downside here is that at extremely low
            // framerate, the game will run in slow motion
            accumulatedLogicTime = Constants.SIMULATION_MAX_DELTA_TIME;

            if (WorldTimeScale > 1)
            {
                ++timeScaleMissedUpdates;

                // Only show the warning once per instance of not enough performance
                if (timeScaleMissedUpdates == 2)
                    GD.PrintErr("World time scale is higher than we have processing power for");
            }

            ++missedUpdates;
        }
        else
        {
            if (timeScaleMissedUpdates > 0)
                --timeScaleMissedUpdates;

            ++successfulUpdates;
        }

        Processing = true;

        // Make sure all commands are flushed if someone added some in the time between updates
        ApplyRecordedCommands();

        // See the similar check in ProcessAll to see what this is about (this is about special component debug mode)
        bool useNormalPhysics = disableComponentChecking || !GenerateThreadedSystems.UseCheckedComponentAccess;

        // Make sure physics is not running while the systems are
        if (useNormalPhysics)
            WaitForStartedPhysicsRun();

        if (!disableComponentChecking)
            ComponentAccessChecks.ReportSimulationActive(true);

#if DEBUG
        try
        {
            OnProcessFixedLogic(accumulatedLogicTime);
        }
        catch (Exception e)
        {
            if (Debugger.IsAttached)
                Debugger.Break();

            GD.PrintErr($"Unhandled exception in world simulation: {e}");

            // For now this quits so that other threads won't get stuck waiting for the world simulation to complete
            SceneManager.Instance.QuitDueToError();
            return false;
        }
#else
        OnProcessFixedLogic(accumulatedLogicTime);
#endif

        if (!disableComponentChecking)
            ComponentAccessChecks.ReportSimulationActive(false);

        ApplyRecordedCommands();

        ProcessDestroyQueue();

        lock (queuedInvokes)
        {
            while (queuedInvokes.Count > 0)
            {
                try
                {
                    queuedInvokes.Dequeue().Invoke();
                }
                catch (Exception e)
                {
                    GD.PrintErr($"Unhandled exception in world simulation invoke: {e}");

                    if (Debugger.IsAttached)
                        Debugger.Break();
                }
            }
        }

        if (useNormalPhysics)
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
    public void ProcessFrameLogic(float delta)
    {
        ThrowIfNotInitialized();

        Processing = true;

        if (!disableComponentChecking)
            ComponentAccessChecks.ReportSimulationActive(true);

        OnProcessFrameLogic(delta);

        Processing = false;
        if (!disableComponentChecking)
            ComponentAccessChecks.ReportSimulationActive(false);
    }

    public Entity CreateEmptyEntity(ComponentType[] types)
    {
        // Ensure thread unsafe operation doesn't happen
        if (Processing)
        {
            throw new InvalidOperationException("Can't use entity create at this time, use deferred create");
        }

        return entities.Create(types);
    }

    public Entity CreateEntityDeferred(CommandBuffer activeRecording, ComponentType[] componentTypes)
    {
        return activeRecording.Create(componentTypes);
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

        // Apply all commands first to clear these out to prevent these causing something to spawn after the clear
        ApplyRecordedCommands();

        ProcessDestroyQueue();

        // This loop is here to ensure that no entities are left after destroy callbacks have been used
        while (true)
        {
            bool despawned = false;

            // If destroy all is used a lot, then this temporary memory use (ToList) here should be solved
            var toDestroy = new List<Entity>();
            entities.Query(new QueryDescription(), entity => toDestroy.Add(entity));

            foreach (var entity in toDestroy)
            {
                if (entity == skip)
                    continue;

                PerformEntityDestroy(entity);
                despawned = true;
            }

            if (!despawned || skip != null)
                break;
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

    public CommandBuffer StartRecordingEntityCommands()
    {
        lock (availableRecorders)
        {
            if (availableRecorders.Count > 0)
                return availableRecorders.Dequeue();

            ++totalCreatedRecorders;
            return new CommandBuffer();
        }
    }

    public void FinishRecordingEntityCommands(CommandBuffer recorder)
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

    public void OnFailedRecordingEntityCommands(CommandBuffer recorder)
    {
        recorder.Dispose();
        lock (availableRecorders)
        {
            GD.Print("An entity command recording has failed, it will be discarded. " +
                "Hopefully unrelated operations were not impacted");
            --totalCreatedRecorders;
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

        if (!entity.IsAlive())
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
    ///   to get immediate access to a created entity. For example, used when spawning the player.
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
        var found = Entity.Null;

        // TODO: there's probably a much better way to do this with Arch (might need to go through the archetype list)
        EntitySystem.Query(new QueryDescription().WithAll<T>(), entity =>
        {
            if (found == Entity.Null)
            {
                found = entity;
            }
        });

        return found;
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

    public float GetAndResetTrackedSimulationSpeedRatio()
    {
        var total = successfulUpdates + missedUpdates;

        // If called too often, we have no data, and at that point we'll assume we are running at full speed
        if (total == 0)
            return 1;

        var result = successfulUpdates / (float)total;

        successfulUpdates = 0;
        missedUpdates = 0;

        return result;
    }

    public virtual void FreeNodeResources()
    {
    }

    public abstract void WriteToArchive(ISArchiveWriter writer);

    public void ActivateWorldOnReadContext(ISArchiveReader reader)
    {
        var manager = (ISaveContext)reader.ReadManager;

        if (manager.ProcessedEntityWorld != null)
        {
            throw new InvalidOperationException(
                "Cannot activate this world on read context as something is already active");
        }

        manager.ProcessedEntityWorld = EntitySystem;
    }

    public void DeactivateWorldOnReadContext(ISArchiveReader reader)
    {
        var manager = (ISaveContext)reader.ReadManager;

        if (manager.ProcessedEntityWorld != EntitySystem)
            throw new InvalidOperationException("Someone deactivated this world read context already");

        manager.ProcessedEntityWorld = null;
    }

    /// <summary>
    ///   Note that often when this is disposed, the Nodes are already disposed, so this has to skip releasing them.
    ///   If that is not the case, it is required to call <see cref="FreeNodeResources"/> before calling Dispose.
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

    protected virtual void WriteBasePropertiesToArchive(ISArchiveWriter writer)
    {
        lock (entitiesToNotSave)
        {
            writer.WriteObjectProperties(entitiesToNotSave);
        }

        writer.WriteAnyRegisteredValueAsObject(entities);
        writer.Write(minimumTimeBetweenLogicUpdates);
        writer.Write(RunAI);
        writer.Write(PlayerPosition);
        writer.Write(WorldTimeScale);
    }

    protected virtual void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_BASE or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_BASE);

        ActivateWorldOnReadContext(reader);

        // The first two properties must be read already by the time this is called due to being used for construction
        minimumTimeBetweenLogicUpdates = reader.ReadFloat();
        RunAI = reader.ReadBool();
        PlayerPosition = reader.ReadVector3();
        WorldTimeScale = reader.ReadFloat();
    }

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

    protected abstract void OnProcessFrameLogic(float delta);

    protected virtual void OnPlayerPositionSet(Vector3 playerPosition)
    {
    }

    protected void PerformEntityDestroy(Entity entity)
    {
        lock (entitiesToNotSave)
        {
            entitiesToNotSave.Remove(entity);
        }

        // Skip multiple destruction of entities that were already destroyed but were queued to be destroyed again
        if (!entity.IsAlive())
        {
            GD.Print("Ignoring duplicate destroy of entity ", entity);
            return;
        }

        lock (availableRecorders)
        {
            if (nonEmptyRecorders.Count > 0)
            {
                GD.PrintErr("Cannot destroy entities while pending command buffers exist");
                throw new InvalidOperationException("Cannot destroy entities while pending command buffers exist");
            }
        }

        OnEntityDestroyed(entity);

        // If callbacks created any pending operations, those must be flushed now
        lock (availableRecorders)
        {
            if (nonEmptyRecorders.Count > 0)
            {
                ApplyRecordedCommands();
            }
        }

        // Destroy the entity from the ECS system
        EntitySystem.Destroy(entity);
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
        if (entity == Entity.Null)
            throw new ArgumentException("Entity reported destroyed cannot be null");
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
                recorder.Playback(EntitySystem);
            }
            catch (Exception e)
            {
                GD.PrintErr("Deferred entity command applying caused an exception: ", e);
                recorder.Dispose();

                // To get back to a somewhat correct state, we need to clear the recorder from the available ones
                GD.PrintErr("Flushing available recorders to get rid of the problematic one");
                var temp = availableRecorders.ToArray();
                int oldCount = availableRecorders.Count;

                if (oldCount < 1)
                    throw new InvalidOperationException("Played recorder should have been in available recorders");

                availableRecorders.Clear();

                foreach (var potentialRecorder in temp)
                {
                    if (potentialRecorder != recorder)
                        availableRecorders.Enqueue(potentialRecorder);
                }

                if (availableRecorders.Count - 1 < oldCount)
                {
                    throw new Exception("We somehow lost recorders while copying");
                }

#if DEBUG
                throw;
#endif
            }
        }

        nonEmptyRecorders.Clear();
    }
}
