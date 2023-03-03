using System;
using Godot;

/// <summary>
///   A clump of some resource that can be found in the world
/// </summary>
public class ResourceEntity : RigidBody, IInteractableEntity, IWorldResource
{
    private bool resourceTypeSet;
    private PackedScene? resourceScene;

    public AliveMarker AliveMarker { get; } = new();

    public Spatial EntityNode => this;

    public float InteractDistanceOffset => 0;

    public Vector3? OffsetToInteractCenter => null;
    public Vector3? ExtraInteractOverlayOffset => null;

    public PackedScene WorldRepresentation => resourceScene ?? throw new NotSupportedException("Not initialized yet");

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    public void SetResource(IWorldResource resourceType)
    {
        if (resourceTypeSet)
            throw new NotSupportedException("Resource type is already set");

        resourceTypeSet = true;

        resourceScene = resourceType.WorldRepresentation;

        AddChild(resourceScene.Instance());
    }
}
