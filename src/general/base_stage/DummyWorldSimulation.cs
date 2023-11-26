using System;
using DefaultEcs;
using DefaultEcs.Command;

/// <summary>
///   For use in the prototypes not yet converted to using world simulations
/// </summary>
public class DummyWorldSimulation : IWorldSimulation
{
    public World EntitySystem { get; } = new();
    public bool Processing { get; set; }

    public void ResolveNodeReferences()
    {
    }

    public Entity CreateEmptyEntity()
    {
        throw new NotSupportedException("Dummy simulation doesn't support adding entities");
    }

    public EntityRecord CreateEntityDeferred(WorldRecord activeRecording)
    {
        throw new NotSupportedException("Dummy simulation doesn't support adding entities");
    }

    public bool DestroyEntity(Entity entity)
    {
        return false;
    }

    public void DestroyAllEntities(Entity? skip = null)
    {
        throw new NotImplementedException();
    }

    public void ReportEntityDyingSoon(in Entity entity)
    {
    }

    public bool IsEntityInWorld(Entity entity)
    {
        return false;
    }

    public bool IsQueuedForDeletion(Entity entity)
    {
        return false;
    }

    public EntityCommandRecorder StartRecordingEntityCommands()
    {
        // Technically we could support this but we'd need actually some logic in the process method of ours
        throw new NotSupportedException("Dummy simulation doesn't support deferred commands");
    }

    public WorldRecord GetRecorderWorld(EntityCommandRecorder recorder)
    {
        throw new NotSupportedException("Dummy simulation doesn't support deferred commands");
    }

    public void FinishRecordingEntityCommands(EntityCommandRecorder recorder)
    {
    }

    public bool ProcessAll(float delta)
    {
        return true;
    }

    public bool ProcessLogic(float delta)
    {
        return true;
    }

    public bool HasSystemsWithPendingOperations()
    {
        return false;
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
}
