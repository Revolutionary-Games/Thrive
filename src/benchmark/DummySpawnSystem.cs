using System;
using Godot;

/// <summary>
///   A dummy class that satisfies the spawner system interface
/// </summary>
public class DummySpawnSystem : ISpawnSystem
{
    private readonly Action<ISpawned>? addTrackedCallback;

    public DummySpawnSystem(Action<ISpawned>? addTrackedCallback = null)
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

    public void Process(float delta, Vector3 playerPosition)
    {
    }

    public void AddEntityToTrack(ISpawned entity)
    {
        addTrackedCallback?.Invoke(entity);
    }

    public bool IsUnderEntityLimitForReproducing()
    {
        return AllowReproduction;
    }
}
