namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles creating microbe physics and handling a few house keeping tasks based on the final cell size data
    ///   from the membrane
    /// </summary>
    [With(typeof(CellProperties))]
    [With(typeof(MicrobePhysicsExtraData))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(PhysicsShapeHolder))]
    [RunsAfter(typeof(MicrobeVisualsSystem))]
    [WritesToComponent(typeof(CompoundAbsorber))]
    public sealed class MicrobePhysicsCreationAndSizeSystem : AEntitySetSystem<float>
    {
        private JVecF3[] temporaryBuffer = new JVecF3[50];

        public MicrobePhysicsCreationAndSizeSystem(World world, IParallelRunner parallelRunner) : base(world,
            parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var cellProperties = ref entity.Get<CellProperties>();

            if (cellProperties.ShapeCreated)
                return;

            ref var shapeHolder = ref entity.Get<PhysicsShapeHolder>();

            if (shapeHolder.Shape != null)
                return;

            // Create a shape for an entity missing it

            var membrane = cellProperties.CreatedMembrane;

            // Wait until membrane is created
            if (!cellProperties.IsMembraneReady())
                return;

            ref var extraData = ref entity.Get<MicrobePhysicsExtraData>();

            // This catch is here in the very unlikely case that the membrane would throw an exception (due to being
            // disposed if a microbe was deleted before it got a physics body initialized for it)
            try
            {
                var rawData = membrane!.Computed2DVertices;

                if (rawData.Count < 1)
                    return;

                UpdateNonPhysicsSizeData(entity, membrane.EncompassingCircleRadius, ref cellProperties);

                // TODO: shape creation could be postponed for colony members until they are detached (right now
                // their bodies won't get created as they are disabled)

                extraData.MicrobeShapesCount = 0;
                extraData.TotalShapeCount = 0;
                extraData.PilusCount = 0;

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
                ++extraData.MicrobeShapesCount;
                ++extraData.TotalShapeCount;

                ref var organelles = ref entity.Get<OrganelleContainer>();

                if (organelles.Organelles == null)
                {
                    throw new InvalidOperationException(
                        "Organelles need to be initialized before membrane is generated for shape creation");
                }

                if (entity.Has<MicrobeColony>())
                {
                    // TODO: cell colony physics (and colony member pili), the bodies need to be added colony member
                    // list order

                    throw new NotImplementedException();
                }

                // Pili are after the microbe shapes, otherwise pilus collision detection can't be done as we just
                // compare the sub-shape index to the number of microbe collisions to determine if something is a pilus
                // And to detect between the pilus variants, first normal pili are created and only then injectisomes
                bool hasInjectisomes = false;

                foreach (var organelle in organelles.Organelles)
                {
                    if (organelle.Definition.HasPilusComponent)
                    {
                        if (organelle.Upgrades.HasInjectisomeUpgrade())
                        {
                            hasInjectisomes = true;
                            continue;
                        }

                        CreatePilusShape(ref extraData);
                    }
                }

                if (hasInjectisomes)
                {
                    foreach (var organelle in organelles.Organelles)
                    {
                        if (organelle.Definition.HasPilusComponent && organelle.Upgrades.HasInjectisomeUpgrade())
                        {
                            CreatePilusShape(ref extraData);
                            ++extraData.PilusInjectisomeCount;
                        }
                    }
                }

                // Ensure physics body is recreated if the shape changed
                shapeHolder.UpdateBodyShapeIfCreated = true;
                cellProperties.ShapeCreated = true;
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to create physics body for a microbe: " + e);
            }
        }

        private void CreatePilusShape(ref MicrobePhysicsExtraData extraData)
        {
            throw new NotImplementedException();

            // TODO: does this still need:
            // var rotation = MathUtils.CreateRotationForPhysicsOrganelle(angle);

            // TODO: does this need some special positioning like in the old version:
            // membraneCoords += membranePointDirection * Constants.DEFAULT_HEX_SIZE * 2;

            // if (organelle.ParentMicrobe!.CellTypeProperties.IsBacteria)
            // {
            //     membraneCoords *= 0.5f;
            // }
            //
            // var physicsRotation = MathUtils.CreateRotationForPhysicsOrganelle(angle);

            // TODO: cache two variants of the pilus shape: one for bacteria and one for eukaryotes
            /*float pilusSize = Constants.PILUS_PHYSICS_SIZE;

            // Scale the size down for bacteria
            if (organelle!.ParentMicrobe!.CellTypeProperties.IsBacteria)
            {
                pilusSize *= 0.5f;
            }

            // Turns out cones are really hated by physics engines, so we'll need to permanently make do with a cylinder
            var shape = new CylinderShape();
            shape.Radius = pilusSize / 10.0f;
            shape.Height = pilusSize;*/

            ++extraData.PilusCount;
            ++extraData.TotalShapeCount;
        }

        private void UpdateNonPhysicsSizeData(in Entity entity, float membraneRadius, ref CellProperties cellProperties)
        {
            cellProperties.UnadjustedRadius = membraneRadius;

            if (entity.Has<CompoundAbsorber>())
            {
                // Max here buffs compound absorbing for the smallest cells
                entity.Get<CompoundAbsorber>().AbsorbRadius =
                    Math.Max(cellProperties.Radius, Constants.MICROBE_MIN_ABSORB_RADIUS);
            }
        }
    }
}
