namespace Components
{
    using System;

    /// <summary>
    ///   Allows modifying <see cref="Physics"/> collisions of this entity
    /// </summary>
    public struct CollisionManagement
    {
        // TODO: some kind of physics callback registration

        public bool AllCollisionsDisabled;

        public bool Dirty;

        public delegate void OnCollidedWith(PhysicsBody body, int collidedSubShapeDataOurs,
            int collidedSubShapeDataTheirs);
    }

    public static class CollisionManagementExtensions
    {
        public static void RegisterCollisionCallback(this CollisionManagement entity,
            CollisionManagement.OnCollidedWith onCollided)
        {
            throw new NotImplementedException();
        }
    }
}
