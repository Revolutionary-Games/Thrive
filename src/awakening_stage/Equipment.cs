using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A concrete, equipable piece of equipment
/// </summary>
public partial class Equipment : RigidBody3D, IInteractableEntity
{
    public Equipment(EquipmentDefinition definition)
    {
        Definition = definition;

        AddChild(definition.WorldRepresentation.Instantiate());

        // TODO: physics customization
        var owner = CreateShapeOwner(this);
        ShapeOwnerAddShape(owner, new BoxShape3D
        {
            Size = new Vector3(1, 1, 1),
        });
    }

    [JsonProperty]
    public EquipmentDefinition Definition { get; private set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Node3D EntityNode => this;

    [JsonIgnore]
    public string ReadableName => Definition.Name;

    [JsonIgnore]
    public Texture2D Icon => Definition.Icon;

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
