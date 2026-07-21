namespace Test.Utils;

using System;
using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using SharedBase.Archive;

/// <summary>
///   Simulation capable of running enough of a world for tests
/// </summary>
public class TestWorldSimulation : IWorldSimulation
{
    private readonly Queue<Entity> queuedForDelete = new();
    private readonly Queue<CommandBuffer> availableRecorders = new();
    private readonly HashSet<CommandBuffer> nonEmptyRecorders = new();
    private int totalCreatedRecorders;

    public World EntitySystem { get; } = World.Create();
    public bool Processing { get; set; }
    public float WorldTimeScale { get; set; } = 1;

    public ushort CurrentArchiveVersion => 1;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.TestWorldSimulation;
    public bool CanBeReferencedInArchive => false;

    public void ResolveNodeReferences()
    {
    }

    public Entity CreateEntityDeferred(CommandBuffer recorder, ComponentType[] types)
    {
        return recorder.Create(types);
    }

    public Entity CreateEmptyEntity(ComponentType[] types)
    {
        if (Processing)
        {
            throw new InvalidOperationException("Can't use entity create at this time, use deferred create");
        }

        return EntitySystem.Create(types);
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

            var toDestroy = new List<Entity>();
            EntitySystem.Query(new QueryDescription(), toDestroy.Add);

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
    }

    public bool IsEntityInWorld(Entity entity)
    {
        if (entity == Entity.Null)
            return false;

        if (!entity.IsAlive())
            return false;

        lock (queuedForDelete)
        {
            return !queuedForDelete.Contains(entity);
        }
    }

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
            if (availableRecorders.Contains(recorder))
                throw new ArgumentException("Entity command recorder already returned");

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
            --totalCreatedRecorders;
        }
    }

    public bool ProcessAll(float delta)
    {
        ProcessLogic(delta);
        return true;
    }

    public bool ProcessLogic(float delta)
    {
        Processing = true;
        ApplyRecordedCommands();
        ProcessDestroyQueue();
        Processing = false;
        return true;
    }

    public bool HasSystemsWithPendingOperations()
    {
        return false;
    }

    public float GetAndResetTrackedSimulationSpeedRatio()
    {
        return 1;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        // Not currently supported
        throw new NotSupportedException();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            EntitySystem.Dispose();
        }
    }

    private void PerformEntityDestroy(Entity entity)
    {
        if (!entity.IsAlive())
        {
            return;
        }

        // Destroy the entity from the ECS system
        EntitySystem.Destroy(entity);
    }

    private void ProcessDestroyQueue()
    {
        lock (queuedForDelete)
        {
            // This uses a while loop to allow entity destroy callbacks to queue more entities to be destroyed
            while (queuedForDelete.Count > 0)
            {
                PerformEntityDestroy(queuedForDelete.Dequeue());
            }
        }
    }

    private void ApplyRecordedCommands()
    {
        // TODO: logic verification against leaked recorders (somehow if possible to write)
        _ = totalCreatedRecorders;

        if (nonEmptyRecorders.Count < 1)
            return;

        foreach (var recorder in nonEmptyRecorders)
        {
            recorder.Playback(EntitySystem);
        }

        nonEmptyRecorders.Clear();
    }
}
