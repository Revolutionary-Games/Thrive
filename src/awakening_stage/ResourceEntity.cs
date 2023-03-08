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

    [JsonProperty]
    private string? iconPath;

    private Texture? icon;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    [JsonIgnore]
    public float InteractDistanceOffset => 0;

    [JsonIgnore]
    public Vector3? ExtraInteractOverlayOffset => null;

    [JsonProperty]
    public bool InteractionDisabled { get; set; }

    [JsonIgnore]

    // TODO: resources that are too heavy to carry
    public bool CanBeCarried => true;

    [JsonIgnore]
    public string ReadableName => TranslationServer.Translate(untranslatedName);

    [JsonIgnore]
    public string InternalName => untranslatedName;

    [JsonIgnore]
    public PackedScene WorldRepresentation => resourceScene ?? throw new NotSupportedException("Not initialized yet");

    [JsonIgnore]
    public Texture Icon
    {
        get
        {
            if (iconPath == null)
                throw new NotSupportedException("Icon path is not initialized");

            icon ??= GD.Load<Texture>(iconPath);
            return icon;
        }
    }

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

        iconPath = resourceType.Icon.ResourcePath;
        icon = resourceType.Icon;

        AddChild(resourceScene.Instance());
    }
}
