using System.Collections.Generic;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   List of resources a stage uses for use with preloading the graphics
/// </summary>
public class StageResourcesList : IRegistryType
{
    public List<VisualResourceIdentifier> RequiredVisualResources = new();

    public List<string> RequiredScenes = new();

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (RequiredVisualResources.Count < 1 && RequiredScenes.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "No resources specified");
    }

    public void ApplyTranslations()
    {
    }
}
