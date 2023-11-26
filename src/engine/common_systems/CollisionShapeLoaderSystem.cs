namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Loads predefined collision shapes from resources
    /// </summary>
    [With(typeof(CollisionShapeLoader))]
    [With(typeof(PhysicsShapeHolder))]
    public sealed class CollisionShapeLoaderSystem : AEntitySetSystem<float>
    {
        public CollisionShapeLoaderSystem(World world, IParallelRunner runner) :
            base(world, runner, Constants.SYSTEM_HIGH_ENTITIES_PER_THREAD)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var loader = ref entity.Get<CollisionShapeLoader>();

            if (loader.ShapeLoaded)
                return;

            ref var shapeHolder = ref entity.Get<PhysicsShapeHolder>();

            float density;
            if (loader.ApplyDensity)
            {
                density = loader.Density;
            }
            else
            {
                // TODO: per resource object defaults (if those are possible to add)
                density = 1000;
            }

            // TODO: switch to pre-processing collision shapes before the game is exported for faster runtime loading
            shapeHolder.Shape = PhysicsShape.CreateShapeFromGodotResource(loader.CollisionResourcePath, density);

            if (!loader.SkipForceRecreateBodyIfCreated)
                shapeHolder.UpdateBodyShapeIfCreated = true;

            loader.ShapeLoaded = true;
        }
    }
}
