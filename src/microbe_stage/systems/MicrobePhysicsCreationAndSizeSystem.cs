namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
    /// <remarks>
    ///   <para>
    ///     This doesn't run on attached entities to avoid creating physics bodies for engulfed cells or ones in a
    ///     colony (only colony leader has a physics shape)
    ///   </para>
    /// </remarks>
    [With(typeof(CellProperties))]
    [With(typeof(MicrobePhysicsExtraData))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(PhysicsShapeHolder))]
    [Without(typeof(AttachedToEntity))]
    [WritesToComponent(typeof(MicrobeColony))]
    [WritesToComponent(typeof(CompoundAbsorber))]
    [ReadsComponent(typeof(OrganelleContainer))]
    [ReadsComponent(typeof(AttachedToEntity))]
    [RunsAfter(typeof(MicrobeVisualsSystem))]
    [RunsBefore(typeof(PhysicsBodyCreationSystem))]
    [RunsBefore(typeof(MicrobeReproductionSystem))]
    [RunsBefore(typeof(MulticellularGrowthSystem))]
    public sealed class MicrobePhysicsCreationAndSizeSystem : AEntitySetSystem<float>
    {
        private readonly float pilusDensity;

        private readonly ThreadLocal<List<(PhysicsShape Shape, Vector3 Position, Quaternion Rotation)>>
            temporaryCombinedShapeData = new(() => new List<(PhysicsShape Shape, Vector3 Position, Quaternion Rotation)>());

        private readonly ThreadLocal<List<(Membrane Membrane, bool Bacteria)>> temporaryColonyMemberMembranes =
            new(() => new List<(Membrane Membrane, bool Bacteria)>());

        private readonly
            ThreadLocal<List<(OrganelleLayout<PlacedOrganelle> Organelles, Vector3 ExtraOffset, Quaternion ExtraRotation)>>
            temporaryColonyMemberOrganelles =
                new(() =>
                    new List<(OrganelleLayout<PlacedOrganelle> Organelles, Vector3 ExtraOffset, Quaternion ExtraRotation)>());

        private readonly Lazy<PhysicsShape> eukaryoticPilus;

        /// <summary>
        ///   Scaled down pilus size for bacteria
        /// </summary>
        private readonly Lazy<PhysicsShape> prokaryoticPilus;

        public MicrobePhysicsCreationAndSizeSystem(World world, IParallelRunner parallelRunner) : base(world,
            parallelRunner)
        {
            pilusDensity = SimulationParameters.Instance.GetOrganelleType("pilus").Density;

            eukaryoticPilus = new Lazy<PhysicsShape>(() => CreatePilusShape(Constants.PILUS_PHYSICS_SIZE));
            prokaryoticPilus = new Lazy<PhysicsShape>(() => CreatePilusShape(Constants.PILUS_PHYSICS_SIZE * 0.5f));
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var cellProperties = ref entity.Get<CellProperties>();

            if (cellProperties.ShapeCreated)
                return;

            ref var shapeHolder = ref entity.Get<PhysicsShapeHolder>();

            // We don't skip creating a shape if there is already one as microbes can change shape, so we re-apply
            // the shape if there is a previous one

            // Create a shape for an entity missing it

            var membrane = cellProperties.CreatedMembrane;

            // Wait until membrane is created (and no longer being updated)
            if (!cellProperties.IsMembraneReady())
                return;

            ref var extraData = ref entity.Get<MicrobePhysicsExtraData>();

            List<(Membrane Membrane, bool Bacteria)>? colonyMembranes = null;

            // This catch is here in the very unlikely case that the membrane would throw an exception (due to being
            // disposed if a microbe was deleted before it got a physics body initialized for it)
            try
            {
                var rawData = membrane!.MembraneData.Vertices2D;
                var count = membrane.MembraneData.VertexCount;

                if (count < 1)
                {
                    GD.PrintErr("Generated membrane data has no vertices, can't create collision shape");
                    return;
                }

                UpdateNonPhysicsSizeData(entity, membrane.EncompassingCircleRadius, ref cellProperties);

                ref var organelles = ref entity.Get<OrganelleContainer>();

                if (organelles.Organelles == null)
                {
                    throw new InvalidOperationException(
                        "Organelles need to be initialized before membrane is generated for shape creation");
                }

                // TODO: shape creation could be postponed for colony members until they are detached (right now
                // their bodies won't get created as they are disabled, so make sure that works and then remove this
                // TODO comment)

                // If there are no pili or colony members then a single shape is enough for this microbe
                bool requiresCompoundShape = false;

                if (entity.Has<MicrobeColony>())
                {
                    // Skip creating shape if some colony member isn't ready yet
                    colonyMembranes = temporaryColonyMemberMembranes.Value;
                    ref var colony = ref entity.Get<MicrobeColony>();

                    foreach (var member in colony.ColonyMembers)
                    {
                        if (member == entity)
                            continue;

                        ref var memberProperties = ref member.Get<CellProperties>();

                        if (!memberProperties.IsMembraneReady())
                        {
                            // Need to wait for this member to generate its membrane
                            colonyMembranes.Clear();
                            return;
                        }

                        // Update colony member compound absorbing sizes in case they aren't up to date to make
                        // microbe colony compound absorbing work properly
                        UpdateNonPhysicsSizeData(member, memberProperties.CreatedMembrane!.EncompassingCircleRadius,
                            ref memberProperties);

                        colonyMembranes.Add((memberProperties.CreatedMembrane!, memberProperties.IsBacteria));
                    }

                    requiresCompoundShape = true;
                }
                else if (organelles.Organelles.Any(o => o.Definition.HasPilusComponent))
                {
                    requiresCompoundShape = true;
                }

                extraData.MicrobeShapesCount = 0;
                extraData.TotalShapeCount = 0;
                extraData.PilusCount = 0;

                // TODO: background thread shape creation to not take up main thread time (or maybe at least the
                // density calculation?)

                var oldShape = shapeHolder.Shape;

                if (!requiresCompoundShape)
                {
                    shapeHolder.Shape = CreateSimpleMicrobeShape(ref extraData, ref organelles, ref cellProperties,
                        rawData, count);
                }
                else
                {
                    // TODO: caching of compound shapes to make the old shape matching detection work
                    shapeHolder.Shape = CreateCompoundMicrobeShape(ref extraData, ref organelles, ref cellProperties,
                        entity, rawData, count, colonyMembranes);

                    if (colonyMembranes != null)
                    {
                        // Update full colony rotation properties. Note that this is here for historical reasons as
                        // this used to use the shape of the entire colony here, which is only available easily here.
                        ref var colony = ref entity.Get<MicrobeColony>();
                        colony.CalculateRotationSpeed();
                    }
                }

                // Skip updating the physics body shape if we got the same cached shape as we had before
                if (!ReferenceEquals(oldShape, shapeHolder.Shape))
                {
                    // Ensure physics body is recreated if the shape changed
                    shapeHolder.UpdateBodyShapeIfCreated = true;
                }

                cellProperties.ShapeCreated = true;
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to create physics body for a microbe: " + e);
            }
            finally
            {
                colonyMembranes?.Clear();
            }
        }

        private PhysicsShape CreateSimpleMicrobeShape(ref MicrobePhysicsExtraData extraData,
            ref OrganelleContainer organelles, ref CellProperties cellProperties,
            Vector2[] membraneVertices, int vertexCount)
        {
            UpdateRotationRate(ref organelles);

            var shape = PhysicsShape.GetOrCreateMicrobeShape(membraneVertices, vertexCount,
                MicrobeInternalCalculations.CalculateAverageDensity(organelles.Organelles!),
                cellProperties.IsBacteria);

            ++extraData.MicrobeShapesCount;
            ++extraData.TotalShapeCount;

            // Simple shape can't have pili in it

            return shape;
        }

        private PhysicsShape CreateColonyMemberBaseShape(ref MicrobePhysicsExtraData extraData,
            ref OrganelleContainer organelles, Membrane membrane, bool isBacteria)
        {
            var data = membrane.MembraneData;
            var shape = PhysicsShape.GetOrCreateMicrobeShape(data.Vertices2D, data.VertexCount,
                MicrobeInternalCalculations.CalculateAverageDensity(organelles.Organelles!),
                isBacteria);

            // Rotation rate doesn't need to be updated as the microbe, if ever ejected, will create its own shape
            // Colony rotation calculation uses the organelles directly to calculate the rotation.

            ++extraData.MicrobeShapesCount;
            ++extraData.TotalShapeCount;

            return shape;
        }

        private PhysicsShape CreateCompoundMicrobeShape(ref MicrobePhysicsExtraData extraData,
            ref OrganelleContainer organelles, ref CellProperties cellProperties, in Entity entity,
            Vector2[] membraneVertices, int vertexCount, List<(Membrane Membrane, bool Bacteria)>? colonyMembranes)
        {
            UpdateRotationRate(ref organelles);

            var combinedData = temporaryCombinedShapeData.Value;

#if DEBUG
            if (combinedData.Count > 0)
                throw new Exception("Combined shape data list was not properly cleared on last use");
#endif

            // Base microbe shape is always first
            combinedData.Add((
                CreateSimpleMicrobeShape(ref extraData, ref organelles, ref cellProperties, membraneVertices,
                    vertexCount), Vector3.Zero, Quaternion.Identity));

            List<(OrganelleLayout<PlacedOrganelle> Organelles, Vector3 ExtraOffset, Quaternion ExtraRotation)>?
                memberOrganelles = null;

            // Then the (potential) colony members
            if (colonyMembranes != null)
            {
                ref var colony = ref entity.Get<MicrobeColony>();

                var members = colony.ColonyMembers;
                int memberCount = members.Length;

                memberOrganelles = temporaryColonyMemberOrganelles.Value;

                // The bodies need to be added colony member list order
                for (int i = 0; i < memberCount; ++i)
                {
                    var member = members[i];

                    if (member == entity)
                    {
#if DEBUG
                        if (i != 0)
                            GD.PrintErr("Colony leader not in first position as is assumed");
#endif
                        continue;
                    }

                    // The -1 is here as the membrane list excludes the lead cell (which is in the first position in
                    // members)
                    var (membrane, isBacteria) = colonyMembranes[i - 1];

                    ref var currentMemberOrganelles = ref member.Get<OrganelleContainer>();

                    ref var attached = ref member.Get<AttachedToEntity>();

                    memberOrganelles.Add((currentMemberOrganelles.Organelles ??
                        throw new Exception("Colony member has no organelles but it had a membrane"),
                        attached.RelativePosition, attached.RelativeRotation));

                    combinedData.Add((
                        CreateColonyMemberBaseShape(ref extraData, ref currentMemberOrganelles, membrane, isBacteria),
                        attached.RelativePosition, attached.RelativeRotation));
                }
            }

            // Pili are after the microbe shapes, otherwise pilus collision detection can't be done as we just
            // compare the sub-shape index to the number of microbe collisions to determine if something is a pilus
            // And to detect between the pilus variants, first normal pili are created and only then injectisomes
            bool hasInjectisomes = false;

            foreach (var organelle in organelles.Organelles!)
            {
                if (organelle.Definition.HasPilusComponent)
                {
                    if (organelle.Upgrades.HasInjectisomeUpgrade())
                    {
                        hasInjectisomes = true;
                        continue;
                    }

                    combinedData.Add(CreatePilusShape(ref extraData, ref cellProperties, organelle, Vector3.Zero,
                        Quaternion.Identity));
                }
            }

            // Colony member pili
            if (memberOrganelles != null)
            {
                foreach (var entry in memberOrganelles)
                {
                    foreach (var organelle in entry.Organelles)
                    {
                        if (organelle.Definition.HasPilusComponent)
                        {
                            if (organelle.Upgrades.HasInjectisomeUpgrade())
                            {
                                hasInjectisomes = true;
                                continue;
                            }

                            combinedData.Add(CreatePilusShape(ref extraData, ref cellProperties, organelle,
                                entry.ExtraOffset, entry.ExtraRotation));
                        }
                    }
                }
            }

            if (hasInjectisomes)
            {
                foreach (var organelle in organelles.Organelles)
                {
                    if (organelle.Definition.HasPilusComponent && organelle.Upgrades.HasInjectisomeUpgrade())
                    {
                        combinedData.Add(CreatePilusShape(ref extraData, ref cellProperties, organelle, Vector3.Zero,
                            Quaternion.Identity));
                        ++extraData.PilusInjectisomeCount;
                    }
                }

                // Colony member injectisomes
                if (memberOrganelles != null)
                {
                    foreach (var entry in memberOrganelles)
                    {
                        foreach (var organelle in entry.Organelles)
                        {
                            if (organelle.Definition.HasPilusComponent && organelle.Upgrades.HasInjectisomeUpgrade())
                            {
                                combinedData.Add(CreatePilusShape(ref extraData, ref cellProperties, organelle,
                                    entry.ExtraOffset, entry.ExtraRotation));
                                ++extraData.PilusInjectisomeCount;
                            }
                        }
                    }
                }
            }

            if (extraData.TotalShapeCount != combinedData.Count)
            {
                combinedData.Clear();
                throw new Exception("Incorrect total shape count result in microbe physics creation");
            }

            // Create the final shape
            // This uses a static combined shape as the shapes are fully re-created each time
            // TODO: investigate if modifiable combined shape would be a better fit for the game
            var combinedShape = PhysicsShape.CreateCombinedShapeStatic(combinedData);

            combinedData.Clear();

            return combinedShape;
        }

        private (PhysicsShape Shape, Vector3 Position, Quaternion Rotation) CreatePilusShape(
            ref MicrobePhysicsExtraData extraData, ref CellProperties cellProperties,
            PlacedOrganelle placedOrganelle, Vector3 extraOffset, Quaternion extraRotation)
        {
            var externalPosition = cellProperties.CalculateExternalOrganellePosition(placedOrganelle.Position,
                placedOrganelle.Orientation, out var rotation);

            var (position, orientation) =
                placedOrganelle.CalculatePhysicsExternalTransform(externalPosition, rotation,
                    cellProperties.IsBacteria);

            ++extraData.PilusCount;
            ++extraData.TotalShapeCount;

            return (cellProperties.IsBacteria ? prokaryoticPilus.Value : eukaryoticPilus.Value,
                extraRotation * position + extraOffset, extraRotation * orientation);
        }

        /// <summary>
        ///   Updates the microbe movement's used rotation rate.
        ///   Note that the PhysicsShape is not currently used in rotation calculations, and this code is here due to
        ///   earlier version requiring it.
        /// </summary>
        private void UpdateRotationRate(ref OrganelleContainer organelleContainer)
        {
            if (organelleContainer.Organelles == null)
            {
                throw new InvalidOperationException(
                    "Can't calculate rotation rate for organelle container with no organelles");
            }

            organelleContainer.RotationSpeed =
                MicrobeInternalCalculations.CalculateRotationSpeed(organelleContainer.Organelles.Organelles);
        }

        private PhysicsShape CreatePilusShape(float size)
        {
            var radius = size / 9.0f;

            // Jolt physics also doesn't support cones so, cylinder is now the permanent pilus shape
            return PhysicsShape.CreateCylinder(size * 0.5f, radius, pilusDensity);
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

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                temporaryCombinedShapeData.Dispose();
                temporaryColonyMemberMembranes.Dispose();
                temporaryColonyMemberOrganelles.Dispose();
            }
        }
    }
}
