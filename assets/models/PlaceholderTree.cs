﻿using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Until plants are procedurally generated species, this serves as a source of wood
/// </summary>
public partial class PlaceholderTree : StaticBody3D, IInteractableEntity, IHarvestAction
{
    private const float AnimationDuration = 1.5f;
    private const float PostAnimationDuration = 4;

    private readonly WorldResource woodResource = SimulationParameters.Instance.GetWorldResource("wood");

    private bool harvested;
    private int availableWood = 4;

    private double animationTime;
    private Transform3D animationStart;
    private Transform3D animationEnd;

    public AliveMarker AliveMarker { get; } = new();
    public Node3D EntityNode => this;

    /// <summary>
    ///   Placeholder name, intentionally not translated (second part is now in
    ///   <see cref="ExtraInteractionPopupDescription"/>)
    /// </summary>
    public string ReadableName => "Placeholder Tree";

    public Texture2D Icon => woodResource.Icon;

    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }
    public float InteractDistanceOffset => 0.5f;
    public Vector3? ExtraInteractionCenterOffset => new Vector3(0, 1.5f, 0);

    public string ExtraInteractionPopupDescription =>
        "All plants will be procedurally generated by auto-evo in the future";

    public bool InteractionDisabled { get; set; }
    public bool CanBeCarried => false;

    public override void _Process(double delta)
    {
        if (!harvested)
            return;

        // Play some kind of falling animation, not a very good one
        animationTime += delta;

        if (animationTime > AnimationDuration + PostAnimationDuration)
        {
            this.DestroyAndQueueFree();
        }
        else
        {
            var progress = Math.Min(animationTime / AnimationDuration, 1);

            Transform = animationStart.InterpolateWith(animationEnd, (float)progress);
        }
    }

    public IHarvestAction? GetHarvestingInfo()
    {
        // Can only harvest once
        if (harvested)
            return null;

        return this;
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

    public EquipmentCategory? CheckRequiredTool(ICollection<EquipmentCategory> availableTools)
    {
        if (!availableTools.Contains(EquipmentCategory.Axe))
            return EquipmentCategory.Axe;

        return null;
    }

    public List<IInteractableEntity> PerformHarvest()
    {
        harvested = true;
        InteractionDisabled = true;

        // Set up a really simple falling animation
        animationStart = Transform;
        animationEnd = new Transform3D(new Basis(animationStart.Basis.GetRotationQuaternion() *
                new Quaternion(new Vector3(0, 0, 1), MathF.PI * 0.5f)),
            animationStart.Origin - new Vector3(0, -0.3f, 0));

        // Disable collision while being destroyed
        CollisionLayer = 0;
        CollisionMask = 0;

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
