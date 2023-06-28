namespace Components
{
    using Godot;

    /// <summary>
    ///   Displays a single <see cref="Spatial"/> as this entity's graphics in Godot
    /// </summary>
    public struct SpatialInstance
    {
        public Spatial? GraphicalInstance;

        /// <summary>
        ///   If not null applies visual scale to <see cref="GraphicalInstance"/>
        /// </summary>
        public Vector3? VisualScale;
    }
}
