using Godot;

/// <summary>
///   All nodes that can be spawned with the spawn system must inherit this class
/// </summary>
public abstract class SpawnedRigidBody : RigidBody, IEntity
{
    private Sector? currentSector;

    public abstract AliveMarker AliveMarker { get; }
    public abstract Node EntityNode { get; }

    public Sector CurrentSector
    {
        get
        {
            var position = Translation;
            if (currentSector?.IsInSector(position) != true)
                currentSector = Sector.FromPosition(position);

            return currentSector.Value;
        }
    }

    public abstract void OnDestroyed();
}
