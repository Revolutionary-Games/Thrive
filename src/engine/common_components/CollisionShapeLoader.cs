namespace Components
{
    using Newtonsoft.Json;

    /// <summary>
    ///   Specifies a collision shape resource to be loaded into a <see cref="PhysicsShapeHolder"/>
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct CollisionShapeLoader
    {
        public string CollisionResourcePath;

        /// <summary>
        ///   Density of the shape. Only applies if <see cref="ApplyDensity"/> is true.
        /// </summary>
        public float Density;

        /// <summary>
        ///   If false a default density (if known for the collision resource) is used
        /// </summary>
        public bool ApplyDensity;

        /// <summary>
        ///   If this is set to true then when this shape is created it doesn't force a <see cref="Physics"/> to
        ///   recreate the body for the changed shape (if the body was already created). When false it is ensured that
        ///   the body gets recreated when the shape changes.
        /// </summary>
        public bool SkipForceRecreateBodyIfCreated;

        /// <summary>
        ///   Must be set to false if parameters are changed for the shape to be reloaded
        /// </summary>
        [JsonIgnore]
        public bool ShapeLoaded;

        public CollisionShapeLoader(string resourcePath, float density)
        {
            CollisionResourcePath = resourcePath;
            Density = density;
            ApplyDensity = true;

            SkipForceRecreateBodyIfCreated = false;
            ShapeLoaded = false;
        }
    }
}
