namespace Components
{
    using Newtonsoft.Json;

    /// <summary>
    ///   Holds a physics shape once one is ready and then allows creating a physics body from it
    /// </summary>
    public struct PhysicsShapeHolder
    {
        public PhysicsShape? Shape;

        /// <summary>
        ///   When true the body is created as a static body that cannot move
        /// </summary>
        public bool BodyIsStatic;

        /// <summary>
        ///   When true the related physics body will be recreated from <see cref="Shape"/> when the shape is ready.
        ///   Will be automatically reset to false afterwards.
        /// </summary>
        public bool RecreateBody;
    }
}
