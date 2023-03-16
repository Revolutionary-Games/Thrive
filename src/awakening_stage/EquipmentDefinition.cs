using System;
using System.ComponentModel;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Definition for an equipment type
/// </summary>
/// <remarks>
///   <para>
///     Note that in the future we need to allow the player to customize these so a similar system as for difficulty
///     for saving and loading these are needed to allow the switch to customized objects
///   </para>
/// </remarks>
[TypeConverter(typeof(EquipmentDefinitionStringConverter))]
public class EquipmentDefinition : IRegistryType
{
    private readonly Lazy<PackedScene> worldRepresentation;
    private readonly Lazy<Texture> icon;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonConstructor]
    public EquipmentDefinition(string name)
    {
        Name = name;

        worldRepresentation = new Lazy<PackedScene>(LoadWorldScene);
        icon = new Lazy<Texture>(LoadIcon);
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

    [JsonProperty]
    public string WorldRepresentationScene { get; private set; } = string.Empty;

    [JsonProperty]
    public string InventoryIcon { get; private set; } = string.Empty;

    [JsonProperty]
    public EquipmentSlot Slot { get; private set; }

    [JsonProperty]
    public EquipmentCategory Category { get; private set; }

    /// <summary>
    ///   How strong this item is in the <see cref="Category"/> this is in. Used to easily define more powerful
    ///   variants (technologically superior) of existing items, for example metal tools versus stone tools
    /// </summary>
    [JsonProperty]
    public float ItemPower { get; private set; }

    [JsonIgnore]
    public PackedScene WorldRepresentation => worldRepresentation.Value;

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

        if (string.IsNullOrEmpty(InventoryIcon))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing inventory icon");

        if (ItemPower is <= 0 or float.MaxValue)
            throw new InvalidRegistryDataException(name, GetType().Name, "Item power is incorrect");
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return "Equipment " + Name;
    }

    // TODO: a proper resource manager where these can be unloaded when
    private PackedScene LoadWorldScene()
    {
        return GD.Load<PackedScene>(WorldRepresentationScene);
    }

    private Texture LoadIcon()
    {
        return GD.Load<Texture>(InventoryIcon);
    }
}
