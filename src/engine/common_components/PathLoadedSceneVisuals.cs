namespace Components
{
    using Newtonsoft.Json;

    /// <summary>
    ///   Specifies an exact scene path to load <see cref="SpatialInstance"/> from. Using
    ///   <see cref="PredefinedVisuals"/> should be preferred for all cases where that is usable for the situation.
    /// </summary>
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
    }
}
