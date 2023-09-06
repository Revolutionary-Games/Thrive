namespace Components
{
    using Newtonsoft.Json;

    /// <summary>
    ///   Holds a physics shape once one is ready and then allows creating a physics body from it
    /// </summary>
    public struct PhysicsShapeHolder
    {
        [JsonIgnore]
        public PhysicsShape? Shape;

        /// <summary>
        ///   When true the body is created as a static body that cannot move
        /// </summary>
        public bool BodyIsStatic;

        /// <summary>
        ///   When true the related physics body will be updated from <see cref="Shape"/> when the shape is ready.
        ///   Will be automatically reset to false afterwards.
        /// </summary>
        public bool UpdateBodyShapeIfCreated;
    }

    public static class PhysicsShapeHolderExtensions
    {
        /// <summary>
        ///   Gets the mass of a shape holder's shape if exist (if doesn't exist sets mass to 1)
        /// </summary>
        /// <param name="shapeHolder">Shape holder to look at</param>
        /// <param name="mass">The found shape mass or 1000 if not found</param>
        /// <returns>True if mass was retrieved</returns>
        public static bool TryGetShapeMass(this ref PhysicsShapeHolder shapeHolder, out float mass)
        {
            if (shapeHolder.Shape == null)
            {
                mass = 1000;
                return false;
            }

            mass = shapeHolder.Shape.GetMass();
            return true;
        }
    }
}
