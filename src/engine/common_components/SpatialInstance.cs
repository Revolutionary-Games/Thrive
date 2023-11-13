namespace Components
{
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Displays a single <see cref="Spatial"/> as this entity's graphics in Godot
    /// </summary>
    public struct SpatialInstance
    {
        [JsonIgnore]
        public Spatial? GraphicalInstance;

        /// <summary>
        ///   Visual scale to set. Only applies when <see cref="ApplyVisualScale"/> is set to true to only require
        ///   entities that want to scale to set this field
        /// </summary>
        public Vector3 VisualScale;

        /// <summary>
        ///   If true applies visual scale to <see cref="GraphicalInstance"/>
        /// </summary>
        public bool ApplyVisualScale;
    }
}
