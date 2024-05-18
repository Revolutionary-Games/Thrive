using Godot;
using Newtonsoft.Json;

/// <summary>
///   General data about an available upgrade that is either present or not (some more in-depth upgrades have their
///   own data classes to store all of their data)
/// </summary>
public class AvailableUpgrade : IRegistryType
{
    private LoadedSceneWithModelInfo loadedSceneData;

#pragma warning disable 169,649 // Used through reflection
    /// <summary>
    ///   A path to a scene to display this organelle with. If empty won't have a display model.
    /// </summary>
    [JsonProperty]
    private SceneWithModelInfo graphics;

    private string? untranslatedName;
    private string? untranslatedDescription;
#pragma warning restore 169,649

    /// <summary>
    ///   When true this is the default upgrade shown in the upgrade GUI for reverting upgrades
    /// </summary>
    [JsonProperty]
    public bool IsDefault { get; private set; }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; } = null!;

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedDescription))]
    public string Description { get; private set; } = null!;

    /// <summary>
    ///   Cost of selecting this upgrade in the editor
    /// </summary>
    [JsonProperty]
    public int MPCost { get; private set; }

    [JsonProperty]
    public string IconPath { get; private set; } = string.Empty;

    /// <summary>
    ///   Loaded icon for display in GUIs
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: only load while in the right stage for this upgrade to save on resources
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public Texture2D? LoadedIcon { get; private set; }

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        if (string.IsNullOrEmpty(Description))
            throw new InvalidRegistryDataException(name, GetType().Name, "Description is not set");

        if (IsDefault)
        {
            // For the default upgrade we don't have an icon right now, but might have something in the future
            IconPath = string.Empty;
        }
        else
        {
            if (string.IsNullOrEmpty(IconPath))
                throw new InvalidRegistryDataException(name, GetType().Name, "IconPath is missing");
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void Resolve()
    {
        if (!string.IsNullOrEmpty(IconPath))
            LoadedIcon = GD.Load<Texture2D>(IconPath);

        // Preload the scene for instantiating in microbes
        // TODO: switch this to only load when loading the microbe stage to not load this in the future when we have
        // playable stages that don't need these graphics
        if (!string.IsNullOrEmpty(graphics.ScenePath))
        {
            loadedSceneData.LoadFrom(graphics);
        }
    }

    public LoadedSceneWithModelInfo? TryGetGraphicsScene()
    {
        return loadedSceneData;
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
