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

    [JsonIgnore]
    public List<SceneResource> RequiredScenes = new();

    // TODO: if textures are implemented they need to also share instances so that preloading works correctly

    /// <summary>
    ///   If set, then grabs all resources from the given stage
    /// </summary>
    public MainGameState InheritFrom = MainGameState.Invalid;

    // Set through JSON
#pragma warning disable CS0649
    [JsonProperty("RequiredVisualResources")]
    private List<VisualResourceIdentifier>? requiredVisualResourcesRaw;

    [JsonProperty("RequiredScenes")]
    private List<string>? requiredScenesRaw;
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

        if ((requiredVisualResourcesRaw == null || requiredVisualResourcesRaw.Count < 1) &&
            (requiredScenesRaw == null || requiredScenesRaw.Count < 1))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "No resources specified");
        }

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

        if (requiredScenesRaw != null)
        {
            if (new HashSet<string>(requiredScenesRaw).Count != requiredScenesRaw.Count)
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Duplicate scenes specified");
            }
        }
#endif

        if (Stage == MainGameState.MicrobeStage && requiredScenesRaw is not { Count: > 1 })
        {
            // Catching a problem with JSON config not loading the scenes at all
            throw new InvalidRegistryDataException(name, GetType().Name, "Microbe stage should have scene resources");
        }
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

        if (requiredScenesRaw != null)
        {
            foreach (var scenePath in requiredScenesRaw)
            {
                RequiredScenes.Add(simulationParameters.GetSceneResource(scenePath));
            }
        }

        if (InheritFrom != MainGameState.Invalid)
        {
            var resources = simulationParameters.GetStageResources(InheritFrom);

            // Only add non-specified ones
#if DEBUG
            foreach (var resource in resources.RequiredVisualResources)
            {
                if (RequiredVisualResources.Contains(resource))
                    throw new Exception("Duplicate resource (from inheritance): " + resource.Identifier);

                RequiredVisualResources.Add(resource);
            }

            foreach (var scene in resources.RequiredScenes)
            {
                if (RequiredScenes.Contains(scene))
                    throw new Exception("Duplicate scene (from inheritance): " + scene.Identifier);

                RequiredScenes.Add(scene);
            }
#else
            RequiredVisualResources.AddRange(resources.RequiredVisualResources);
            RequiredScenes.AddRange(resources.RequiredScenes);
#endif

            // TODO: textures (need to again load through another list to ensure equal instances for preloading)
        }
    }

    public void ApplyTranslations()
    {
    }
}
