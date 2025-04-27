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

    /// <summary>
    ///   If set, then grabs all resources from the given stage
    /// </summary>
    public MainGameState InheritFrom = MainGameState.Invalid;

    // Set through JSON
#pragma warning disable CS0649
    [JsonProperty("RequiredVisualResources")]
    private List<VisualResourceIdentifier>? requiredVisualResourcesRaw;
#pragma warning restore CS0649

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

        if (InheritFrom != MainGameState.Invalid && InheritFrom == Stage)
            throw new InvalidRegistryDataException(name, GetType().Name, "Cannot inherit from itself");

        // Fail if duplicate items are in the data lists
#if DEBUG
        if (requiredVisualResourcesRaw != null)
        {
            if (new HashSet<VisualResourceIdentifier>(requiredVisualResourcesRaw).Count !=
                requiredVisualResourcesRaw.Count)
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Duplicate resources specified");
            }
        }

        if (new HashSet<SceneResource>(RequiredScenes).Count != RequiredScenes.Count)
            throw new InvalidRegistryDataException(name, GetType().Name, "Duplicate scenes specified");
#endif
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

        if (InheritFrom != MainGameState.Invalid)
        {
            var resources = simulationParameters.GetStageResources(InheritFrom);
            RequiredVisualResources.AddRange(resources.RequiredVisualResources);
            RequiredScenes.AddRange(resources.RequiredScenes);

            // TODO: textures
        }
    }

    public void ApplyTranslations()
    {
    }
}
