using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A structure placed in the world. May or may not be fully constructed
/// </summary>
public class PlacedStructure : Spatial, IInteractableEntity, IConstructable
{
    private readonly List<StructureComponent> componentInstances = new();

#pragma warning disable CA2213
    private Spatial scaffoldingParent = null!;
    private Spatial visualsParent = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private Dictionary<WorldResource, int>? missingResourcesToFullyConstruct;

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

    // TODO: a separate interact offset for when constructing versus when built
    [JsonIgnore]
    public Vector3? ExtraInteractionCenterOffset =>
        Definition?.InteractOffset ?? throw new InvalidOperationException("Not initialized");

    public string? ExtraInteractionPopupDescription
    {
        get
        {
            if (Completed)
                return null;

            if (missingResourcesToFullyConstruct == null)
                return TranslationServer.Translate("STRUCTURE_HAS_REQUIRED_RESOURCES_TO_BUILD");

            // Display the still required resources
            string resourceAmountFormat = TranslationServer.Translate("RESOURCE_AMOUNT_SHORT");

            return TranslationServer.Translate("STRUCTURE_REQUIRED_RESOURCES_TO_FINISH")
                .FormatSafe(string.Join(", ",
                    missingResourcesToFullyConstruct.Select(r =>
                        resourceAmountFormat.FormatSafe(r.Key.Name, r.Value))));
        }
    }

    public bool InteractionDisabled { get; set; }

    [JsonIgnore]
    public bool CanBeCarried => false;

    [JsonIgnore]
    public bool DepositActionAllowed => !Completed && missingResourcesToFullyConstruct != null;

    [JsonIgnore]
    public bool AutoTakesResources => true;

    [JsonIgnore]
    public bool HasRequiredResourcesToConstruct => missingResourcesToFullyConstruct == null;

    [JsonIgnore]
    public float TimedActionDuration => 5;

    public override void _Ready()
    {
        scaffoldingParent = GetNode<Spatial>("ScaffoldingHolder");
        visualsParent = GetNode<Spatial>("VisualSceneHolder");
    }

    public void Init(StructureDefinition definition, bool fullyConstructed = false)
    {
        Definition = definition;

        visualsParent.AddChild(definition.WorldRepresentation.Instance());

        // TODO: move the physics from the visual scene to this type directly

        if (!fullyConstructed)
        {
            missingResourcesToFullyConstruct = definition.RequiredResources.CloneShallow();

            // Setup scaffolding
            scaffoldingParent.AddChild(definition.ScaffoldingScene.Instance());

            // And the real visuals but placed really low to play a simple building animation
            visualsParent.Translation = new Vector3(0, definition.WorldSize.y * -0.9f, 0);
        }
        else
        {
            OnCompleted();
        }
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    /// <summary>
    ///   Processes this structure when in the society stage
    /// </summary>
    /// <param name="delta">Time since last update</param>
    /// <param name="societyData">Access to the data where the structure accesses and writes things</param>
    public void ProcessSociety(float delta, ISocietyStructureDataAccess societyData)
    {
        foreach (var component in componentInstances)
        {
            component.ProcessSociety(delta, societyData);
        }
    }

    public IHarvestAction? GetHarvestingInfo()
    {
        return null;
    }

    public IEnumerable<(InteractionType Type, string? DisabledAlternativeText)>? GetExtraAvailableActions()
    {
        if (!Completed)
            return null;

        var result = new List<(InteractionType Type, string? DisabledAlternativeText)>();

        foreach (var component in componentInstances)
        {
            component.GetExtraAvailableActions(result);
        }

        return result;
    }

    public bool PerformExtraAction(InteractionType interactionType)
    {
        if (!Completed)
            return false;

        foreach (var component in componentInstances)
        {
            if (component.PerformExtraAction(interactionType))
                return true;
        }

        return false;
    }

    public IEnumerable<InventorySlotData>? GetWantedItems(IInventory availableItems)
    {
        if (missingResourcesToFullyConstruct == null)
            return null;

        return availableItems.FindAvailableResources(missingResourcesToFullyConstruct);
    }

    public void DepositItems(IEnumerable<IInventoryItem> items)
    {
        if (missingResourcesToFullyConstruct == null)
        {
            GD.PrintErr("Items attempted to be given to a structure that isn't missing anything");
            return;
        }

        // Mark off resources that are now given
        foreach (var item in items)
        {
            var resource = item.ResourceFromItem();

            if (resource == null)
            {
                GD.PrintErr("Structure given an item that is not a resource");
                continue;
            }

            if (!missingResourcesToFullyConstruct.TryGetValue(resource, out var missingAmount))
            {
                GD.PrintErr("Structure given an item it didn't ask for, this item is lost!");
                continue;
            }

            --missingAmount;

            if (missingAmount <= 0)
            {
                missingResourcesToFullyConstruct.Remove(resource);
            }
            else
            {
                missingResourcesToFullyConstruct[resource] = missingAmount;
            }
        }

        // Set to null when empty to mark all items deposited
        if (missingResourcesToFullyConstruct.Count < 1)
            missingResourcesToFullyConstruct = null;
    }

    /// <summary>
    ///   <see cref="DepositItems"/> variant for taking from bulk storage, only takes when all resources are available
    /// </summary>
    /// <param name="availableResources">The available resources</param>
    /// <returns>True when all resources are now taken</returns>
    public bool DepositBulkResources(IResourceContainer availableResources)
    {
        // Allow calling this when this doesn't actually need anything
        if (missingResourcesToFullyConstruct == null)
            return true;

        if (!availableResources.TakeResourcesIfPossible(missingResourcesToFullyConstruct))
            return false;

        missingResourcesToFullyConstruct = null;
        return true;
    }

    public void ReportActionProgress(float progress)
    {
        if (Definition == null)
            throw new InvalidOperationException("Not initialized");

        // Update the construction animation
        // TODO: slerp if this can be too jittery otherwise
        visualsParent.Translation = new Vector3(0, Definition.WorldSize.y * (-0.9f + 0.9f * progress), 0);
    }

    public void OnFinishTimeTakingAction()
    {
        if (!HasRequiredResourcesToConstruct)
            GD.PrintErr("Structure force completed (due to an action) even though it still needs resources");

        OnCompleted();
    }

    public T? GetComponent<T>()
        where T : StructureComponent
    {
        foreach (var component in componentInstances)
        {
            if (component is T casted)
                return casted;
        }

        return null;
    }

    /// <summary>
    ///   Forces the structure to become immediately completed without any actions or needed resources
    /// </summary>
    public void ForceCompletion()
    {
        if (Completed)
            return;

        OnCompleted();
    }

    private void OnCompleted()
    {
        if (Definition == null)
            throw new InvalidOperationException("Definition not set");

        Completed = true;
        missingResourcesToFullyConstruct = null;

        // Remove the scaffolding
        scaffoldingParent.QueueFreeChildren();

        // Ensure visuals are at the right position
        visualsParent.Translation = Vector3.Zero;

        // Create the components
        foreach (var factory in Definition.Components.Factories)
        {
            componentInstances.Add(factory.Create(this));
        }
    }
}
