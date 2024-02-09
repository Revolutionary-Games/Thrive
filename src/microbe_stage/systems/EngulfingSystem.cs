namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.Command;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles starting pulling in <see cref="Engulfable"/> to <see cref="Engulfer"/> entities and also expelling
    ///   things engulfers don't want to eat. Handles the endosome graphics as well.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     In an optimal ECS design this would be a much more general system, but due to being ported from the old
    ///     microbe code, this is heavily dependent on microbes being the engulfers. If this was done with a brand new
    ///     design this code wouldn't be this good to have so many assumptions about the types of engulfers.
    ///   </para>
    /// </remarks>
    [With(typeof(Engulfer))]
    [With(typeof(Health))]
    [With(typeof(CollisionManagement))]
    [With(typeof(MicrobePhysicsExtraData))]
    [With(typeof(MicrobeControl))]
    [With(typeof(CellProperties))]
    [With(typeof(CompoundStorage))]
    [With(typeof(SoundEffectPlayer))]
    [With(typeof(SpatialInstance))]
    [With(typeof(SpeciesMember))]
    [WritesToComponent(typeof(Engulfable))]
    [WritesToComponent(typeof(Physics))]
    [WritesToComponent(typeof(RenderPriorityOverride))]
    [WritesToComponent(typeof(CompoundAbsorber))]
    [WritesToComponent(typeof(UnneededCompoundVenter))]
    [WritesToComponent(typeof(SpatialInstance))]
    [WritesToComponent(typeof(AttachedToEntity))]
    [WritesToComponent(typeof(MicrobeColony))]
    [WritesToComponent(typeof(MicrobeAI))]
    [ReadsComponent(typeof(CollisionManagement))]
    [ReadsComponent(typeof(MicrobePhysicsExtraData))]
    [ReadsComponent(typeof(OrganelleContainer))]
    [ReadsComponent(typeof(MicrobeEventCallbacks))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsAfter(typeof(ColonyCompoundDistributionSystem))]
    [RunsAfter(typeof(PilusDamageSystem))]
    [RunsAfter(typeof(MicrobeVisualsSystem))]
    [RunsBefore(typeof(SpatialAttachSystem))]
    [RuntimeCost(11)]
    [RunsOnMainThread]
    public sealed class EngulfingSystem : AEntitySetSystem<float>
    {
        /// <summary>
        ///   Cache to re-use bulk transport animation objects
        /// </summary>
        private static readonly Queue<Engulfable.BulkTransportAnimation> UnusedTransportAnimations = new();

#pragma warning disable CA2213
        private readonly PackedScene endosomeScene;
#pragma warning restore CA2213

        private readonly IWorldSimulation worldSimulation;
        private readonly ISpawnSystem spawnSystem;

        private readonly Compound atp;

        private readonly Random random = new();

        /// <summary>
        ///   Holds <see cref="Endosome"/> graphics instances. The second level dictionary maps from the engulfed
        ///   entity to the endosome that is placed on it to visually show it being engulfed.
        /// </summary>
        private readonly Dictionary<Entity, Dictionary<Entity, Endosome>> entityEngulfingEndosomeGraphics = new();

        // Temporary variables to handle deleting unused endosome graphics without temporary lists
        private readonly List<Entity> usedTopLevelEngulfers = new();
        private readonly List<Entity> topLevelEngulfersToDelete = new();
        private readonly List<KeyValuePair<Entity, Entity>> usedEngulfedObjects = new();
        private readonly List<KeyValuePair<Entity, Entity>> engulfedObjectsToDelete = new();

        /// <summary>
        ///   Used to avoid a temporary list allocation
        /// </summary>
        private readonly List<Entity> tempEntitiesToEject = new();

        /// <summary>
        ///   Used to keep track of entities that just began to be engulfed. Transport animation and other operations
        ///   are skipped on these for one update to avoid a problem where the attached component is not created yet.
        /// </summary>
        private readonly HashSet<Entity> beginningEngulfedObjects = new();

        // TODO: caching for Endosome scenes (will need to report as intentionally orphaned nodes)

        /// <summary>
        ///   Temporary storage for some expelled object expire time calculations, used to avoid allocating an extra
        ///   list per update.
        /// </summary>
        private readonly List<KeyValuePair<Entity, float>> tempWorkSpaceForTimeReduction = new();

        private GameWorld? gameWorld;

        private bool endosomeDebugAlreadyPrinted;

        public EngulfingSystem(IWorldSimulation worldSimulation, ISpawnSystem spawnSystem, World world) :
            base(world, null)
        {
            this.worldSimulation = worldSimulation;
            this.spawnSystem = spawnSystem;
            endosomeScene = GD.Load<PackedScene>("res://src/microbe_stage/Endosome.tscn");

            atp = SimulationParameters.Instance.GetCompound("atp");
        }

        public static bool AddAlreadyEngulfedObject(ref Engulfer engulfer, in Entity engulferEntity,
            ref Engulfable engulfable, in Entity engulfableEntity)
        {
            if (!IngestEngulfableFromOtherEntity(ref engulfer, engulferEntity, ref engulfable, engulfableEntity))
            {
                GD.PrintErr("Failed to add already engulfed object to another engulfer");
                return false;
            }

            return true;
        }

        public void SetWorld(GameWorld world)
        {
            gameWorld = world;
        }

        /// <summary>
        ///   Eject all engulfables of a destroyed entity (if it is an engulfer). Or if the entity is an engulfable
        ///   force eject if from an engulfer if it is inside any.
        /// </summary>
        public void OnEntityDestroyed(in Entity entity)
        {
            if (entity.Has<Engulfable>())
            {
                ref var engulfable = ref entity.Get<Engulfable>();

                if (engulfable.HostileEngulfer.IsAlive && engulfable.HostileEngulfer.Has<Engulfer>())
                {
                    // Force eject from the engulfer
                    ForceEjectSingleEngulfable(ref engulfable.HostileEngulfer.Get<Engulfer>(),
                        engulfable.HostileEngulfer, entity);
                }
            }

            if (!entity.Has<Engulfer>())
                return;

            EjectEngulfablesOnDeath(entity);
        }

        protected override void PreUpdate(float state)
        {
            base.PreUpdate(state);

            if (gameWorld == null)
                throw new InvalidOperationException("GameWorld not set");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var engulfer = ref entity.Get<Engulfer>();
            ref var health = ref entity.Get<Health>();

            // Don't process engulfing when dead
            if (health.Dead)
            {
                // Need to eject everything (there's also a separate eject for to be deleted entities as not all dead
                // entities have a chance to still process this)
                if (engulfer.EngulfedObjects != null)
                {
                    // This sets the list to null to not constantly run this (the if block this is in won't get
                    // executed anymore)
                    EjectEverythingFromDeadEngulfer(ref engulfer, entity);
                }

                return;
            }

            usedTopLevelEngulfers.Add(entity);

            ref var control = ref entity.Get<MicrobeControl>();
            ref var cellProperties = ref entity.Get<CellProperties>();

            bool checkEngulfStartCollisions = false;

            var actuallyEngulfing = control.State == MicrobeState.Engulf && cellProperties.MembraneType.CanEngulf;

            if (actuallyEngulfing)
            {
                // Drain atp
                var cost = Constants.ENGULFING_ATP_COST_PER_SECOND * delta;

                var compounds = entity.Get<CompoundStorage>().Compounds;

                // Stop engulfing if out of ATP or if this is an engulfable that has been engulfed
                bool engulfed = false;

                if (entity.Has<Engulfable>())
                {
                    engulfed = entity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None;
                }

                if (compounds.TakeCompound(atp, cost) < cost - 0.001f || engulfed)
                {
                    control.SetStateColonyAware(entity, MicrobeState.Normal);
                }
                else
                {
                    checkEngulfStartCollisions = true;
                }
            }
            else
            {
                if (control.State == MicrobeState.Engulf)
                {
                    // Force out of incorrect state (but don't force whole colony in case there is a cell type in the
                    // colony that can engulf even if the leader can't)
                    control.State = MicrobeState.Normal;
                }
            }

            ref var soundPlayer = ref entity.Get<SoundEffectPlayer>();

            // Play sound
            if (actuallyEngulfing)
            {
                // To balance loudness, here the engulfment audio's max volume is reduced to 0.6 in linear volume
                soundPlayer.PlayGraduallyTurningUpLoopingSound(Constants.MICROBE_ENGULFING_MODE_SOUND, 0.6f, 0,
                    delta);
            }
            else
            {
                soundPlayer.PlayGraduallyTurningDownSound(Constants.MICROBE_ENGULFING_MODE_SOUND, delta);
            }

            bool hasColony = false;

            // Colony leader detects all collisions, even when not in engulf mode, as long as colony is in engulf mode
            if (entity.Has<MicrobeColony>())
            {
                hasColony = true;

                if (entity.Get<MicrobeColony>().ColonyState == MicrobeState.Engulf)
                    checkEngulfStartCollisions = true;
            }

            if (checkEngulfStartCollisions)
            {
                CheckStartEngulfing(ref entity.Get<CollisionManagement>(), ref cellProperties, ref engulfer,
                    entity, hasColony);
            }

            HandleExpiringExpelledObjects(ref engulfer, delta);

            if (engulfer.EngulfedObjects == null)
                return;

            // Update animations and move between different states when necessary for all the currently engulfed
            // objects
            for (int i = engulfer.EngulfedObjects.Count - 1; i >= 0; --i)
            {
                var engulfedEntity = engulfer.EngulfedObjects![i];

                if (!engulfedEntity.IsAlive || !engulfedEntity.Has<Engulfable>())
                {
                    // Clear once the object has been fully eaten / deleted. We can't call RemoveEngulfedObject
                    // as the engulfed object may be invalid already
                    engulfer.EngulfedObjects.RemoveAt(i);

                    continue;
                }

                usedEngulfedObjects.Add(new KeyValuePair<Entity, Entity>(entity, engulfedEntity));

                // Entities that were just engulfed need one extra update to materialize their new components
                if (beginningEngulfedObjects.Contains(engulfedEntity))
                    continue;

                ref var engulfable = ref engulfedEntity.Get<Engulfable>();

                var transportData = engulfable.BulkTransport;

                if (engulfable.PhagocytosisStep == PhagocytosisPhase.Digested &&
                    transportData?.DigestionEjectionStarted != true)
                {
                    if (transportData == null)
                    {
                        transportData = GetNewTransportAnimation();
                        engulfable.BulkTransport = transportData;
                    }

                    if (!engulfedEntity.Has<AttachedToEntity>())
                    {
                        GD.PrintErr("Engulfable is in Digested state but it has no attached component");
                        engulfer.EngulfedObjects.RemoveAt(i);
                    }

                    var currentEndosomeScale = Vector3.One * Mathf.Epsilon;
                    var endosome = GetEndosomeIfExists(entity, engulfedEntity);

                    if (endosome != null)
                        currentEndosomeScale = endosome.Scale;

                    transportData.TargetValuesToLerp = (null, null, Vector3.One * Mathf.Epsilon);
                    StartBulkTransport(ref engulfable, ref engulfedEntity.Get<AttachedToEntity>(), 1.5f,
                        currentEndosomeScale, false);
                }

                // Only handle the animations / state changes when they need updating
                if (transportData?.Interpolate != true &&
                    engulfable.PhagocytosisStep != PhagocytosisPhase.RequestExocytosis &&
                    engulfable.PhagocytosisStep != PhagocytosisPhase.Ejection)
                {
                    continue;
                }

                if (AnimateBulkTransport(entity, ref engulfable, engulfedEntity, delta))
                {
                    switch (engulfable.PhagocytosisStep)
                    {
                        case PhagocytosisPhase.Ingestion:
                            CompleteIngestion(entity, ref engulfable, engulfedEntity);
                            break;

                        case PhagocytosisPhase.Digested:
                            RemoveEngulfedObject(ref engulfer, engulfedEntity, ref engulfable, true);
                            break;

                        case PhagocytosisPhase.RequestExocytosis:
                            EjectEngulfable(ref engulfer, ref cellProperties, entity, false, ref engulfable,
                                engulfedEntity);
                            break;

                        case PhagocytosisPhase.Exocytosis:
                        {
                            var endosome = GetEndosomeIfExists(entity, engulfedEntity);

                            if (endosome != null)
                            {
                                endosome.Hide();
                                DeleteEndosome(endosome);
                                RemoveEndosomeFromEntity(entity, endosome);
                            }

                            if (transportData == null)
                            {
                                GD.PrintErr("Forcing ejection completion due to missing animation");
                                CompleteEjection(ref engulfer, entity, ref engulfable, engulfedEntity);
                                break;
                            }

                            // Preserve any previous animation properties that may have been setup by exocytosis
                            // request
                            transportData.TargetValuesToLerp = (transportData.TargetValuesToLerp.Translation,
                                engulfable.OriginalScale, transportData.TargetValuesToLerp.EndosomeScale);
                            StartBulkTransport(ref engulfable,
                                ref engulfedEntity.Get<AttachedToEntity>(), 1.0f,
                                Vector3.One);
                            engulfable.PhagocytosisStep = PhagocytosisPhase.Ejection;
                            break;
                        }

                        case PhagocytosisPhase.Ejection:
                            CompleteEjection(ref engulfer, entity, ref engulfable, engulfedEntity);
                            break;
                    }
                }
            }

            var colour = cellProperties.Colour;
            SetPhagosomeColours(entity, colour);
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

            beginningEngulfedObjects.Clear();

            // Delete unused endosome graphics. First mark unused things
            foreach (var entry in entityEngulfingEndosomeGraphics)
            {
                if (!usedTopLevelEngulfers.Contains(entry.Key))
                {
                    topLevelEngulfersToDelete.Add(entry.Key);
                    continue;
                }

                foreach (var childEntry in entry.Value)
                {
                    var key = new KeyValuePair<Entity, Entity>(entry.Key, childEntry.Key);
                    if (!usedEngulfedObjects.Contains(key))
                    {
                        engulfedObjectsToDelete.Add(key);
                    }
                }
            }

            usedTopLevelEngulfers.Clear();
            usedEngulfedObjects.Clear();

            // Then delete them
            foreach (var toDelete in topLevelEngulfersToDelete)
            {
                // Delete this entire top level entry
                var data = entityEngulfingEndosomeGraphics[toDelete];

                foreach (var endosome in data.Values)
                {
                    DeleteEndosome(endosome);
                }

                entityEngulfingEndosomeGraphics.Remove(toDelete);
            }

            // Single child object deletions instead of top level deletions
            foreach (var toDelete in engulfedObjectsToDelete)
            {
                var container = entityEngulfingEndosomeGraphics[toDelete.Key];

                if (container.TryGetValue(toDelete.Value, out var endosome))
                {
                    if (!container.Remove(toDelete.Value))
                        GD.PrintErr("Failed to remove endosome from entity list it was just deleted from");

                    DeleteEndosome(endosome);
                }
                else
                {
                    GD.PrintErr("Failed to get endosome to delete");
                }
            }

            topLevelEngulfersToDelete.Clear();
            engulfedObjectsToDelete.Clear();
        }

        /// <summary>
        ///   Ingestion variant for taking an object that is engulfed by a different engulfer and adding it to this
        ///   engulfer. Needs to match the core operations in the fresh ingest variant otherwise things will go very
        ///   wrong.
        /// </summary>
        /// <returns>True on success</returns>
        private static bool IngestEngulfableFromOtherEntity(ref Engulfer engulfer, in Entity engulferEntity,
            ref Engulfable engulfable, in Entity targetEntity, float animationSpeed = 3)
        {
            if (!targetEntity.Has<AttachedToEntity>())
            {
                GD.PrintErr(
                    $"Engulfable to move to different engulfer doesn't have {nameof(AttachedToEntity)} component");
                return false;
            }

            if (!engulferEntity.Has<CellProperties>())
            {
                GD.PrintErr("This ingest engulfable from other only works on cell type engulfers");
                return false;
            }

            ref var engulferCellProperties = ref engulferEntity.Get<CellProperties>();

            if (engulferCellProperties.CreatedMembrane == null)
            {
                GD.PrintErr("Failing to take over another engulfable as membrane is not generated yet");
                return false;
            }

            ref var targetSpatial = ref targetEntity.Get<SpatialInstance>();

            if (!CalculateEngulfableRadius(targetEntity, out var targetRadius))
            {
                GD.PrintErr("Failed to calculate engulfable radius of an engulfable to be moved to us");
                return false;
            }

            if (engulfable.PhagocytosisStep == PhagocytosisPhase.None)
            {
                GD.Print("Taking over an engulfable that is not in engulfed state, this is probably going " +
                    "to do something wrong");
            }

            float radius = engulferCellProperties.CreatedMembrane.EncompassingCircleRadius;

            if (engulferCellProperties.IsBacteria)
                radius *= 0.5f;

            // TODO: check that the positioning and animating make sense here, it should as this is only used for
            // recursively engulfed objects that should already be inside the engulfer, but re-checking this
            // functionality after the ECS conversion would be good.
            ref var targetEntityPosition = ref targetEntity.Get<WorldPosition>();
            ref var engulferPosition = ref engulferEntity.Get<WorldPosition>();

            AddEngulfableToEngulferDataList(ref engulfer, engulferEntity, ref engulfable, targetEntity);

            // Additional compounds have already been set by the original ingestion action

            var engulfableFinalPosition = CalculateEngulfableTargetPosition(ref engulferCellProperties,
                ref engulferPosition, radius, ref targetEntityPosition, ref targetSpatial, targetRadius, new Random(),
                out var relativePosition, out var boundingBoxSize);

            Vector3 originalScale;

            if (engulfable.OriginalScale != Vector3.Zero)
            {
                originalScale = engulfable.OriginalScale;
            }
            else
            {
                GD.PrintErr("Engulfable moved between engulfers has no original scale stored");

#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif

                originalScale = Vector3.One;
            }

            CreateEngulfableTransport(ref engulfable, engulfableFinalPosition, originalScale, boundingBoxSize);

            var initialEndosomeScale = CalculateInitialEndosomeScale();

            // If the other body is already attached this needs to handle that correctly

            var attached = AdjustExistingAttachedComponentForEngulfed(engulferEntity, ref targetEntityPosition,
                targetEntity, relativePosition);

            StartBulkTransport(ref engulfable, ref attached, animationSpeed, initialEndosomeScale);

            // Update render priority in this special case (normal case goes through OnBecomeEngulfed)
            if (targetEntity.Has<RenderPriorityOverride>() && engulferEntity.Has<RenderPriorityOverride>())
            {
                var engulferPriority = engulferEntity.Get<RenderPriorityOverride>().RenderPriority;

                ref var renderPriority = ref targetEntity.Get<RenderPriorityOverride>();

                // Make the render priority of our organelles be on top of the highest possible render priority
                // of the hostile engulfer's organelles
                renderPriority.RenderPriority = engulferPriority + Constants.HEX_MAX_RENDER_PRIORITY + 2;
                renderPriority.RenderPriorityApplied = false;

                // The above doesn't take recursive engulfing into account but that's probably fine enough in this case
            }

            // Physics should be already handled

            return true;
        }

        private static bool CalculateEngulfableRadius(in Entity targetEntity, out float targetRadius)
        {
            targetRadius = 1;

            if (targetEntity.Has<CellProperties>())
            {
                ref var targetCellProperties = ref targetEntity.Get<CellProperties>();

                // Skip for now if target membrane is not ready
                if (targetCellProperties.CreatedMembrane == null)
                    return false;

                targetRadius = targetCellProperties.CreatedMembrane.EncompassingCircleRadius;

                if (targetCellProperties.IsBacteria)
                    targetRadius *= 0.5f;
            }
            else if (targetEntity.Has<EntityRadiusInfo>())
            {
                targetRadius = targetEntity.Get<EntityRadiusInfo>().Radius;
            }
            else
            {
                GD.PrintErr("Unknown radius of engulfed object, won't know how far in it needs to be pulled");
            }

            return true;
        }

        private static void AddEngulfableToEngulferDataList(ref Engulfer engulfer, Entity engulferEntity,
            ref Engulfable engulfable, Entity targetEntity)
        {
            engulfable.HostileEngulfer = engulferEntity;
            engulfable.PhagocytosisStep = PhagocytosisPhase.Ingestion;

            engulfer.EngulfedObjects ??= new List<Entity>();
            engulfer.EngulfedObjects.Add(targetEntity);

            // Update used engulfing space, this will be re-calculated by the digestion system (as used space changes
            // as digestion progresses)
            engulfer.UsedIngestionCapacity += engulfable.AdjustedEngulfSize;
        }

        private static Vector3 CalculateEngulfableTargetPosition(ref CellProperties engulferCellProperties,
            ref WorldPosition engulferPosition, float radius, ref WorldPosition targetEntityPosition,
            ref SpatialInstance targetSpatial, float targetRadius, Random random, out Vector3 relativePosition,
            out Vector3 boundingBoxSize)
        {
            // Below is for figuring out where to place the object attempted to be engulfed inside the cytoplasm,
            // calculated accordingly to hopefully minimize any part of the object sticking out the membrane.
            // Note: extremely long and thin objects might still stick out

            var targetRadiusNormalized = Mathf.Clamp(targetRadius / radius, 0.0f, 1.0f);

            // This needs to convert the relative vector from world space to engulfer local space as this is used
            // as the attached component position so it is applied relative to the engulfer and its rotation
            relativePosition =
                engulferPosition.Rotation.Inverse().Xform(targetEntityPosition.Position - engulferPosition.Position);

            var nearestPointOfMembraneToTarget =
                engulferCellProperties.CreatedMembrane!.GetVectorTowardsNearestPointOfMembrane(relativePosition.x,
                    relativePosition.z);

            // The point nearest to the membrane calculation doesn't take being bacteria into account
            if (engulferCellProperties.IsBacteria)
                nearestPointOfMembraneToTarget *= 0.5f;

            // From the calculated nearest point of membrane above we then linearly interpolate it by the engulfed's
            // normalized radius to this cell's center in order to "shrink" the point relative to this cell's origin.
            // This will then act as a "maximum extent/edge" that qualifies as the interior of the engulfer's membrane
            var viableStoringAreaEdge =
                nearestPointOfMembraneToTarget.LinearInterpolate(Vector3.Zero, targetRadiusNormalized);

            // Get the final storing position by taking a value between this cell's center and the storing area edge.
            // This would lessen the possibility of engulfed things getting bunched up in the same position.
            var ingestionPoint = new Vector3(random.Next(0.0f, viableStoringAreaEdge.x),
                engulferPosition.Position.y,
                random.Next(0.0f, viableStoringAreaEdge.z));

            boundingBoxSize = Vector3.One;

            if (targetSpatial.GraphicalInstance != null)
            {
                var geometryInstance = targetSpatial.GraphicalInstance as GeometryInstance;

                // TODO: should this use EntityMaterial.AutoRetrieveModelPath to find the path of the graphics instance
                // in the node? This probably doesn't work for all kinds of chunks correctly

                // Most engulfables have their graphical node as the first child of their primary node
                if (geometryInstance == null && targetSpatial.GraphicalInstance.GetChildCount() > 0)
                {
                    geometryInstance = targetSpatial.GraphicalInstance.GetChild(0) as GeometryInstance;
                }

                if (geometryInstance != null)
                {
                    boundingBoxSize = geometryInstance.GetAabb().Size;

                    // Apply the current visual scale as it is not included in the AABB automatically
                    boundingBoxSize *= geometryInstance.Scale;
                }
                else
                {
                    GD.PrintErr("Engulfed something that couldn't have AABB calculated (graphical instance: ",
                        targetSpatial.GraphicalInstance, ")");
                }
            }
            else
            {
                GD.PrintErr(
                    "Engulfed something with no graphical instance set, can't calculate bounding box for scaling");
            }

            // In the case of flat mesh (like membrane) we don't want the endosome to end up completely flat
            // as it can cause unwanted visual glitch
            if (boundingBoxSize.y < Mathf.Epsilon)
                boundingBoxSize = new Vector3(boundingBoxSize.x, 0.1f, boundingBoxSize.z);

            return ingestionPoint;
        }

        private static void CreateEngulfableTransport(ref Engulfable engulfable, Vector3 ingestionPoint,
            Vector3 originalScale, Vector3 boundingBoxSize)
        {
            // Phagosome is now created when needed to be updated by the transport method instead of here immediately
            var bulkTransport = GetNewTransportAnimation();

            bulkTransport.TargetValuesToLerp = (ingestionPoint, originalScale / 2, boundingBoxSize);

            engulfable.BulkTransport = bulkTransport;
        }

        private static Engulfable.BulkTransportAnimation GetNewTransportAnimation()
        {
            lock (UnusedTransportAnimations)
            {
                return UnusedTransportAnimations.Count > 0 ?
                    UnusedTransportAnimations.Dequeue() :
                    new Engulfable.BulkTransportAnimation();
            }
        }

        private static Vector3 CalculateInitialEndosomeScale()
        {
            // TODO check what the initial scale of the endosome should be?
            var initialEndosomeScale = Vector3.One * Mathf.Epsilon;
            return initialEndosomeScale;
        }

        private static ref AttachedToEntity AdjustExistingAttachedComponentForEngulfed(in Entity engulferEntity,
            ref WorldPosition targetEntityPosition, in Entity targetEntity, Vector3 relativePosition)
        {
            ref var attached = ref targetEntity.Get<AttachedToEntity>();
            attached.AttachedTo = engulferEntity;
            attached.RelativePosition = relativePosition;
            attached.RelativeRotation = targetEntityPosition.Rotation.Inverse();

            return ref attached;
        }

        /// <summary>
        ///   Begins phagocytosis related lerp animation. Note that
        ///   <see cref="Engulfable.BulkTransportAnimation.TargetValuesToLerp"/> must be set before calling this.
        /// </summary>
        private static void StartBulkTransport(ref Engulfable engulfable,
            ref AttachedToEntity initialRelativePositionInfo, float duration, Vector3 currentEndosomeScale,
            bool resetElapsedTime = true)
        {
            if (engulfable.PhagocytosisStep == PhagocytosisPhase.None)
            {
                GD.PrintErr("Started bulk transport animation on not engulfed thing");
            }

            var transportData = engulfable.BulkTransport;

            // Only need to recreate the animation data when one doesn't exist, we can reuse existing data in other
            // cases
            if (transportData == null)
            {
                transportData = GetNewTransportAnimation();
                engulfable.BulkTransport = transportData;

                GD.PrintErr("New backup engulf animation data was created, this should be avoided " +
                    "(data should be created before bulk transport starts)");

#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif
            }

            if (resetElapsedTime)
                transportData.AnimationTimeElapsed = 0;

            var scale = Vector3.One;

            if (engulfable.OriginalScale.LengthSquared() < MathUtils.EPSILON)
            {
                GD.PrintErr("Started transport animation original scale is not set");

#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif
            }
            else
            {
                scale = engulfable.OriginalScale;
            }

            transportData.InitialValuesToLerp =
                (initialRelativePositionInfo.RelativePosition, scale, currentEndosomeScale);
            transportData.LerpDuration = duration;
            transportData.Interpolate = true;
        }

        /// <summary>
        ///   Stops phagocytosis related lerp animation
        /// </summary>
        private static void StopBulkTransport(Engulfable.BulkTransportAnimation animation)
        {
            // This tells the animation to not run anymore
            animation.Interpolate = false;

            animation.AnimationTimeElapsed = 0;
        }

        private Endosome CreateEndosome(in Entity entity, ref SpatialInstance endosomeParent, in Entity engulfedObject,
            int engulfedMaxRenderPriority)
        {
            if (endosomeParent.GraphicalInstance == null)
                throw new InvalidOperationException("Endosome parent SpatialInstance has no graphics");

            if (!entityEngulfingEndosomeGraphics.TryGetValue(entity, out var dataContainer))
            {
                dataContainer = new Dictionary<Entity, Endosome>();

                entityEngulfingEndosomeGraphics[entity] = dataContainer;
            }

            if (dataContainer.TryGetValue(engulfedObject, out var endosome))
                return endosome;

            // New entry needed
            var newData = endosomeScene.Instance<Endosome>();

            // Tint is not applied here as all phagosome tints are applied always after processing an engulfer

            newData.RenderPriority = engulfedMaxRenderPriority + dataContainer.Count + 1;

            endosomeParent.GraphicalInstance.AddChild(newData);

            dataContainer[engulfedObject] = newData;
            return newData;
        }

        private Endosome? GetEndosomeIfExists(in Entity entity, in Entity engulfedObject)
        {
            if (!entityEngulfingEndosomeGraphics.TryGetValue(entity, out var dataContainer))
                return null;

            if (dataContainer.TryGetValue(engulfedObject, out var endosome))
                return endosome;

            return null;
        }

        private void RemoveEndosomeFromEntity(in Entity entity, Endosome endosome)
        {
            if (entityEngulfingEndosomeGraphics.TryGetValue(entity, out var dataContainer))
            {
                foreach (var entry in dataContainer)
                {
                    if (entry.Value == endosome)
                    {
                        if (dataContainer.Remove(entry.Key))
                            return;
                    }
                }
            }

            GD.PrintErr("Failed to unlink endosome from engulfer that should have been using it");
        }

        private void EjectEverythingFromDeadEngulfer(ref Engulfer engulfer, in Entity entity)
        {
            if (engulfer.EngulfedObjects == null)
                return;

            // A copy of the list is needed as in some situations EjectEngulfable immediately removes an object
            // and modifies the engulfed list
            tempEntitiesToEject.AddRange(engulfer.EngulfedObjects);

            ref var cellProperties = ref entity.Get<CellProperties>();

            foreach (var engulfedObject in tempEntitiesToEject)
            {
                // In case here, the engulfer being dead, we check to make sure the engulfed objects aren't incorrect
                if (!engulfedObject.IsAlive || !engulfedObject.Has<Engulfable>())
                {
                    GD.PrintErr("Ejecting everything from a dead engulfable encountered a destroyed engulfed entity");
                    continue;
                }

                EjectEngulfable(ref engulfer, ref cellProperties, entity, true, ref engulfedObject.Get<Engulfable>(),
                    engulfedObject);
            }

            tempEntitiesToEject.Clear();

            // Should be fine to clear this list object like this as a dead entity should get deleted entirely
            // soon
            engulfer.EngulfedObjects = null;
        }

        private void CheckStartEngulfing(ref CollisionManagement collisionManagement, ref CellProperties cellProperties,
            ref Engulfer engulfer, in Entity entity, bool resolveColony)
        {
            ref var ourExtraData = ref entity.Get<MicrobePhysicsExtraData>();

            var count = collisionManagement.GetActiveCollisions(out var collisions);

            if (count < 1)
                return;

            ref var species = ref entity.Get<SpeciesMember>();

            for (int i = 0; i < count; ++i)
            {
                ref var collision = ref collisions![i];

                if (!collision.SecondEntity.Has<Engulfable>())
                    continue;

                // Can't engulf through our pilus
                if (ourExtraData.IsSubShapePilus(collision.FirstSubShapeData))
                    continue;

                var realTarget = collision.SecondEntity;

                // Also can't engulf when the other physics body has a pilus
                if (realTarget.Has<MicrobePhysicsExtraData>())
                {
                    ref var secondExtraData = ref realTarget.Get<MicrobePhysicsExtraData>();

                    if (secondExtraData.IsSubShapePilus(collision.SecondSubShapeData))
                        continue;

                    // Resolve potential colony for the second entity
                    if (realTarget.Has<MicrobeColony>())
                    {
                        ref var secondColony = ref realTarget.Get<MicrobeColony>();
                        if (secondColony.GetMicrobeFromSubShape(ref secondExtraData, collision.SecondSubShapeData,
                                out var adjusted))
                        {
                            realTarget = adjusted;
                        }
                    }
                }

                // Can't engulf dead things
                if (realTarget.Has<Health>() && realTarget.Get<Health>().Dead)
                    continue;

                // Pili don't block engulfing, check starting engulfing
                var realEngulfer = entity;

                if (resolveColony)
                {
                    // Need to resolve the real microbe that collided
                    ref var colony = ref entity.Get<MicrobeColony>();
                    if (colony.GetMicrobeFromSubShape(ref ourExtraData, collision.FirstSubShapeData, out var adjusted))
                    {
                        realEngulfer = adjusted;
                    }
                }

                ref var actualEngulfer = ref engulfer;

                ref var actualCellProperties =
                    ref realEngulfer == entity ? ref cellProperties : ref realEngulfer.Get<CellProperties>();

                if (CheckStartEngulfingOnCandidate(ref actualCellProperties, ref actualEngulfer, ref species,
                        realEngulfer, realTarget))
                {
                    // Engulf at most one thing per update, if further collisions still exist next update we'll pull
                    // it in then
                    return;
                }
            }
        }

        /// <summary>
        ///   This checks if we can start engulfing
        /// </summary>
        /// <returns>True if something started to be engulfed</returns>
        private bool CheckStartEngulfingOnCandidate(ref CellProperties cellProperties,
            ref Engulfer engulfer, ref SpeciesMember speciesMember, in Entity entity, in Entity engulfable)
        {
            var engulfCheckResult = cellProperties.CanEngulfObject(ref speciesMember, ref engulfer, engulfable);

            if (!engulfable.Has<Engulfable>())
            {
                GD.PrintErr("Cannot start engulfing entity that passed engulf check as it is missing " +
                    "engulfable component");
                return false;
            }

            if (engulfCheckResult == EngulfCheckResult.Ok)
            {
                // TODO: add some way for this to detect delay added components so that this can't conflict with the
                // binding system
                lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
                {
                    ref var engulfableComponent = ref engulfable.Get<Engulfable>();

                    if (engulfableComponent.PhagocytosisStep != PhagocytosisPhase.None)
                    {
                        throw new InvalidOperationException(
                            "Detected something that is currently engulfed as being engulfable");
                    }

                    return IngestEngulfable(ref engulfer, ref cellProperties, entity, ref engulfableComponent,
                        engulfable);
                }
            }

            if (engulfCheckResult == EngulfCheckResult.IngestedMatterFull)
            {
                if (entity.Has<MicrobeEventCallbacks>())
                {
                    ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

                    callbacks.OnEngulfmentStorageFull?.Invoke(entity);

                    entity.SendNoticeIfPossible(() =>
                        new SimpleHUDMessage(TranslationServer.Translate("NOTICE_ENGULF_STORAGE_FULL")));
                }
            }
            else if (engulfCheckResult == EngulfCheckResult.TargetTooBig)
            {
                if (entity.Has<MicrobeEventCallbacks>())
                {
                    entity.SendNoticeIfPossible(() =>
                        new SimpleHUDMessage(TranslationServer.Translate("NOTICE_ENGULF_SIZE_TOO_SMALL")));
                }
            }

            return false;
        }

        /// <summary>
        ///   Attempts to engulf the given target into the cytoplasm. Does not check whether the target
        ///   can be engulfed or not (as that check should be done already).
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This can be called from a different entity for another entity to engulf something. For example in the
        ///     case where an entity that has engulfed another is itself engulfed, in that case anything the first
        ///     engulfer ejects will get ingested by the other engulfer automatically.
        ///   </para>
        ///   <para>
        ///     Note that there is a variant of this method that takes an already engulfed object and moves it to a
        ///     different engulfer. These two methods need to be kept in sync if either is updated.
        ///   </para>
        /// </remarks>
        private bool IngestEngulfable(ref Engulfer engulfer, ref CellProperties engulferCellProperties,
            in Entity engulferEntity, ref Engulfable engulfable, in Entity targetEntity, float animationSpeed = 2.0f)
        {
            // Can't ingest before our membrane and the target membrane are ready (if target is a microbe)
            if (engulferCellProperties.CreatedMembrane == null)
                return false;

            if (!targetEntity.Has<SpatialInstance>())
            {
                GD.PrintErr("Only entities with spatial instance can be engulfed");
                return false;
            }

            ref var targetSpatial = ref targetEntity.Get<SpatialInstance>();

            // TODO: should this wait until target graphics are initialized?
            // if (targetSpatial.GraphicalInstance == null)

            if (!CalculateEngulfableRadius(targetEntity, out var targetRadius))
                return false;

            if (engulfable.PhagocytosisStep != PhagocytosisPhase.None)
            {
                GD.Print("Tried to ingest something that is already target of a phagocytosis process");
                return false;
            }

            float radius = engulferCellProperties.CreatedMembrane.EncompassingCircleRadius;

            if (engulferCellProperties.IsBacteria)
                radius *= 0.5f;

            EntityCommandRecorder? recorder = null;

            // Steal this cell from a colony if it is in a colony currently
            // Right now this causes extra operations for deleting the attach component but avoiding that would
            // complicate the code a lot here
            if (targetEntity.Has<MicrobeColonyMember>() || targetEntity.Has<MicrobeColony>())
            {
                recorder ??= worldSimulation.StartRecordingEntityCommands();

                // TODO: make sure that engulfing cells out of a colony don't cause issues
                // When testing I saw some bugs with cells just becoming ghosts when engulfing was attempted to be
                // started but that may have been caused by my testing method of overriding the required size ratio (
                // in just one place so maybe some other later check then immediately canceled the engulf)
                // - hhyyrylainen
                if (!MicrobeColonyHelpers.RemoveFromColony(targetEntity, recorder))
                {
                    GD.PrintErr("Failed to engulf a member of a cell colony (can't remove it)");
                    return false;
                }
            }

            AddEngulfableToEngulferDataList(ref engulfer, engulferEntity, ref engulfable, targetEntity);

            CalculateAdditionalCompoundsInNewlyEngulfedObject(ref engulfable, targetEntity);

            if (engulferEntity.Has<PlayerMarker>() && targetEntity.Has<CellProperties>())
            {
                gameWorld!.StatisticsTracker.TotalEngulfedByPlayer.Increment(1);
            }

            ref var targetEntityPosition = ref targetEntity.Get<WorldPosition>();
            ref var engulferPosition = ref engulferEntity.Get<WorldPosition>();

            var engulfableFinalPosition = CalculateEngulfableTargetPosition(ref engulferCellProperties,
                ref engulferPosition, radius, ref targetEntityPosition, ref targetSpatial, targetRadius, random,
                out var relativePosition, out var boundingBoxSize);

            ref var engulferPriority = ref engulferEntity.Get<RenderPriorityOverride>();

            // This sets the target render priority
            engulfable.OnBecomeEngulfed(targetEntity, engulferPriority.RenderPriority);

            // This is setup in OnBecomeEngulfed so this code must be after that
            var originalScale = engulfable.OriginalScale;

            CreateEngulfableTransport(ref engulfable, engulfableFinalPosition, originalScale, boundingBoxSize);

            // If the other body is already attached this needs to handle that correctly
            if (targetEntity.Has<AttachedToEntity>())
            {
                var attached = AdjustExistingAttachedComponentForEngulfed(engulferEntity, ref targetEntityPosition,
                    targetEntity, relativePosition);

                StartBulkTransport(ref engulfable, ref attached, animationSpeed,
                    CalculateInitialEndosomeScale());
            }
            else
            {
                recorder ??= worldSimulation.StartRecordingEntityCommands();

                var targetRecord = recorder.Record(targetEntity);

                var attached = new AttachedToEntity(engulferEntity, relativePosition,
                    targetEntityPosition.Rotation.Inverse());

                StartBulkTransport(ref engulfable, ref attached, animationSpeed,
                    CalculateInitialEndosomeScale());

                targetRecord.Set(attached);
            }

            if (recorder != null)
                worldSimulation.FinishRecordingEntityCommands(recorder);

            // Disable physics for the engulfed entity
            ref var physics = ref targetEntity.Get<Physics>();
            physics.BodyDisabled = true;

            // Skip updating this engulfable during this update as the attached component will only be created when
            // the command recorder is executed. And for consistency in the case that the component existed we still
            // do this as there should be no harm in this delay.
            beginningEngulfedObjects.Add(targetEntity);

            return true;
        }

        private void CompleteIngestion(in Entity entity, ref Engulfable engulfable, in Entity engulfedObject)
        {
            engulfable.PhagocytosisStep = PhagocytosisPhase.Ingested;

            if (entity.Has<MicrobeEventCallbacks>())
            {
                ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

                callbacks.OnSuccessfulEngulfment?.Invoke(entity, engulfedObject);
            }

            // There used to be an ingest callback like for the ejection but it didn't end up having any code in it
            // so it is now removed. Just the event callback above is left.
        }

        /// <summary>
        ///   Expels an ingested object from this microbe out into the environment.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Doesn't set <see cref="Engulfer.EngulfedObjects"/> to null even if empty
        ///   </para>
        /// </remarks>
        private void EjectEngulfable(ref Engulfer engulfer, ref CellProperties engulferCellProperties, in Entity entity,
            bool engulferDead, ref Engulfable engulfable, in Entity engulfedObject, float animationSpeed = 2.0f)
        {
            // If entity itself is engulfed, then it can't expel things. Except when dead as that overrides things
            if (entity.Has<Engulfable>() && entity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None &&
                !engulferDead)
            {
                return;
            }

            // TODO: being dead should probably override the following two if checks
            // Need to skip until the engulfer's membrane is ready
            if (engulferCellProperties.CreatedMembrane == null)
            {
                GD.PrintErr("Skipping ejecting engulfable as the engulfer doesn't have membrane ready yet");
                return;
            }

            if (engulfable.PhagocytosisStep is PhagocytosisPhase.Exocytosis or PhagocytosisPhase.None
                or PhagocytosisPhase.Ejection)
            {
                return;
            }

            if (engulfer.EngulfedObjects == null)
            {
                GD.PrintErr("Engulfer has no list of engulfed objects, it cannot expel anything");
                return;
            }

            if (!engulfer.EngulfedObjects.Contains(engulfedObject))
            {
                GD.PrintErr("Tried to eject something from engulfer that it hasn't engulfed");
                return;
            }

            engulfable.PhagocytosisStep = PhagocytosisPhase.Exocytosis;

            // The back of the microbe
            var exit = Hex.AxialToCartesian(new Hex(0, 1));
            var nearestPointOfMembraneToTarget =
                engulferCellProperties.CreatedMembrane.GetVectorTowardsNearestPointOfMembrane(exit.x, exit.z);

            // The point nearest to the membrane calculation doesn't take being bacteria into account
            if (engulferCellProperties.IsBacteria)
                nearestPointOfMembraneToTarget *= 0.5f;

            // If the animation is missing then for simplicity we just eject immediately or if the attached to
            // component is missing even though it should be always there

            if (!engulfedObject.Has<AttachedToEntity>())
            {
                GD.Print($"Immediately ejecting engulfable that has no {nameof(AttachedToEntity)} component");

                CompleteEjection(ref engulfer, entity, ref engulfable, engulfedObject);

#if DEBUG
                if (engulfer.EngulfedObjects?.Contains(engulfedObject) == true)
                {
                    throw new Exception("Complete ejection didn't remove engulfed object from list");
                }
#endif

                return;
            }

            ref var attached = ref engulfedObject.Get<AttachedToEntity>();

            var relativePosition = attached.RelativePosition;

            // If engulfer cell is dead (us) or the engulfed is positioned outside any of our closest membrane,
            // immediately eject it without animation.
            // TODO: Asses performance cost in massive cells (of the membrane Contains)?
            if (engulferDead ||
                !engulferCellProperties.CreatedMembrane.Contains(relativePosition.x, relativePosition.z))
            {
                CompleteEjection(ref engulfer, entity, ref engulfable, engulfedObject);

#if DEBUG
                if (engulfer.EngulfedObjects?.Contains(engulfedObject) == true)
                {
                    throw new Exception("Complete ejection didn't remove engulfed object from list");
                }
#endif
                return;
            }

            // Animate object move to the nearest point of the membrane
            var targetEndosomeScale = Vector3.One * Mathf.Epsilon;

            var endosome = GetEndosomeIfExists(entity, engulfedObject);

            var currentEndosomeScale = targetEndosomeScale;
            if (endosome != null)
            {
                currentEndosomeScale = endosome.Scale;
            }
            else
            {
                GD.PrintErr("Cannot properly animate endosome for ejection (current scale unknown)");
            }

            var animation = engulfable.BulkTransport;

            if (animation == null)
            {
                // Ejection was requested when there was no animation
                animation = GetNewTransportAnimation();
                engulfable.BulkTransport = animation;
            }

            animation.TargetValuesToLerp = (nearestPointOfMembraneToTarget, null, targetEndosomeScale);
            StartBulkTransport(ref engulfable, ref attached, animationSpeed, currentEndosomeScale);

            // The rest of the operation is done in CompleteEjection
        }

        private void CompleteEjection(ref Engulfer engulfer, in Entity entity, ref Engulfable engulfable,
            in Entity engulfableObject, bool canMoveToHigherLevelEngulfer = true)
        {
            if (engulfer.EngulfedObjects == null)
            {
                throw new InvalidOperationException(
                    "Engulfer trying to eject something when it doesn't even have engulfed objects list");
            }

            engulfer.ExpelledObjects ??= new Dictionary<Entity, float>();

            // Mark the object as recently expelled (0 seconds since ejection)
            engulfer.ExpelledObjects[engulfableObject] = 0;

            PerformEjectionForceAndAttachedRemove(entity, ref engulfable, engulfableObject);

            RemoveEngulfedObject(ref engulfer, engulfableObject, ref engulfable, false);

            // The phagosome will be deleted automatically, we just hide it here to make it disappear on the same frame
            // as the ejection completes
            var endosome = GetEndosomeIfExists(entity, engulfableObject);

            endosome?.Hide();

            if (entity.Has<Engulfable>() && canMoveToHigherLevelEngulfer)
            {
                ref var engulfersEngulfable = ref entity.Get<Engulfable>();

                if (engulfersEngulfable.PhagocytosisStep != PhagocytosisPhase.None)
                {
                    if (!engulfersEngulfable.HostileEngulfer.IsAlive ||
                        !engulfersEngulfable.HostileEngulfer.Has<Engulfer>())
                    {
                        GD.PrintErr("Attempt to pass ejected object to our engulfer failed because that " +
                            "engulfer is not alive");
                        return;
                    }

                    // Skip sending to the hostile engulfer if it is dead
                    if (engulfersEngulfable.HostileEngulfer.Has<Health>() &&
                        engulfersEngulfable.HostileEngulfer.Get<Health>().Dead)
                    {
                        GD.Print("Not sending engulfable to our engulfer as that is dead");
                        return;
                    }

                    ref var hostileEngulfer = ref engulfersEngulfable.HostileEngulfer.Get<Engulfer>();

                    // We have our own engulfer and it wants to claim this object we've just expelled
                    if (!IngestEngulfable(ref hostileEngulfer,
                            ref engulfersEngulfable.HostileEngulfer.Get<CellProperties>(),
                            engulfersEngulfable.HostileEngulfer, ref engulfable,
                            engulfableObject))
                    {
                        GD.PrintErr("Failed to pass ejected object from an engulfed object to its engulfer");
                    }
                }
            }
        }

        private void PerformEjectionForceAndAttachedRemove(in Entity entity, ref Engulfable engulfable,
            Entity engulfableObject)
        {
            var relativePosition = Vector3.Forward;

            // This lock is a bit useless but for symmetry on start this is also used here on eject
            lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
            {
                if (!engulfableObject.Has<AttachedToEntity>())
                {
                    GD.PrintErr("Ejected entity that has no attached component");
                }
                else
                {
                    relativePosition = engulfableObject.Get<AttachedToEntity>().RelativePosition;
                }

                var recorder = worldSimulation.StartRecordingEntityCommands();

                var recorderEntity = recorder.Record(engulfableObject);

                // Stop this entity being attached to us
                recorderEntity.Remove<AttachedToEntity>();

                worldSimulation.FinishRecordingEntityCommands(recorder);
            }

            // Try to get velocity of the engulfer for ejection impulse strength calculation
            var engulferVelocity = Vector3.Zero;

            // This failing is not critical as a stationary non-physics based engulfer could make sense, in which case
            // the engulfer's velocity being assumed to be 0 is entirely correct
            if (entity.Has<Physics>())
            {
                ref var engulferPhysics = ref entity.Get<Physics>();

                if (engulferPhysics.TrackVelocity)
                {
                    engulferVelocity = engulferPhysics.Velocity;
                }
                else
                {
                    GD.PrintErr("Engulfer doesn't track velocity, can't apply correct ejection impulse");
                }
            }

            // Re-enable physics
            ref var physics = ref engulfableObject.Get<Physics>();
            physics.BodyDisabled = false;

            ref var engulferPosition = ref entity.Get<WorldPosition>();

            // And give an impulse
            // TODO: check is it correct to rotate by the rotation here on the relative position for this force
            var relativeVelocity =
                engulferPosition.Rotation.Xform(relativePosition) * Constants.ENGULF_EJECTION_VELOCITY;

            // Apply outwards ejection speed
            physics.Velocity = engulferVelocity + relativeVelocity;
            physics.AngularVelocity = Vector3.Zero;
            physics.VelocitiesApplied = false;

            // Reset engulfable state after the ejection (but before RemoveEngulfedObject to allow this to still see
            // the hostile engulfer entity)
            engulfable.OnExpelledFromEngulfment(engulfableObject, spawnSystem, worldSimulation);
        }

        /// <summary>
        ///   Removes an engulfed object from the data lists in an engulfer and detaches the animation state.
        ///   Doesn't do any ejection actions. This is purely for once data needs to be removed once it is safe to do
        ///   so.
        /// </summary>
        private void RemoveEngulfedObject(ref Engulfer engulfer, Entity engulfedEntity, ref Engulfable engulfable,
            bool destroy)
        {
            if (engulfer.EngulfedObjects == null)
                throw new InvalidOperationException("Engulfed objects should not be null when this is called");

            if (!engulfer.EngulfedObjects.Remove(engulfedEntity))
            {
                GD.PrintErr("Failed to remove engulfed object from engulfer's list of engulfed objects");
            }

            var transport = engulfable.BulkTransport;
            if (transport != null)
            {
                transport.Interpolate = false;
                transport.DigestionEjectionStarted = false;

                lock (UnusedTransportAnimations)
                {
                    UnusedTransportAnimations.Enqueue(transport);
                }

                engulfable.BulkTransport = null;
            }

            engulfable.PhagocytosisStep = PhagocytosisPhase.None;
            engulfable.HostileEngulfer = default;

#if DEBUG
            if (engulfedEntity.Get<SpatialInstance>().VisualScale != engulfable.OriginalScale && !destroy)
            {
                GD.PrintErr("Original scale not applied correctly before eject");

                if (Debugger.IsAttached)
                    Debugger.Break();
            }
#endif

            // Thanks to digestion decreasing the size of engulfed objects, this doesn't match what we took in
            // originally. This relies on the digestion system updating this later to make sure this is correct
            engulfer.UsedIngestionCapacity =
                Math.Max(0, engulfer.UsedIngestionCapacity - engulfable.AdjustedEngulfSize);

            if (destroy)
            {
                worldSimulation.DestroyEntity(engulfedEntity);
            }
        }

        /// <summary>
        ///   Animates transporting objects from phagocytosis process with linear interpolation.
        /// </summary>
        /// <returns>True when Lerp finishes.</returns>
        private bool AnimateBulkTransport(in Entity entity, ref Engulfable engulfable, in Entity engulfedObject,
            float delta)
        {
            ref var spatial = ref engulfedObject.Get<SpatialInstance>();

            if (spatial.GraphicalInstance == null)
            {
                // Can't create phagosome until spatial instance is created. Returning false here will retry the bulk
                // transport animation each update.
                return false;
            }

            var animation = engulfable.BulkTransport;

            if (animation == null)
            {
                // Exocytosis request can be performed even without animation starting
                if (engulfable.PhagocytosisStep == PhagocytosisPhase.RequestExocytosis)
                    return true;

                // Some code didn't initialize the animation data
                GD.PrintErr($"{nameof(AnimateBulkTransport)} cannot run because bulk animation data is null");
                return true;
            }

            if (!animation.Interpolate)
            {
                // Animation is complete, this happens when the steps are updated for example to request exocytosis
                return true;
            }

            // Safety check in case the animation started too soon (component not created yet)
            if (!engulfedObject.Has<AttachedToEntity>())
            {
                GD.PrintErr("Engulfed object doesn't have attached to component set when doing bulk animation");
                return false;
            }

            var endosome = GetEndosomeIfExists(entity, engulfedObject);

            if (endosome == null)
            {
                // TODO: if state is ejecting then phagosome creation should be skipped to save creating an object that
                // will be deleted in a few frames anyway

                // 1 is from membrane
                int basePriority = 1 + Constants.HEX_MAX_RENDER_PRIORITY;

                if (engulfedObject.Has<RenderPriorityOverride>())
                {
                    basePriority += engulfedObject.Get<RenderPriorityOverride>().RenderPriority;
                }

                // Form phagosome as it is missing
                endosome = CreateEndosome(entity, ref spatial, engulfedObject, basePriority);
            }

            ref var relativePosition = ref engulfedObject.Get<AttachedToEntity>();

            if (animation.AnimationTimeElapsed < animation.LerpDuration)
            {
                animation.AnimationTimeElapsed += delta;

                var fraction = animation.AnimationTimeElapsed / animation.LerpDuration;

                // Ease out
                fraction = Mathf.Sin(fraction * Mathf.Pi * 0.5f);

                if (animation.TargetValuesToLerp.Translation.HasValue)
                {
                    relativePosition.RelativePosition = animation.InitialValuesToLerp.Translation.LinearInterpolate(
                        animation.TargetValuesToLerp.Translation.Value, fraction);
                }

                // There's an extra safety check here about the scale animation to not accidentally override things
                // if the object has already restored its real scale (this shouldn't be necessary but I added this here
                // anyway when trying to debug a visual scale flickering problem related to engulfing -hhyyrylainen)
                if (animation.TargetValuesToLerp.Scale.HasValue && animation.Interpolate)
                {
                    spatial.VisualScale = animation.InitialValuesToLerp.Scale.LinearInterpolate(
                        animation.TargetValuesToLerp.Scale.Value, fraction);
                    spatial.ApplyVisualScale = true;
                }

                if (animation.TargetValuesToLerp.EndosomeScale.HasValue)
                {
                    endosome.Scale = animation.InitialValuesToLerp.EndosomeScale.LinearInterpolate(
                        animation.TargetValuesToLerp.EndosomeScale.Value, fraction);
                }

                // Endosome is parented to the visuals of the engulfed object, so its position shouldn't be updated
                // endosome.Translation = relativePosition.RelativePosition;

                return false;
            }

            // Snap values
            if (animation.TargetValuesToLerp.Translation.HasValue)
                relativePosition.RelativePosition = animation.TargetValuesToLerp.Translation.Value;

            // See the comment above where Interpolate is also referenced as to why it is here as well
            if (animation.TargetValuesToLerp.Scale.HasValue && animation.Interpolate)
            {
                spatial.VisualScale = animation.TargetValuesToLerp.Scale.Value;
                spatial.ApplyVisualScale = true;
            }

            if (animation.TargetValuesToLerp.EndosomeScale.HasValue)
                endosome.Scale = animation.TargetValuesToLerp.EndosomeScale.Value;

            StopBulkTransport(animation);

            return true;
        }

        private void HandleExpiringExpelledObjects(ref Engulfer engulfer, float delta)
        {
            if (engulfer.ExpelledObjects == null)
                return;

            foreach (var expelled in engulfer.ExpelledObjects)
            {
                tempWorkSpaceForTimeReduction.Add(new KeyValuePair<Entity, float>(expelled.Key,
                    expelled.Value + delta));
            }

            foreach (var pair in tempWorkSpaceForTimeReduction)
            {
                if (pair.Value >= Constants.ENGULF_EJECTED_COOLDOWN)
                {
                    engulfer.ExpelledObjects.Remove(pair.Key);
                }
                else
                {
                    engulfer.ExpelledObjects[pair.Key] = pair.Value;
                }
            }

            tempWorkSpaceForTimeReduction.Clear();
        }

        private void SetPhagosomeColours(in Entity entity, Color colour)
        {
            if (!entityEngulfingEndosomeGraphics.TryGetValue(entity, out var endosomes))
                return;

            foreach (var endosomeEntry in endosomes)
            {
                endosomeEntry.Value.Tint = colour;
            }
        }

        /// <summary>
        ///   Performs the deletion of endosome operation. Note that contexts where the endosome may still be used
        ///   by an entity <see cref="RemoveEndosomeFromEntity"/> needs to be called
        /// </summary>
        /// <param name="endosome">The endosome object that is no longer required</param>
        private void DeleteEndosome(Endosome endosome)
        {
            try
            {
                if (endosome.IsQueuedForDeletion())
                    return;

                endosome.QueueFree();
            }
            catch (ObjectDisposedException)
            {
                // This can happen when the engulfed entity's visual instance has already been destroyed and
                // that resulted in the endosome graphics node to be deleted as it is parented there

                // Only print this message once as otherwise it gets printed quite a lot (at least in the benchmark)
                if (!endosomeDebugAlreadyPrinted)
                {
                    GD.Print("Endosome was already disposed");
                    endosomeDebugAlreadyPrinted = true;
                }

                // If caching is added already destroyed endosomes have to be skipped here
                // return;
            }

            // TODO: caching for endosomes (need to detach from the old parent)
        }

        private void CalculateAdditionalCompoundsInNewlyEngulfedObject(ref Engulfable engulfable,
            in Entity engulfableEntity)
        {
            engulfable.AdditionalEngulfableCompounds =
                engulfable.CalculateAdditionalDigestibleCompounds(engulfableEntity);

            if (engulfableEntity.Has<CompoundStorage>())
            {
                engulfable.InitialTotalEngulfableCompounds = engulfableEntity.Get<CompoundStorage>().Compounds
                    .Where(c => c.Key.Digestible)
                    .Sum(c => c.Value);

#if DEBUG
                foreach (var entry in engulfableEntity.Get<CompoundStorage>().Compounds
                             .Where(c => c.Key.Digestible))
                {
                    if (entry.Value < 0)
                        throw new Exception("Negative stored compound amount in engulfed cell");
                }
#endif
            }
            else
            {
                // This is a fallback against causing a crash here, but engulfing won't be able to digest anything
                engulfable.InitialTotalEngulfableCompounds = 0;
            }

            if (engulfable.AdditionalEngulfableCompounds != null)
            {
#if DEBUG
                foreach (var entry in engulfable.AdditionalEngulfableCompounds)
                {
                    if (entry.Value < 0)
                        throw new Exception("Negative calculated additional compound");
                }
#endif

                engulfable.InitialTotalEngulfableCompounds +=
                    engulfable.AdditionalEngulfableCompounds.Sum(c => c.Value);
            }
        }

        private void EjectEngulfablesOnDeath(Entity entity)
        {
            ref var engulfer = ref entity.Get<Engulfer>();

            if (engulfer.EngulfedObjects is not { Count: > 0 })
                return;

            // Immediately force eject all the engulfed objects
            // Loop is used here to be able to release all the objects that can be (are not dead / missing components)
            for (int i = engulfer.EngulfedObjects.Count - 1; i >= 0; --i)
            {
                ForceEjectSingleEngulfable(ref engulfer, entity, engulfer.EngulfedObjects![i]);
            }
        }

        private void ForceEjectSingleEngulfable(ref Engulfer engulfer, in Entity entity, in Entity toEject)
        {
            if (!toEject.Has<Engulfable>())
            {
                GD.Print("Skip ejecting engulfable on engulfer destroy as it no longer has engulfable component");
                return;
            }

            ref var engulfable = ref toEject.Get<Engulfable>();

            // This shouldn't happen but here's this workaround to stop crashing
            if (engulfer.EngulfedObjects == null)
            {
                GD.PrintErr("Force ejection on engulfer that doesn't have engulfed object list setup is skipping " +
                    "normal eject logic");

                PerformEjectionForceAndAttachedRemove(entity, ref engulfable, toEject);
                return;
            }

            CompleteEjection(ref engulfer, entity, ref engulfable, toEject, false);
        }
    }
}
