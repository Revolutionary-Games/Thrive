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

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonConstructor]
    public StructureDefinition(string name)
    {
        Name = name;

        worldRepresentation = new Lazy<PackedScene>(LoadWorldScene);
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

    [JsonProperty]
    public string WorldRepresentationScene { get; private set; } = string.Empty;

    [JsonProperty]
    public Vector3 WorldSize { get; private set; }

    [JsonProperty]
    public Vector3 InteractOffset { get; private set; }

    [JsonProperty]
    public Dictionary<WorldResource, int> RequiredResources { get; private set; } = new();

    [JsonIgnore]
    public PackedScene WorldRepresentation => worldRepresentation.Value;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (string.IsNullOrEmpty(WorldRepresentationScene))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing world representation scene");

        if (RequiredResources.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty required resources");

        if (RequiredResources.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad required resource amount");

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
}
