using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   List of resources a stage uses for use with preloading the graphics
/// </summary>
public class StageResourcesList : IRegistryType
{
    [JsonIgnore]
    public List<VisualResourceData> RequiredVisualResources = new();

    public List<SceneResource> RequiredScenes = new();

    [JsonProperty("RequiredVisualResources")]
    private List<VisualResourceIdentifier>? requiredVisualResourcesRaw;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    [JsonIgnore]
    public MainGameState Stage { get; set; }

    public void Check(string name)
    {
        if (!Enum.TryParse(InternalName, out MainGameState identifier))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Failed to parse internal name as game state");
        }

        Stage = identifier;

        if (Stage == MainGameState.Invalid)
            throw new InvalidRegistryDataException(name, GetType().Name, "Stage identifier type is invalid");

        if ((requiredVisualResourcesRaw == null || requiredVisualResourcesRaw.Count < 1) && RequiredScenes.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "No resources specified");
    }

    public void Resolve(SimulationParameters simulationParameters)
    {
        if (requiredVisualResourcesRaw != null)
        {
            foreach (var resourceIdentifier in requiredVisualResourcesRaw)
            {
                RequiredVisualResources.Add(simulationParameters.GetVisualResource(resourceIdentifier));
            }
        }
    }

    public void ApplyTranslations()
    {
    }
}
