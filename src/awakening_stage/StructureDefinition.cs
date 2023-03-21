using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[TypeConverter(typeof(StructureStringConverter))]
public class StructureDefinition : IRegistryType
{
    private readonly Lazy<PackedScene> worldRepresentation;
    private readonly Lazy<PackedScene> ghostRepresentation;
    private readonly Lazy<PackedScene> scaffoldingScene;
    private readonly Lazy<Texture> icon;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonConstructor]
    public StructureDefinition(string name)
    {
        Name = name;

        worldRepresentation = new Lazy<PackedScene>(LoadWorldScene);
        ghostRepresentation = new Lazy<PackedScene>(LoadGhostScene);
        scaffoldingScene = new Lazy<PackedScene>(LoadScaffoldingScene);
        icon = new Lazy<Texture>(LoadIcon);
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

    [JsonProperty]
    public string WorldRepresentationScene { get; private set; } = string.Empty;

    [JsonProperty]
    public string GhostScenePath { get; private set; } = string.Empty;

    [JsonProperty]
    public string ScaffoldingScenePath { get; private set; } = string.Empty;

    [JsonProperty]
    public string BuildingIcon { get; private set; } = string.Empty;

    [JsonProperty]
    public Vector3 WorldSize { get; private set; }

    [JsonProperty]
    public Vector3 InteractOffset { get; private set; }

    [JsonProperty]
    public Dictionary<WorldResource, int> RequiredResources { get; private set; } = new();

    [JsonProperty]
    public Dictionary<WorldResource, int> ScaffoldingCost { get; private set; } = new();

    [JsonIgnore]
    public PackedScene WorldRepresentation => worldRepresentation.Value;

    [JsonIgnore]
    public PackedScene GhostScene => ghostRepresentation.Value;

    [JsonIgnore]
    public PackedScene ScaffoldingScene => scaffoldingScene.Value;

    [JsonIgnore]
    public Texture Icon => icon.Value;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (string.IsNullOrEmpty(WorldRepresentationScene))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing world representation scene");

        if (string.IsNullOrEmpty(GhostScenePath))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing ghost scene");

        if (string.IsNullOrEmpty(ScaffoldingScenePath))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing scaffolding scene");

        if (string.IsNullOrEmpty(BuildingIcon))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing icon");

        if (RequiredResources.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty required resources");

        if (RequiredResources.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad required resource amount");

        if (ScaffoldingCost.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty scaffolding cost");

        if (ScaffoldingCost.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad required scaffolding resource amount");

        /*if (WorldSize.x <= 0 || WorldSize.y <= 0 || WorldSize.z <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad world size");*/
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return "Structure type " + Name;
    }

    // TODO: a proper resource manager where these can be unloaded when
    private PackedScene LoadWorldScene()
    {
        return GD.Load<PackedScene>(WorldRepresentationScene);
    }

    private PackedScene LoadGhostScene()
    {
        return GD.Load<PackedScene>(GhostScenePath);
    }

    private PackedScene LoadScaffoldingScene()
    {
        return GD.Load<PackedScene>(ScaffoldingScenePath);
    }

    private Texture LoadIcon()
    {
        return GD.Load<Texture>(BuildingIcon);
    }
}
