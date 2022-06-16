using Godot;
using Newtonsoft.Json;

public class EasterEggDefinition : IRegistryType
{
    public string? DisplayScene;

    public string? DisplaySceneModelPath;

    public string? DisplaySceneAnimation;

    [JsonIgnore]
    public PackedScene? LoadedScene;

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
    }

    public void Resolve(SimulationParameters parameters)
    {
        // Preload the scene for instantiating in microbes
        if (!string.IsNullOrEmpty(DisplayScene))
        {
            LoadedScene = GD.Load<PackedScene>(DisplayScene);
        }
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}