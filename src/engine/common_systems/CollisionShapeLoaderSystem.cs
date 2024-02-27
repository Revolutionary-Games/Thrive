namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;

    /// <summary>
    ///   Loads predefined collision shapes from resources
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Runs on main thread for now due to needing to load Godot resources for physics. Maybe in the future we
    ///     have a pre-converted format for the game that doesn't need this. Also multithreading disabled for now.
    ///   </para>
    /// </remarks>
    [With(typeof(CollisionShapeLoader))]
    [With(typeof(PhysicsShapeHolder))]
    [RuntimeCost(0.5f)]
    [RunsOnMainThread]
    public sealed class CollisionShapeLoaderSystem : AEntitySetSystem<float>
    {
        public CollisionShapeLoaderSystem(World world) : base(world, null)
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
