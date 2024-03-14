using System;
using System.ComponentModel;
using Godot;
using Newtonsoft.Json;
using Saving.Serializers;

/// <summary>
///   A defined world resource
/// </summary>
[TypeConverter($"Saving.Serializers.{nameof(WorldResourceStringConverter)}")]
public class WorldResource : IRegistryType, IPlayerReadableName
{
    private readonly Lazy<PackedScene> worldRepresentation;
    private readonly Lazy<Texture2D> icon;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonConstructor]
    public WorldResource(string name)
    {
        Name = name;

        worldRepresentation = new Lazy<PackedScene>(LoadWorldScene);
        icon = new Lazy<Texture2D>(LoadIcon);
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

    [JsonProperty]
    public string WorldRepresentationScene { get; private set; } = string.Empty;

    [JsonProperty]
    public string InventoryIcon { get; private set; } = string.Empty;

    [JsonIgnore]
    public PackedScene WorldRepresentation => worldRepresentation.Value;

    [JsonIgnore]
    public Texture2D Icon => icon.Value;

    [JsonIgnore]
    public string ReadableName => Name;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (string.IsNullOrEmpty(WorldRepresentationScene))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing world representation scene");

        if (string.IsNullOrEmpty(InventoryIcon))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing inventory icon");
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return "Resource " + Name;
    }

    // TODO: a proper resource manager where these can be unloaded when
    private PackedScene LoadWorldScene()
    {
        return GD.Load<PackedScene>(WorldRepresentationScene);
    }

    private Texture2D LoadIcon()
    {
        return GD.Load<Texture2D>(InventoryIcon);
    }
}
