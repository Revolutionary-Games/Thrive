namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using Vector3 = Godot.Vector3;
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
    [WritesToComponent(typeof(AttachedChildren))]
    [WritesToComponent(typeof(Physics))]
    [ReadsComponent(typeof(CellProperties))]
    [ReadsComponent(typeof(SpeciesMember))]
    [ReadsComponent(typeof(MicrobeEventCallbacks))]
    [ReadsComponent(typeof(PhysicsShapeHolder))]
    [RunsAfter(typeof(PilusDamageSystem))]
    [RunsAfter(typeof(MicrobeVisualsSystem))]
    [RunsOnMainThread]
    public sealed class EngulfingSystem : AEntitySetSystem<float>
    {
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
        ///   Cache to re-use bulk transport animation objects
        /// </summary>
        private readonly Queue<Engulfable.BulkTransportAnimation> unusedTransportAnimations = new();

        // TODO: caching for Endosome scenes (will need to report as intentionally orphaned nodes)

        /// <summary>
        ///   Temporary storage for some expelled object expire time calculations, used to avoid allocating an extra
        ///   list per update.
        /// </summary>
        private readonly List<KeyValuePair<Entity, float>> tempWorkSpaceForTimeReduction = new();

        public EngulfingSystem(IWorldSimulation worldSimulation, ISpawnSystem spawnSystem, World world) :
            base(world, null)
        {
            this.worldSimulation = worldSimulation;
            this.spawnSystem = spawnSystem;
            endosomeScene = GD.Load<PackedScene>("res://src/microbe_stage/Endosome.tscn");

            atp = SimulationParameters.Instance.GetCompound("atp");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var engulfer = ref entity.Get<Engulfer>();
            ref var health = ref entity.Get<Health>();

            // Don't process engulfing when dead
            if (health.Dead)
            {
                // TODO: should this wait until death is processed?

                // Need to eject everything
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
                    control.State = MicrobeState.Normal;
                }
                else
                {
                    if (entity.Has<MicrobeColonyMember>())
                    {
                        // TODO: fix colony members to be able to engulf things
                        throw new NotImplementedException();
                    }

                    CheckStartEngulfing(ref entity.Get<CollisionManagement>(), ref cellProperties, ref engulfer,
                        entity);
                }
            }
            else
            {
                if (control.State == MicrobeState.Engulf)
                {
                    // Force out of incorrect state
                    control.State = MicrobeState.Normal;
                }
            }

            ref var soundPlayer = ref entity.Get<SoundEffectPlayer>();

            // To simplify the logic this audio is now played non-looping
            // TODO: if this sounds too bad with the sound volume no longer fading then this will need to change
            soundPlayer.PlaySoundEffectIfNotPlayingAlready(Constants.MICROBE_BINDING_MODE_SOUND, 0.6f);

            // Play sound
            if (actuallyEngulfing)
            {
                // To balance loudness, here the engulfment audio's max volume is reduced to 0.6 in linear volume
                soundPlayer.PlayGraduallyTurningUpLoopingSound(Constants.MICROBE_ENGULFING_MODE_SOUND, 0.6f, 0, delta);
            }
            else
            {
                soundPlayer.PlayGraduallyTurningDownSound(Constants.MICROBE_ENGULFING_MODE_SOUND, delta);
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

                ref var engulfable = ref engulfedEntity.Get<Engulfable>();

                var transportData = engulfable.BulkTransport;

                if (engulfable.PhagocytosisStep == PhagocytosisPhase.Digested)
                {
                    if (transportData == null)
                    {
                        transportData = new Engulfable.BulkTransportAnimation();
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
                    StartBulkTransport(ref engulfable, engulfedEntity, ref engulfedEntity.Get<AttachedToEntity>(), 1.5f,
                        currentEndosomeScale, false);
                }

                // Only handle the animations / state changes when they need updating
                if (transportData?.Interpolate != true)
                    continue;

                if (AnimateBulkTransport(entity, ref engulfable, engulfedEntity, delta))
                {
                    switch (engulfable.PhagocytosisStep)
                    {
                        case PhagocytosisPhase.Ingestion:
                            CompleteIngestion(entity, ref engulfable, engulfedEntity);
                            break;

                        case PhagocytosisPhase.Digested:
                            RemoveEngulfedObject(ref engulfer, engulfedEntity, ref engulfable);
                            worldSimulation.DestroyEntity(engulfedEntity);
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
                            }

                            transportData.TargetValuesToLerp = (null, transportData.OriginalScale, null);
                            StartBulkTransport(ref engulfable, engulfedEntity,
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

        private void EjectEverythingFromDeadEngulfer(ref Engulfer engulfer, in Entity entity)
        {
            if (engulfer.EngulfedObjects == null)
                return;

            ref var cellProperties = ref entity.Get<CellProperties>();

            foreach (var engulfedObject in engulfer.EngulfedObjects)
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

            // Should be fine to clear this list object like this as a dead entity should get deleted entirely
            // soon
            engulfer.EngulfedObjects = null;
        }

        private void CheckStartEngulfing(ref CollisionManagement collisionManagement, ref CellProperties cellProperties,
            ref Engulfer engulfer, in Entity entity)
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

                // Also can't engulf when the other physics body has a pilus
                if (collision.SecondEntity.Has<MicrobePhysicsExtraData>() && collision.SecondEntity
                        .Get<MicrobePhysicsExtraData>().IsSubShapePilus(collision.SecondSubShapeData))
                {
                    continue;
                }

                // Can't engulf dead things
                if (collision.SecondEntity.Has<Health>() && collision.SecondEntity.Get<Health>().Dead)
                    continue;

                // Pili don't block engulfing, check starting engulfing
                if (CheckStartEngulfingOnCandidate(ref cellProperties, ref engulfer, ref species, in entity,
                        collision.SecondEntity))
                {
                    // Engulf at most one thing per update, if the collision still exist next update we'll pull it in
                    // then
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
                    return IngestEngulfable(ref engulfer, ref cellProperties, entity, ref engulfable.Get<Engulfable>(),
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

            float targetRadius = 1;

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

            if (engulfable.PhagocytosisStep != PhagocytosisPhase.None)
            {
                GD.Print("Tried to ingest something that is already target of a phagocytosis process");
                return false;
            }

            float radius = engulferCellProperties.CreatedMembrane.EncompassingCircleRadius;

            if (engulferCellProperties.IsBacteria)
                radius *= 0.5f;

            ref var targetEntityPosition = ref targetEntity.Get<WorldPosition>();
            ref var engulferPosition = ref engulferEntity.Get<WorldPosition>();

            engulfable.HostileEngulfer = engulferEntity;
            engulfable.PhagocytosisStep = PhagocytosisPhase.Ingestion;

            engulfer.EngulfedObjects ??= new List<Entity>();
            engulfer.EngulfedObjects.Add(targetEntity);

            // Update used engulfing space, this will be re-calculated by the digestion system (as used space changes
            // as digestion progresses)
            engulfer.UsedIngestionCapacity += engulfable.AdjustedEngulfSize;

            CalculateAdditionalCompoundsInNewlyEngulfedObject(ref engulfable, targetEntity);

            if (targetEntity.Has<MicrobeColonyMember>())
            {
                // Steal this cell from a colony if it is in a colony currently

                // TODO: implement eating members from colonies
                throw new NotImplementedException();

                // Colony?.RemoveFromColony(targetEntity);
            }

            // Below is for figuring out where to place the object attempted to be engulfed inside the cytoplasm,
            // calculated accordingly to hopefully minimize any part of the object sticking out the membrane.
            // Note: extremely long and thin objects might still stick out

            var targetRadiusNormalized = Mathf.Clamp(targetRadius / radius, 0.0f, 1.0f);

            var relativePosition = targetEntityPosition.Position - engulferPosition.Position;
            var rotatedRelativeVector = engulferPosition.Rotation.Xform(relativePosition);

            var nearestPointOfMembraneToTarget =
                engulferCellProperties.CreatedMembrane.GetVectorTowardsNearestPointOfMembrane(
                    rotatedRelativeVector.x, rotatedRelativeVector.z);

            // The point nearest to the membrane calculation doesn't take being bacteria into account
            if (engulferCellProperties.IsBacteria)
                nearestPointOfMembraneToTarget *= 0.5f;

            // From the calculated nearest point of membrane above we then linearly interpolate it by the engulfed's
            // normalized radius to this cell's center in order to "shrink" the point relative to this cell's origin.
            // This will then act as a "maximum extent/edge" that qualifies as the interior of the engulfer's membrane
            var viableStoringAreaEdge = nearestPointOfMembraneToTarget.LinearInterpolate(
                Vector3.Zero, targetRadiusNormalized);

            // Get the final storing position by taking a value between this cell's center and the storing area edge.
            // This would lessen the possibility of engulfed things getting bunched up in the same position.
            var ingestionPoint = new Vector3(
                random.Next(0.0f, viableStoringAreaEdge.x),
                engulferPosition.Position.y,
                random.Next(0.0f, viableStoringAreaEdge.z));

            var boundingBoxSize = Vector3.One;

            if (targetSpatial.GraphicalInstance is GeometryInstance geometryInstance)
            {
                boundingBoxSize = geometryInstance.GetAabb().Size;
            }
            else
            {
                GD.PrintErr("Engulfed something that couldn't have AABB calculated (graphical instance: ",
                    targetSpatial.GraphicalInstance, ")");
            }

            // In the case of flat mesh (like membrane) we don't want the endosome to end up completely flat
            // as it can cause unwanted visual glitch
            if (boundingBoxSize.y < Mathf.Epsilon)
                boundingBoxSize = new Vector3(boundingBoxSize.x, 0.1f, boundingBoxSize.z);

            var originalScale = Vector3.One;

            if (targetSpatial.ApplyVisualScale)
                originalScale = targetSpatial.VisualScale;

            // Phagosome is now created when needed to be updated by the transport method instead of here immediately

            var bulkTransport = unusedTransportAnimations.Count > 0 ?
                unusedTransportAnimations.Dequeue() :
                new Engulfable.BulkTransportAnimation();

            bulkTransport.TargetValuesToLerp = (ingestionPoint, originalScale / 2, boundingBoxSize);
            bulkTransport.OriginalScale = originalScale;

            // TODO: store original render priority?
            // bulkTransport.OriginalRenderPriority = target.RenderPriority,

            engulfable.BulkTransport = bulkTransport;

            // Disable physics for the engulfed entity
            ref var physics = ref targetEntity.Get<Physics>();
            physics.BodyDisabled = true;

            // TODO check what the initial scale of the endosome should be?
            var initialEndosomeScale = Vector3.One * Mathf.Epsilon;

            // If the other body is already attached this needs to handle that correctly
            if (targetEntity.Has<AttachedToEntity>())
            {
                ref var attached = ref targetEntity.Get<AttachedToEntity>();
                attached.AttachedTo = engulferEntity;
                attached.RelativePosition = relativePosition;
                attached.RelativeRotation = targetEntityPosition.Rotation.Inverse();

                StartBulkTransport(ref engulfable, targetEntity, ref attached, animationSpeed, initialEndosomeScale);
            }
            else
            {
                var recorder = worldSimulation.StartRecordingEntityCommands();

                var targetRecord = recorder.Record(targetEntity);

                var attached = new AttachedToEntity(engulferEntity, relativePosition,
                    targetEntityPosition.Rotation.Inverse());

                StartBulkTransport(ref engulfable, targetEntity, ref attached, animationSpeed, initialEndosomeScale);

                targetRecord.Set(attached);

                worldSimulation.FinishRecordingEntityCommands(recorder);
            }

            // TODO: render priority
            // We want the ingested material to be always visible over the organelles
            // target.RenderPriority += OrganelleMaxRenderPriority + 1;

            engulfable.OnBecomeEngulfed(targetEntity);
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
        private void EjectEngulfable(ref Engulfer engulfer, ref CellProperties engulferCellProperties, in Entity entity,
            bool engulferDead,
            ref Engulfable engulfable, in Entity engulfedObject, float animationSpeed = 2.0f)
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
                return;

            if (engulfable.PhagocytosisStep is PhagocytosisPhase.Exocytosis or PhagocytosisPhase.None
                or PhagocytosisPhase.Ejection)
            {
                return;
            }

            if (engulfer.EngulfedObjects == null)
                return;

            if (!engulfer.EngulfedObjects.Contains(engulfedObject))
                return;

            engulfable.PhagocytosisStep = PhagocytosisPhase.Exocytosis;

            // The back of the microbe
            var exit = Hex.AxialToCartesian(new Hex(0, 1));
            var nearestPointOfMembraneToTarget =
                engulferCellProperties.CreatedMembrane.GetVectorTowardsNearestPointOfMembrane(exit.x, exit.z);

            // The point nearest to the membrane calculation doesn't take being bacteria into account
            if (engulferCellProperties.IsBacteria)
                nearestPointOfMembraneToTarget *= 0.5f;

            var animation = engulfable.BulkTransport;

            // If the animation is missing then for simplicity we just eject immediately or if the attached to
            // component is missing even though it should be always there

            if (animation == null || engulfedObject.Has<AttachedToEntity>())
            {
                GD.Print("Immediately ejecting engulfable that has no animation properties (or missing " +
                    "attached component)");

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

            var currentEndosomeScale = targetEndosomeScale;

            var endosome = GetEndosomeIfExists(entity, engulfedObject);

            if (endosome != null)
            {
                currentEndosomeScale = endosome.Scale;
            }

            animation.TargetValuesToLerp = (nearestPointOfMembraneToTarget, null, targetEndosomeScale);
            StartBulkTransport(ref engulfable, engulfedObject, ref attached, animationSpeed, currentEndosomeScale);

            // The rest of the operation is done in CompleteEjection
        }

        private void CompleteEjection(ref Engulfer engulfer, in Entity entity, ref Engulfable engulfable,
            in Entity engulfableObject)
        {
            if (engulfer.EngulfedObjects == null)
            {
                throw new InvalidOperationException(
                    "Engulfer trying to eject something when it doesn't even have engulfed objects list");
            }

            engulfer.ExpelledObjects ??= new Dictionary<Entity, float>();

            // Mark the object as recently expelled (0 seconds since ejection)
            engulfer.ExpelledObjects[engulfableObject] = 0;

            Vector3 relativePosition = Vector3.Forward;

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

            // Try to get mass for ejection impulse strength calculation
            float mass = 1000;
            if (engulfableObject.Has<PhysicsShapeHolder>())
            {
                ref var shape = ref engulfableObject.Get<PhysicsShapeHolder>();

                if (shape.Shape != null)
                {
                    mass = shape.Shape.GetMass();
                }
                else
                {
                    GD.PrintErr("Expelled engulfed object doesn't have physics shape initialized, " +
                        "ejection impulse won't be correctly calculated");
                }
            }
            else
            {
                GD.PrintErr("Engulfed object doesn't have shape component, can't know mass for ejection impulse");
            }

            // Re-enable physics
            ref var physics = ref engulfableObject.Get<Physics>();
            physics.BodyDisabled = false;

            ref var engulferPosition = ref entity.Get<WorldPosition>();

            // And give an impulse
            // TODO: check is it correct to rotate by the rotation here on the relative position for this force
            var impulse = engulferPosition.Rotation.Xform(relativePosition) * mass * Constants.ENGULF_EJECTION_FORCE;

            // Apply outwards ejection force
            ref var manualPhysicsControl = ref engulfableObject.Get<ManualPhysicsControl>();
            manualPhysicsControl.ImpulseToGive += impulse + engulferVelocity;
            manualPhysicsControl.PhysicsApplied = false;

            var animation = engulfable.BulkTransport;

            // For now assume that if the animation is missing then no property modifications were done, so this is
            // perfectly fine to skip
            if (animation != null)
            {
                // Reset render priority
                // TODO: render priority
                // engulfable.RenderPriority = animation.OriginalRenderPriority;

                // Restore scale
                if (engulfableObject.Has<SpatialInstance>())
                {
                    ref var spatial = ref engulfableObject.Get<SpatialInstance>();
                    spatial.VisualScale = animation.OriginalScale;

#if DEBUG
                    if (animation.OriginalScale.Length() < MathUtils.EPSILON)
                    {
                        GD.PrintErr("Ejected engulfable with zero original scale");
                    }
#endif
                }
            }

            // Reset engulfable state after the ejection (but before RemoveEngulfedObject to allow this to still see
            // the hostile engulfer entity)
            engulfable.OnExpelledFromEngulfment(engulfableObject, spawnSystem, worldSimulation);

            RemoveEngulfedObject(ref engulfer, engulfableObject, ref engulfable);

            // The phagosome will be deleted automatically, we just hide it here to make it disappear on the same frame
            // as the ejection completes
            var phagosome = GetEndosomeIfExists(entity, engulfableObject);

            phagosome?.Hide();

            if (entity.Has<Engulfable>())
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

        /// <summary>
        ///   Removes an engulfed object from the data lists in an engulfer and detaches the animation state.
        ///   Doesn't do any ejection actions. This is purely for once data needs to be removed once it is safe to do
        ///   so.
        /// </summary>
        private void RemoveEngulfedObject(ref Engulfer engulfer, Entity engulfedEntity, ref Engulfable engulfable)
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
                unusedTransportAnimations.Enqueue(transport);
                engulfable.BulkTransport = null;
            }

            engulfable.PhagocytosisStep = PhagocytosisPhase.None;
            engulfable.HostileEngulfer = default;

            // Thanks to digestion decreasing the size of engulfed objects, this doesn't match what we took in
            // originally. This relies on teh digestion system updating this later to make sure this is correct
            engulfer.UsedIngestionCapacity =
                Math.Max(0, engulfer.UsedIngestionCapacity - engulfable.AdjustedEngulfSize);
        }

        /// <summary>
        ///   Begins phagocytosis related lerp animation. Note that
        ///   <see cref="Engulfable.BulkTransportAnimation.TargetValuesToLerp"/> must be set before calling this.
        /// </summary>
        private void StartBulkTransport(ref Engulfable engulfable, in Entity engulfedObject,
            ref AttachedToEntity initialRelativePositionInfo, float duration,
            Vector3 currentEndosomeScale, bool resetElapsedTime = true)
        {
            var transportData = engulfable.BulkTransport;

            // Only need to recreate the animation data when one doesn't exist, we can reuse existing data in other
            // cases
            if (transportData == null)
            {
                transportData = new Engulfable.BulkTransportAnimation();
                engulfable.BulkTransport = transportData;

                // TODO: this is kind of bad to assume the scale is right like this
                transportData.OriginalScale = Vector3.One;
                GD.PrintErr("New backup engulf animation data was created, this should be avoided " +
                    "(data should be created before bulk transport starts)");
            }

            if (resetElapsedTime)
                transportData.AnimationTimeElapsed = 0;

            Vector3 scale = Vector3.One;

            ref var spatial = ref engulfedObject.Get<SpatialInstance>();

            if (spatial.ApplyVisualScale)
                scale = spatial.VisualScale;

            transportData.InitialValuesToLerp =
                (initialRelativePositionInfo.RelativePosition, scale, currentEndosomeScale);
            transportData.LerpDuration = duration;
            transportData.Interpolate = true;
        }

        /// <summary>
        ///   Stops phagocytosis related lerp animation
        /// </summary>
        private void StopBulkTransport(Engulfable.BulkTransportAnimation animation)
        {
            // This tells the animation to not run anymore
            animation.Interpolate = false;

            animation.AnimationTimeElapsed = 0;
        }

        /// <summary>
        ///   Animates transporting objects from phagocytosis process with linear interpolation.
        /// </summary>
        /// <returns>True when Lerp finishes.</returns>
        private bool AnimateBulkTransport(in Entity entity, ref Engulfable engulfable, in Entity engulfedObject,
            float delta)
        {
            ref var spatial = ref entity.Get<SpatialInstance>();

            if (spatial.GraphicalInstance == null)
            {
                // Can't create phagosome until spatial instance is created. Returning false here will retry the bulk
                // transport animation each update.
                return false;
            }

            var animation = engulfable.BulkTransport;

            if (animation == null)
            {
                // Some code didn't initialize the animation data
                GD.PrintErr($"{nameof(AnimateBulkTransport)} cannot run because bulk animation data is null");
                return true;
            }

            var phagosome = GetEndosomeIfExists(entity, engulfedObject);

            if (phagosome == null)
            {
                // TODO: if state is ejecting then phagosome creation should be skipped to save creating an object that
                // will be deleted in a few frames anyway

                // TODO: render priority calculated properly
                int maxRenderPriority = Constants.HEX_RENDER_PRIORITY_DISTANCE + 1;

                // Form phagosome as it is missing
                phagosome = CreateEndosome(entity, ref spatial, engulfedObject, maxRenderPriority);
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

                if (animation.TargetValuesToLerp.Scale.HasValue)
                {
                    spatial.VisualScale = animation.InitialValuesToLerp.Scale.LinearInterpolate(
                        animation.TargetValuesToLerp.Scale.Value, fraction);
                    spatial.ApplyVisualScale = true;
                }

                if (animation.TargetValuesToLerp.EndosomeScale.HasValue)
                {
                    phagosome.Scale = animation.InitialValuesToLerp.EndosomeScale.LinearInterpolate(
                        animation.TargetValuesToLerp.EndosomeScale.Value, fraction);
                }

                return false;
            }

            // Snap values
            if (animation.TargetValuesToLerp.Translation.HasValue)
                relativePosition.RelativePosition = animation.TargetValuesToLerp.Translation.Value;

            if (animation.TargetValuesToLerp.Scale.HasValue)
            {
                spatial.VisualScale = animation.TargetValuesToLerp.Scale.Value;
                spatial.ApplyVisualScale = true;
            }

            if (animation.TargetValuesToLerp.EndosomeScale.HasValue)
                phagosome.Scale = animation.TargetValuesToLerp.EndosomeScale.Value;

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
                // This can probably happen when the engulfed entity's visual instance has already been destroyed and
                // that resulted in the endosome graphics node to be deleted as it is parented there

                GD.Print("Endosome was already disposed");

                // If caching is added already destroyed endosomes have to be skipped here
                // return;
            }

            // TODO: caching for endosomes
        }

        private void CalculateAdditionalCompoundsInNewlyEngulfedObject(ref Engulfable engulfable,
            in Entity engulfableEntity)
        {
            engulfable.AdditionalEngulfableCompounds =
                engulfable.CalculateAdditionalDigestibleCompounds(engulfableEntity);

            engulfable.InitialTotalEngulfableCompounds = engulfableEntity.Get<CompoundStorage>().Compounds
                .Where(c => c.Key.Digestible)
                .Sum(c => c.Value);

            if (engulfable.AdditionalEngulfableCompounds != null)
            {
                engulfable.InitialTotalEngulfableCompounds +=
                    engulfable.AdditionalEngulfableCompounds.Sum(c => c.Value);
            }
        }
    }
}
