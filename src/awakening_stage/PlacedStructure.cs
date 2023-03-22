using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A structure placed in the world. May or may not be fully constructed
/// </summary>
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
    public string ReadableName
    {
        get
        {
            var typeName = Definition?.Name ?? throw new InvalidOperationException("Not initialized");
            if (Completed)
                return typeName;

            return TranslationServer.Translate("STRUCTURE_IN_PROGRESS_CONSTRUCTION").FormatSafe(typeName);
        }
    }

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
        visualsParent = GetNode<Spatial>("VisualSceneHolder");
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
