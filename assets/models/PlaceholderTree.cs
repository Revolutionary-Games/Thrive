using System;
using Godot;

/// <summary>
///   Until plants are procedurally generated species, this serves as a source of wood
/// </summary>
public class PlaceholderTree : StaticBody, IInteractableEntity
{
    private readonly WorldResource woodResource = SimulationParameters.Instance.GetWorldResource("wood");

    public AliveMarker AliveMarker { get; } = new();
    public Spatial EntityNode => this;

    public string ReadableName => "Placeholder Tree (plants will be procedurally generated in the future)";

    public Texture Icon => woodResource.Icon;

    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }
    public float InteractDistanceOffset => 0.5f;
    public Vector3? ExtraInteractOverlayOffset => null;
    public bool InteractionDisabled { get; set; }
    public bool CanBeCarried => false;

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }
}
