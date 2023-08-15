namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles creating microbe physics
    /// </summary>
    [With(typeof(CellProperties))]
    [With(typeof(PhysicsShapeHolder))]
    public sealed class MicrobePhysicsCreationSystem : AEntitySetSystem<float>
    {
        private JVecF3[] temporaryBuffer = new JVecF3[20];

        public MicrobePhysicsCreationSystem(World world) : base(world, null)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var shapeHolder = ref entity.Get<PhysicsShapeHolder>();

            if (shapeHolder.Shape != null)
                return;

            // Create a shape for an entity missing it

            ref var cellProperties = ref entity.Get<CellProperties>();

            var membrane = cellProperties.CreatedMembrane;

            // Wait until membrane is created
            if (membrane == null)
                return;

            // This catch is here in the very unlikely case that the membrane would throw an exception (due to being
            // disposed if a microbe was deleted before it got a physics body initialized for it)
            try
            {
                // And wait until the membrane is fully initialized
                if (membrane.Dirty)
                    return;

                var rawData = membrane.Computed2DVertices;

                if (rawData.Count < 1)
                    return;

                // TODO: caching for the shape based on a hash of the vertices2D points

                // TODO: background thread shape creation to not take up main thread time

                // TODO: find out if a more performant way can be done to copy this data
                if (temporaryBuffer.Length < rawData.Count)
                    temporaryBuffer = new JVecF3[rawData.Count];

                var buffer = temporaryBuffer;

                for (int i = 0; i < rawData.Count; ++i)
                {
                    buffer[i] = new JVecF3(rawData[i].x, 0, rawData[i].y);
                }

                // TODO: overall density
                shapeHolder.Shape = PhysicsShape.CreateMicrobeShape(new ReadOnlySpan<JVecF3>(buffer, 0, rawData.Count),
                    1000, cellProperties.IsBacteria);
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to create physics body for a microbe: " + e);
            }
        }
    }
}
