namespace Components;

using Newtonsoft.Json;

/// <summary>
///   Specifies an exact scene path to load <see cref="SpatialInstance"/> from. Using
///   <see cref="PredefinedVisuals"/> should be preferred for all cases where that is usable for the situation.
/// </summary>
[JSONDynamicTypeAllowed]
public struct PathLoadedSceneVisuals
{
    /// <summary>
    ///   The scene to display. Setting this to null stops displaying the current scene
    /// </summary>
    public string? ScenePath;

    /// <summary>
    ///   Internal variable for the loading system, do not touch
    /// </summary>
    [JsonIgnore]
    public string? LastLoadedScene;

    /// <summary>
    ///   If true then the loaded scene is directly attached to a <see cref="SpatialInstance"/>. When this is done
    ///   the scene's root scale or transform does not work. So only scenes that work fine with this should set
    ///   this to true.
    /// </summary>
    public bool AttachDirectlyToScene;
}
