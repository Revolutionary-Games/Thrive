using DefaultEcs.Command;
using Godot;

/// <summary>
///   A dummy class that satisfies the spawner system interface
/// </summary>
public class DummySpawnSystem : ISpawnSystem
{
    private readonly OnEntityAddedCallback? addTrackedCallback;

    public DummySpawnSystem(OnEntityAddedCallback? addTrackedCallback = null)
    {
        this.addTrackedCallback = addTrackedCallback;
    }

    public delegate void OnEntityAddedCallback(in EntityRecord entityRecord);

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

    public void NotifyExternalEntitySpawned(in EntityRecord entity, float despawnRadiusSquared, float entityWeight)
    {
        addTrackedCallback?.Invoke(entity);
    }

    public bool IsUnderEntityLimitForReproducing()
    {
        return AllowReproduction;
    }
}
