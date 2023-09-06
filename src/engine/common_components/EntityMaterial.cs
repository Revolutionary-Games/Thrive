namespace Components
{
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Access to a material defined on an entity
    /// </summary>
    public struct EntityMaterial
    {
        [JsonIgnore]
        public ShaderMaterial[]? Materials;

        /// <summary>
        ///   If not null then <see cref="AutoRetrieveFromSpatial"/> uses this as the relative path from the
        ///   <see cref="Spatial"/> node to where the material is retrieved from
        /// </summary>
        public string? AutoRetrieveModelPath;

        /// <summary>
        ///   When true and this entity has a <see cref="SpatialInstance"/> component the material is automatically
        ///   fetched
        /// </summary>
        public bool AutoRetrieveFromSpatial;

        /// <summary>
        ///   Internal flag, don't modify
        /// </summary>
        [JsonIgnore]
        public bool MaterialFetchPerformed;
    }
}
