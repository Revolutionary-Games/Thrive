using System;
using DefaultEcs;

/// <summary>
///   For use in the prototypes not yet converted to using world simulations
/// </summary>
public class DummyWorldSimulation : IWorldSimulation
{
    public Entity CreateEmptyEntity()
    {
        throw new NotSupportedException("Dummy simulation doesn't support adding entities");
    }

    public bool DestroyEntity(Entity entity)
    {
        return false;
    }

    public void DestroyAllEntities(Entity? skip = null)
    {
        throw new System.NotImplementedException();
    }

    public bool IsEntityInWorld(Entity entity)
    {
        return false;
    }

    public bool IsQueuedForDeletion(Entity entity)
    {
        return false;
    }

    public void Dispose()
    {
    }
}
