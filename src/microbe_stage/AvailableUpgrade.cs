﻿using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   General data about an available upgrade that is either present or not (some more in-depth upgrades have their
///   own data classes to store all of their data)
/// </summary>
public class AvailableUpgrade : IRegistryType
{
    private LoadedSceneWithModelInfo loadedSceneData;

#pragma warning disable 169,649 // Used through reflection
    /// <summary>
    ///   A path to a scene to override organelle's display scene. If empty will use organelle's default model.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that this overrides any cell corpse chunk scene set for the organelle type (if this is not empty)
    ///   </para>
    /// </remarks>
    [JsonProperty]
    private SceneWithModelInfo overrideGraphics;

    [JsonProperty(nameof(OverrideProcesses))]
    private Dictionary<string, float>? overrideProcesses;

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
    ///   If not null this list of processes overrides the defaults for the organelle
    /// </summary>
    [JsonIgnore]
    public List<TweakedProcess>? OverrideProcesses { get; private set; }

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

        if (overrideProcesses != null)
        {
            foreach (var process in overrideProcesses)
            {
                if (process.Value <= 0)
                    throw new InvalidRegistryDataException(name, GetType().Name, "Process speed should be positive");
            }
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void Resolve(SimulationParameters parameters)
    {
        if (!string.IsNullOrEmpty(IconPath))
            LoadedIcon = GD.Load<Texture2D>(IconPath);

        // Preload the scene for instantiating in microbes
        // TODO: switch this to only load when loading the microbe stage to not load this in the future when we have
        // playable stages that don't need these graphics
        if (!string.IsNullOrEmpty(overrideGraphics.ScenePath))
        {
            loadedSceneData.LoadFrom(overrideGraphics);
        }

        if (overrideProcesses != null)
        {
            OverrideProcesses = new List<TweakedProcess>();

            foreach (var process in overrideProcesses)
            {
                OverrideProcesses.Add(new TweakedProcess(parameters.GetBioProcess(process.Key), process.Value));
            }

            if (OverrideProcesses.Count < 1)
            {
                throw new InvalidRegistryDataException(InternalName, nameof(AvailableUpgrade),
                    "Override process list cannot be empty");
            }
        }
    }

    public bool TryGetGraphicsScene(out LoadedSceneWithModelInfo model)
    {
        if (loadedSceneData.LoadedScene == null!)
        {
            model = default(LoadedSceneWithModelInfo);

            return false;
        }

        model = loadedSceneData;
        return true;
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
