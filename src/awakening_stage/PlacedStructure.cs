using System;
using Godot;
using Newtonsoft.Json;

public class PlacedStructure : Spatial, IInteractableEntity
{
#pragma warning disable CA2213
    private Spatial scaffoldingParent = null!;
    private Spatial visualsParent = null!;
#pragma warning restore CA2213

    [JsonProperty]
    public bool Completed { get; private set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    public StructureDefinition? Definition { get; private set; }

    [JsonIgnore]
    public string ReadableName => Definition?.Name ?? throw new InvalidOperationException("Not initialized");

    [JsonIgnore]
    public Texture Icon => Definition?.Icon ?? throw new InvalidOperationException("Not initialized");

    [JsonIgnore]
    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }

    [JsonIgnore]
    public float InteractDistanceOffset => 0;

    [JsonIgnore]
    public Vector3? ExtraInteractOverlayOffset =>
        Definition?.InteractOffset ?? throw new InvalidOperationException("Not initialized");

    public bool InteractionDisabled { get; set; }

    [JsonIgnore]
    public bool CanBeCarried => false;

    public override void _Ready()
    {
        scaffoldingParent = GetNode<Spatial>("ScaffoldingHolder");
        visualsParent = GetNode<Spatial>("VisualsHolder");
    }

    public void Init(StructureDefinition definition)
    {
        Definition = definition;

        // Setup scaffolding
        scaffoldingParent.AddChild(definition.ScaffoldingScene.Instance());

        // And the real visuals but placed really low to play a simple building animation
        visualsParent.AddChild(definition.WorldRepresentation.Instance());
        visualsParent.Translation = new Vector3(0, definition.WorldSize.y * -0.9f, 0);
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    public IHarvestAction? GetHarvestingInfo()
    {
        return null;
    }

    private void OnCompleted()
    {
        Completed = true;

        // Remove the scaffolding
        scaffoldingParent.QueueFreeChildren();

        // Ensure visuals are at the right position
        visualsParent.Translation = Vector3.Zero;
    }
}
