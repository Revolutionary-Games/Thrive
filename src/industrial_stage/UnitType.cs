using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   An archetype of an unit the player has. These define the fundamental thing that something is but the plan is to
///   allow full customization based on the player's species of the units they have.
/// </summary>
/// <remarks>
///   <para>
///     For now this implements the <see cref="ICityConstructionProject"/> interface for simplicity for the prototypes
///   </para>
/// </remarks>
[TypeConverter(typeof(UnitTypeStringConverter))]
public class UnitType : IRegistryType, ICityConstructionProject
{
    private readonly Lazy<PackedScene> visualScene;
    private readonly Lazy<PackedScene> spaceVisuals;
    private readonly Lazy<Texture2D> icon;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonConstructor]
    public UnitType(string name)
    {
        Name = name;

        visualScene = new Lazy<PackedScene>(LoadWorldScene);
        spaceVisuals = new Lazy<PackedScene>(LoadSpaceWorldScene);
        icon = new Lazy<Texture2D>(LoadIcon);
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

    [JsonProperty]
    public string VisualScene { get; private set; } = string.Empty;

    /// <summary>
    ///   Alternative visuals to use when in space (if not set uses the <see cref="VisualScene"/>)
    /// </summary>
    [JsonProperty]
    public string SpaceVisuals { get; private set; } = string.Empty;

    [JsonProperty]
    public string UnitIcon { get; private set; } = string.Empty;

    [JsonProperty]
    public float BuildTime { get; private set; } = 30;

    [JsonProperty]
    public bool HasSpaceMovement { get; private set; }

    [JsonProperty]
    public Dictionary<WorldResource, int> BuildCost { get; private set; } = new();

    [JsonIgnore]
    public PackedScene WorldRepresentation => visualScene.Value;

    [JsonIgnore]
    public PackedScene WorldRepresentationSpace => spaceVisuals.Value;

    [JsonIgnore]
    public Texture2D Icon => icon.Value;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    // This has to be specified for the translation extractor to work
    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    [JsonIgnore]
    public LocalizedString ProjectName =>
        new LocalizedString("CONSTRUCTION_UNIT_NAME", new LocalizedString(untranslatedName!));

    [JsonIgnore]
    public IReadOnlyDictionary<WorldResource, int> ConstructionCost => BuildCost;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (string.IsNullOrEmpty(VisualScene) || !FileAccess.FileExists(VisualScene))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing world representation scene");

        if (string.IsNullOrWhiteSpace(SpaceVisuals))
            SpaceVisuals = VisualScene;

#if DEBUG
        if (!FileAccess.FileExists(SpaceVisuals))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing space visuals scene scene");
#endif

        if (string.IsNullOrEmpty(UnitIcon))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing icon");

        if (BuildCost.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty required resources");

        if (BuildCost.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad required resource amount");

        if (BuildTime <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad build time");
    }

    public void Resolve()
    {
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return "Unit type " + Name;
    }

    // TODO: a proper resource manager where these can be unloaded when
    private PackedScene LoadWorldScene()
    {
        return GD.Load<PackedScene>(VisualScene);
    }

    private PackedScene LoadSpaceWorldScene()
    {
        return GD.Load<PackedScene>(SpaceVisuals);
    }

    private Texture2D LoadIcon()
    {
        return GD.Load<Texture2D>(UnitIcon);
    }
}
