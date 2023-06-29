using System;
using DefaultEcs;
using Godot;

/// <summary>
///   A dummy class that satisfies the spawner system interface
/// </summary>
public class DummySpawnSystem : ISpawnSystem
{
    private readonly Action<Entity>? addTrackedCallback;

    public DummySpawnSystem(Action<Entity>? addTrackedCallback = null)
    {
        this.addTrackedCallback = addTrackedCallback;
    }

    public bool AllowReproduction { get; set; }

    public void Init()
    {
    }

    public void Clear()
    {
    }

    public void DespawnAll()
    {
    }

    public void ReportPlayerPosition(Vector3 position)
    {
    }

    public void Update(float delta)
    {
    }

    public void NotifyExternalEntitySpawned(Entity entity, float despawnRadiusSquared, float entityWeight)
    {
        addTrackedCallback?.Invoke(entity);
    }

    public bool IsUnderEntityLimitForReproducing()
    {
        return AllowReproduction;
    }
}
