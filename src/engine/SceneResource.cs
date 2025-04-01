using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A pre-loadable scene
/// </summary>
public class SceneResource : IResource
{
    public SceneResource(string path)
    {
        Path = path;
    }

    [JsonProperty]
    public string Path { get; private set; }

    [JsonProperty]
    public bool RequiresSyncLoad { get; private set; }

    [JsonIgnore]
    public bool UsesPostProcessing => false;

    [JsonProperty]
    public bool RequiresSyncPostProcess { get; private set; }

    [JsonProperty]
    public float EstimatedTimeRequired { get; private set; } = 0.1f;

    [JsonIgnore]
    public bool LoadingPrepared { get; set; }

    [JsonIgnore]
    public bool Loaded { get; private set; }

    [JsonIgnore]
    public string Identifier => Path;

    [JsonIgnore]
    public Action<IResource>? OnComplete { get; set; }

    /// <summary>
    ///   The loaded scene if this is currently in loaded state
    /// </summary>
    [JsonIgnore]
    public PackedScene? LoadedScene { get; private set; }

    public void PrepareLoading()
    {
        // Could maybe pre-read the file from disk here to then speed up the real load with disk caching
    }

    public void Load()
    {
        LoadedScene = GD.Load<PackedScene>(Path);
        Loaded = true;
    }

    public void PerformPostProcessing()
    {
    }

    public void UnLoad()
    {
        // Packed scene instances are shared so they may not be disposed
        LoadedScene = null;
        Loaded = false;
    }
}
