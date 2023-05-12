using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A concrete, equipable piece of equipment
/// </summary>
public class Equipment : RigidBody, IInteractableEntity
{
    public Equipment(EquipmentDefinition definition)
    {
        Definition = definition;

        AddChild(definition.WorldRepresentation.Instance());

        // TODO: physics customization
        var owner = CreateShapeOwner(this);
        ShapeOwnerAddShape(owner, new BoxShape
        {
            Extents = new Vector3(0.5f, 0.5f, 0.5f),
        });
    }

    [JsonProperty]
    public EquipmentDefinition Definition { get; private set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    [JsonIgnore]
    public string ReadableName => Definition.Name;

    [JsonIgnore]
    public Texture Icon => Definition.Icon;

    [JsonIgnore]
    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }

    [JsonIgnore]
    public float InteractDistanceOffset => 0;

    [JsonIgnore]
    public Vector3? ExtraInteractionCenterOffset => null;

    [JsonIgnore]
    public string? ExtraInteractionPopupDescription => null;

    [JsonIgnore]
    public bool InteractionDisabled { get; set; }

    [JsonIgnore]
    public bool CanBeCarried => true;

    public IHarvestAction? GetHarvestingInfo()
    {
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

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }
}
