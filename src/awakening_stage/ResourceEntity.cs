using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A clump of some resource that can be found in the world
/// </summary>
public class ResourceEntity : RigidBody, IInteractableEntity, IWorldResource
{
    [JsonProperty]
    private bool resourceTypeSet;

    private PackedScene? resourceScene;

    [JsonProperty]
    private string untranslatedName = string.Empty;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    [JsonIgnore]
    public float InteractDistanceOffset => 0;

    [JsonIgnore]
    public Vector3? ExtraInteractOverlayOffset => null;

    [JsonIgnore]

    // TODO: resources that are too heavy to carry
    public bool CanBeCarried => true;

    [JsonIgnore]
    public string ReadableName => TranslationServer.Translate(untranslatedName);

    [JsonIgnore]
    public string InternalName => untranslatedName;

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
        untranslatedName = resourceType.InternalName;

        AddChild(resourceScene.Instance());
    }
}
