using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A clump of some resource that can be found in the world
/// </summary>
public partial class ResourceEntity : RigidBody3D, IInteractableEntity
{
    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Node3D EntityNode => this;

    [JsonIgnore]
    public float InteractDistanceOffset => 0;

    [JsonIgnore]
    public Vector3? ExtraInteractionCenterOffset => null;

    [JsonIgnore]
    public string? ExtraInteractionPopupDescription => null;

    [JsonProperty]
    public bool InteractionDisabled { get; set; }

    [JsonIgnore]

    // TODO: resources that are too heavy to carry
    public bool CanBeCarried => true;

    [JsonProperty]
    public WorldResource? ResourceType { get; private set; }

    [JsonIgnore]
    public string ReadableName => ResourceType?.ReadableName ?? throw new NotSupportedException("Not initialized yet");

    [JsonIgnore]
    public Texture2D Icon => ResourceType?.Icon ?? throw new NotSupportedException("Not initialized yet");

    [JsonIgnore]
    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    public void SetResource(WorldResource resourceType)
    {
        if (ResourceType != null)
            throw new NotSupportedException("Resource type is already set");

        ResourceType = resourceType;

        AddChild(ResourceType.WorldRepresentation.Instantiate());
    }

    public IHarvestAction? GetHarvestingInfo()
    {
        // TODO: some resources should probably be breakable into different parts
        return null;
    }

    public IEnumerable<(InteractionType Type, string? DisabledAlternativeText)>? GetExtraAvailableActions()
    {
        return null;
    }

    public bool PerformExtraAction(InteractionType interactionType)
    {
        return false;
    }
}
