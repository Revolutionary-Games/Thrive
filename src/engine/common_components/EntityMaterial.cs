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
        public ShaderMaterial? Material;

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
