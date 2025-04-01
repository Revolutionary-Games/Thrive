using System;
using Godot;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   Info on loading a visual resource (with potentially different quality levels for graphics settings)
/// </summary>
public class VisualResourceData : IRegistryType, IResource
{
    [JsonIgnore]
    public VisualResourceIdentifier VisualIdentifier { get; private set; }

    [JsonProperty]
    public string NormalQualityPath { get; private set; } = string.Empty;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    /// <summary>
    ///   If this is <see cref="Loaded"/> provides access to the packed scene of this visual resource
    /// </summary>
    [JsonIgnore]
    public PackedScene? LoadedNormalQuality { get; private set; }

    [JsonProperty]
    public bool RequiresSyncLoad { get; private set; }

    [JsonIgnore]
    public bool UsesPostProcessing => false;

    [JsonIgnore]
    public bool RequiresSyncPostProcess => false;

    [JsonProperty]
    public float EstimatedTimeRequired { get; set; } = 0.05f;

    [JsonIgnore]
    public bool LoadingPrepared { get; set; }

    [JsonIgnore]
    public bool Loaded { get; set; }

    [JsonIgnore]
    public string Identifier => InternalName;

    [JsonIgnore]
    public Action<IResource>? OnComplete { get; set; }

    public void PrepareLoading()
    {
        // TODO: pre-loading data from disk?
    }

    public void Load()
    {
        LoadedNormalQuality = GD.Load<PackedScene>(NormalQualityPath);
        Loaded = true;
    }

    public void PerformPostProcessing()
    {
    }

    public void UnLoad()
    {
        // Packed scenes are shared objects that can't be Disposed safely
        LoadedNormalQuality = null;
        Loaded = false;
    }

    public void Check(string name)
    {
        if (string.IsNullOrWhiteSpace(NormalQualityPath))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing normal quality scene path");

        // TODO: for safety should these objects verify the scene paths? That would mean checking a ton of scenes if
        // a bunch of game visuals will go through this system

        // Parse the identifier from the internal name
        if (!Enum.TryParse(InternalName, out VisualResourceIdentifier identifier))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Failed to parse internal name as identifier");
        }

        VisualIdentifier = identifier;

        if (VisualIdentifier == VisualResourceIdentifier.None)
            throw new InvalidRegistryDataException(name, GetType().Name, "Resource identifier type is none");

        // Don't load any of the scenes here as otherwise all of the game visuals would always be forced to be loaded
        // in memory.
        // Instead, individual game states should manage scenes that must be loaded while they might instance them
        // See StageResourcesList
    }

    public void ApplyTranslations()
    {
    }
}
