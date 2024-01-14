namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Handles creating the simple shapes of <see cref="SimpleShapeType"/>
    /// </summary>
    [With(typeof(SimpleShapeCreator))]
    [With(typeof(PhysicsShapeHolder))]
    [RunsBefore(typeof(PhysicsBodyCreationSystem))]
    public sealed class SimpleShapeCreatorSystem : AEntitySetSystem<float>
    {
        public SimpleShapeCreatorSystem(World world, IParallelRunner runner) :
            base(world, runner, Constants.SYSTEM_HIGH_ENTITIES_PER_THREAD)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var shapeCreator = ref entity.Get<SimpleShapeCreator>();

            if (shapeCreator.ShapeCreated)
                return;

            ref var shapeHolder = ref entity.Get<PhysicsShapeHolder>();

            shapeHolder.Shape = CreateShape(ref shapeCreator);

            if (!shapeCreator.SkipForceRecreateBodyIfCreated)
                shapeHolder.UpdateBodyShapeIfCreated = true;

            shapeCreator.ShapeCreated = true;
        }

        private PhysicsShape CreateShape(ref SimpleShapeCreator creator)
        {
            var density = creator.Density;

            if (density <= 0)
                density = 1000;

            // TODO: add caching here for small shapes that get recreated a lot
            switch (creator.ShapeType)
            {
                case SimpleShapeType.Box:
                    return PhysicsShape.CreateBox(creator.Size, density);
                case SimpleShapeType.Sphere:
                    return PhysicsShape.CreateSphere(creator.Size, density);
                default:
                    throw new ArgumentOutOfRangeException(nameof(creator.ShapeType),
                        "Unknown simple shape type to create");
            }
        }
    }
}
