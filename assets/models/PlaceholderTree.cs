using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Until plants are procedurally generated species, this serves as a source of wood
/// </summary>
public class PlaceholderTree : StaticBody, IInteractableEntity, IHarvestAction
{
    private readonly WorldResource woodResource = SimulationParameters.Instance.GetWorldResource("wood");

    private bool harvested;
    private int availableWood = 4;

    private float animationTime;
    private Transform animationStart;
    private Transform animationEnd;

    public AliveMarker AliveMarker { get; } = new();
    public Spatial EntityNode => this;

    /// <summary>
    ///   Placeholder name, intentionally not translated
    /// </summary>
    public string ReadableName => "Placeholder Tree (plants will be procedurally generated in the future)";

    public Texture Icon => woodResource.Icon;

    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }
    public float InteractDistanceOffset => 0.5f;
    public Vector3? ExtraInteractOverlayOffset => new Vector3(0, 1.5f, 0);
    public bool InteractionDisabled { get; set; }
    public bool CanBeCarried => false;

    public override void _Process(float delta)
    {
        if (!harvested)
            return;

        // Play some kind of falling animation, not a very good one
        animationTime += delta;
        float animationDuration = 1.5f;

        if (animationTime > animationDuration)
        {
            this.DestroyAndQueueFree();
        }
        else
        {
            var progress = animationTime / animationDuration;

            Transform = animationStart.InterpolateWith(animationEnd, progress);
        }
    }

    public IHarvestAction? GetHarvestingInfo()
    {
        // Can only harvest once
        if (harvested)
            return null;

        return this;
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    public EquipmentCategory? CheckRequiredTool(ICollection<EquipmentCategory> availableTools)
    {
        if (!availableTools.Contains(EquipmentCategory.Axe))
            return EquipmentCategory.Axe;

        return null;
    }

    public List<IInteractableEntity> PerformHarvest()
    {
        harvested = true;

        // Setup a really simple falling animation
        animationStart = Transform;
        animationEnd = new Transform(new Basis(animationStart.basis.Quat() * new Quat(0, 0, 1, Mathf.Pi * 0.5f)),
            animationStart.origin - new Vector3(0, -0.3f, 0));

        // Disable collision while being destroyed
        CollisionLayer = 0;

        // Create the resource entities to drop
        var result = new List<IInteractableEntity>();
        var scene = SpawnHelpers.LoadResourceEntityScene();

        for (int i = 0; i < availableWood; ++i)
        {
            result.Add(SpawnHelpers.CreateHarvestedResourceEntity(woodResource, scene));
        }

        return result;
    }
}
