using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Structure that is placed in the world but in space (see <see cref="PlacedStructure"/>)
/// </summary>
public class PlacedSpaceStructure : Spatial, IEntityWithNameLabel
{
    private static readonly Lazy<PackedScene> LabelScene =
        new(() => GD.Load<PackedScene>("res://src/space_stage/gui/SpaceStructureNameLabel.tscn"));

    private readonly List<SpaceStructureComponent> componentInstances = new();

#pragma warning disable CA2213
    private Spatial scaffoldingParent = null!;
    private Spatial visualsParent = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private Dictionary<WorldResource, int>? missingResourcesToFullyConstruct;

    [JsonProperty]
    private SpaceStructureDefinition? definition;

    /// <summary>
    ///   Emitted when this fleet is selected by the player
    /// </summary>
    [Signal]
    public delegate void OnSelected();

    [JsonProperty]
    public bool Completed { get; private set; }

    [JsonIgnore]
    public SpaceStructureDefinition Definition
    {
        get => definition ?? throw new InvalidOperationException("Not initialized yet");
        private set => definition = value;
    }

    [JsonIgnore]
    public string ReadableName
    {
        get
        {
            var typeName = Definition.Name;
            if (Completed)
                return typeName;

            return TranslationServer.Translate("STRUCTURE_IN_PROGRESS_CONSTRUCTION").FormatSafe(typeName);
        }
    }

    [JsonIgnore]
    public string? StructureExtraDescription
    {
        get
        {
            if (Completed)
            {
                // TODO: implement structure finished type specific text
                return TranslationServer.Translate("SPACE_STRUCTURE_NO_EXTRA_DESCRIPTION");
            }

            if (missingResourcesToFullyConstruct == null)
                return TranslationServer.Translate("SPACE_STRUCTURE_HAS_RESOURCES");

            // Display the still required resources
            string resourceAmountFormat = TranslationServer.Translate("RESOURCE_AMOUNT_SHORT");

            return TranslationServer.Translate("SPACE_STRUCTURE_WAITING_CONSTRUCTION")
                .FormatSafe(string.Join(", ",
                    missingResourcesToFullyConstruct.Select(r =>
                        resourceAmountFormat.FormatSafe(r.Key.Name, r.Value))));
        }
    }

    [JsonIgnore]
    public bool HasRequiredResourcesToConstruct => missingResourcesToFullyConstruct == null;

    [JsonProperty]
    public bool PlayerOwned { get; private set; }

    public Vector3 LabelOffset => new(0, 3, 0);

    [JsonIgnore]
    public Type NameLabelType => typeof(SpaceStructureNameLabel);

    [JsonIgnore]
    public PackedScene NameLabelScene => LabelScene.Value;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    public override void _Ready()
    {
        scaffoldingParent = GetNode<Spatial>("Scaffolding");
        visualsParent = GetNode<Spatial>("Visuals");
    }

    public void Init(SpaceStructureDefinition structureDefinition, bool playerOwned, bool fullyConstructed = false)
    {
        Definition = structureDefinition;
        PlayerOwned = playerOwned;

        if (!fullyConstructed)
        {
            missingResourcesToFullyConstruct = structureDefinition.RequiredResources.CloneShallow();

            // Setup scaffolding
            scaffoldingParent.AddChild(structureDefinition.ScaffoldingScene.Instance());
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

    public void ProcessSpace(float delta, ISocietyStructureDataAccess societyData)
    {
        foreach (var component in componentInstances)
        {
            component.ProcessSpace(delta, societyData);
        }
    }

    public IEnumerable<(InteractionType Type, string? DisabledAlternativeText)> GetAvailableActions()
    {
        var result = new List<(InteractionType Type, string? DisabledAlternativeText)>();

        if (!Completed)
        {
            result.Add((InteractionType.Destroy, null));
            return result;
        }

        foreach (var component in componentInstances)
        {
            component.GetExtraAvailableActions(result);
        }

        result.Add((InteractionType.Destroy, null));
        return result;
    }

    public bool PerformAction(InteractionType interactionType)
    {
        if (interactionType == InteractionType.Destroy)
        {
            // TODO: maybe refund some resources?
            this.DestroyAndQueueFree();
            return true;
        }

        if (!Completed)
            return false;

        foreach (var component in componentInstances)
        {
            if (component.PerformExtraAction(interactionType))
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Takes available resources needed to fully construct this
    /// </summary>
    /// <param name="availableResources">The available resources</param>
    /// <returns>True when all resources are now taken</returns>
    public bool DepositBulkResources(IResourceContainer availableResources)
    {
        if (missingResourcesToFullyConstruct == null)
            return true;

        // TODO: would probably be better to allow space structure to take a bit of resources at once
        if (!availableResources.TakeResourcesIfPossible(missingResourcesToFullyConstruct))
            return false;

        missingResourcesToFullyConstruct = null;
        return true;
    }

    public void OnFinishConstruction()
    {
        if (!HasRequiredResourcesToConstruct)
            GD.PrintErr("Structure force completed even though it still needs resources");

        OnCompleted();
    }

    public T? GetComponent<T>()
        where T : SpaceStructureComponent
    {
        foreach (var component in componentInstances)
        {
            if (component is T casted)
                return casted;
        }

        return null;
    }

    public void ForceCompletion()
    {
        if (Completed)
            return;

        OnCompleted();
    }

    public void OnSelectedThroughLabel()
    {
        EmitSignal(nameof(OnSelected));
    }

    private void OnCompleted()
    {
        if (Definition == null)
            throw new InvalidOperationException("Definition not set");

        Completed = true;
        missingResourcesToFullyConstruct = null;

        // Remove the scaffolding
        scaffoldingParent.QueueFreeChildren();

        // For now we don't have an animation so we just create the actual visuals at this point
        visualsParent.AddChild(Definition.WorldRepresentation.Instance());

        // Create the components
        foreach (var factory in Definition.Components.Factories)
        {
            // TODO: add the this parameter if it will be needed
            // componentInstances.Add(factory.Create(this));
            componentInstances.Add(factory.Create());
        }
    }
}
