using System;
using Arch.Buffer;
using Arch.Core;

/// <summary>
///   For use in the prototypes not yet converted to using world simulations
/// </summary>
public class DummyWorldSimulation : IWorldSimulation
{
    public World EntitySystem { get; } = World.Create();
    public bool Processing { get; set; }
    public float WorldTimeScale { get; set; } = 1;

    public void ResolveNodeReferences()
    {
    }

    public Entity CreateEntityDeferred(CommandBuffer recorder, ComponentType[] types)
    {
        throw new NotSupportedException("Dummy simulation doesn't support adding entities");
    }

    public Entity CreateEmptyEntity(ComponentType[] types)
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

    CommandBuffer IWorldSimulation.StartRecordingEntityCommands()
    {
        throw new NotSupportedException("Dummy simulation doesn't support deferred commands");
    }

    public void FinishRecordingEntityCommands(CommandBuffer recorder)
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

    public float GetAndResetTrackedSimulationSpeedRatio()
    {
        return 1;
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
