namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using Newtonsoft.Json;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles starting pulling in <see cref="Engulfable"/> to <see cref="Engulfer"/> entities and also expelling
    ///   things engulfers don't want to eat. Handles the endosome graphics as well.
    /// </summary>
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
    [ReadsComponent(typeof(CellProperties))]
    [ReadsComponent(typeof(SpeciesMember))]
    [ReadsComponent(typeof(MicrobeEventCallbacks))]
    [RunsAfter(typeof(PilusDamageSystem))]
    [RunsAfter(typeof(MicrobeVisualsSystem))]
    [RunsOnMainThread]
    public sealed class EngulfingSystem : AEntitySetSystem<float>
    {
#pragma warning disable CA2213
        private readonly PackedScene endosomeScene;
#pragma warning restore CA2213

        private readonly IWorldSimulation worldSimulation;

        private readonly Compound atp;

        /// <summary>
        ///   Holds temporary data needed by this system to process engulfed objects
        /// </summary>
        private readonly Dictionary<Entity, List<MicrobePhysicsExtraData>> entityEngulfingExtraObjects = new();

        public EngulfingSystem(IWorldSimulation worldSimulation, World world) : base(world, null)
        {
            this.worldSimulation = worldSimulation;
            endosomeScene = GD.Load<PackedScene>("res://src/microbe_stage/Endosome.tscn");

            atp = SimulationParameters.Instance.GetCompound("atp");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var health = ref entity.Get<Health>();

            // Don't process engulfing when dead
            if (health.Dead)
                return;

            ref var control = ref entity.Get<MicrobeControl>();
            ref var engulfer = ref entity.Get<Engulfer>();
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

                engulfer.AttemptingToEngulf?.Clear();
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

            for (int i = engulfer.EngulfedObjects.Count - 1; i >= 0; --i)
            {
                var engulfedObject = engulfer.EngulfedObjects[i];

                if (!engulfedObject.IsAlive || !engulfedObject.Has<Engulfable>())
                {
                    engulfer.AttemptingToEngulf.Remove(engulfedObject);
                    engulfer.EngulfedObjects.Remove(engulfedObject);
                    continue;
                }

                ref var engulfable = ref engulfedObject.Get<Engulfable>();

                if (engulfable.PhagocytosisStep == PhagocytosisPhase.Digested)
                {
                    engulfedObject.TargetValuesToLerp = (null, null, Vector3.One * Mathf.Epsilon);
                    StartBulkTransport(engulfedObject, 1.5f, false);
                }

                if (!engulfedObject.Interpolate)
                    continue;

                if (AnimateBulkTransport(delta, engulfedObject))
                {
                    switch (engulfable.PhagocytosisStep)
                    {
                        case PhagocytosisPhase.Ingestion:
                            CompleteIngestion(engulfedObject);
                            break;
                        case PhagocytosisPhase.Digested:
                            worldSimulation.DestroyEntity(engulfedObject);
                            engulfer.EngulfedObjects.Remove(engulfedObject);
                            break;
                        case PhagocytosisPhase.Exocytosis:
                            engulfedObject.Phagosome.Value?.Hide();
                            engulfedObject.TargetValuesToLerp = (null, engulfedObject.OriginalScale, null);
                            StartBulkTransport(engulfedObject, 1.0f);
                            engulfable.PhagocytosisStep = PhagocytosisPhase.Ejection;
                            continue;
                        case PhagocytosisPhase.Ejection:
                            CompleteEjection(engulfedObject);
                            break;
                    }
                }
            }

            foreach (var expelled in engulfer.ExpelledObjects)
                expelled.TimeElapsedSinceEjection += delta;

            engulfer.ExpelledObjects?.RemoveAll(e =>
            {
                e.TimeElapsedSinceEjection >= Constants.ENGULF_EJECTED_COOLDOWN
            });
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
                CheckStartEngulfingOnCandidate(ref cellProperties, ref engulfer, ref species, in entity,
                    collision.SecondEntity);
            }
        }

        /// <summary>
        ///   This checks if we can start engulfing
        /// </summary>
        private void CheckStartEngulfingOnCandidate(ref CellProperties cellProperties,
            ref Engulfer engulfer, ref SpeciesMember speciesMember, in Entity entity, in Entity engulfable)
        {
            var engulfCheckResult = cellProperties.CanEngulfObject(ref speciesMember, ref engulfer, engulfable);

            if (engulfCheckResult == EngulfCheckResult.Ok)
            {
                // TODO: add some way for this to detect delay added components so that this can't conflict with the
                // binding system
                lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
                {
                    IngestEngulfable(engulfable);
                }
            }
            else if (engulfCheckResult == EngulfCheckResult.IngestedMatterFull)
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
                    ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

                    entity.SendNoticeIfPossible(() =>
                        new SimpleHUDMessage(TranslationServer.Translate("NOTICE_ENGULF_SIZE_TOO_SMALL")));
                }
            }
        }

        /// <summary>
        ///   Attempts to engulf the given target into the cytoplasm. Does not check whether the target
        ///   can be engulfed or not.
        /// </summary>
        private void IngestEngulfable(IEngulfable target, float animationSpeed = 2.0f)
        {
            if (target.PhagocytosisStep != PhagocytosisPhase.None)
                return;

            var body = target as RigidBody;
            if (body == null)
            {
                // Engulfable must be of rigidbody type to be ingested
                return;
            }

            engulfer.AttemptingToEngulf.Add(target);
            touchedEntities.Remove(target);

            target.HostileEngulfer.Value = this;
            target.PhagocytosisStep = PhagocytosisPhase.Ingestion;

            //  TODO: if the other body is already attached or in a colony this needs to handle that correctly

            body.ReParentWithTransform(this);

            // Below is for figuring out where to place the object attempted to be engulfed inside the cytoplasm,
            // calculated accordingly to hopefully minimize any part of the object sticking out the membrane.
            // Note: extremely long and thin objects might still stick out

            var targetRadiusNormalized = Mathf.Clamp(target.Radius / Radius, 0.0f, 1.0f);

            var nearestPointOfMembraneToTarget = Membrane.GetVectorTowardsNearestPointOfMembrane(
                body.Translation.x, body.Translation.z);

            // The point nearest to the membrane calculation doesn't take being bacteria into account
            if (CellTypeProperties.IsBacteria)
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
                body.Translation.y,
                random.Next(0.0f, viableStoringAreaEdge.z));

            var boundingBoxSize = target.EntityGraphics.GetAabb().Size;

            // In the case of flat mesh (like membrane) we don't want the endosome to end up completely flat
            // as it can cause unwanted visual glitch
            if (boundingBoxSize.y < Mathf.Epsilon)
                boundingBoxSize = new Vector3(boundingBoxSize.x, 0.1f, boundingBoxSize.z);

            // Form phagosome
            var phagosome = endosomeScene.Instance<Endosome>();
            phagosome.Transform = target.EntityGraphics.Transform.Scaled(Vector3.Zero);
            phagosome.Tint = CellTypeProperties.Colour;
            phagosome.RenderPriority = target.RenderPriority + engulfedObjects.Count + 1;
            target.EntityGraphics.AddChild(phagosome);

            var engulfedObject = new EngulfedObjectTemporaryState(target, phagosome)
            {
                TargetValuesToLerp = (ingestionPoint, body.Scale / 2, boundingBoxSize),
                OriginalScale = body.Scale,
                OriginalRenderPriority = target.RenderPriority,
                OriginalCollisionLayer = body.CollisionLayer,
                OriginalCollisionMask = body.CollisionMask,
            };

            engulfedObjects.Add(engulfedObject);

            // We want the ingested material to be always visible over the organelles
            target.RenderPriority += OrganelleMaxRenderPriority + 1;

            // Disable collisions
            body.CollisionLayer = 0;
            body.CollisionMask = 0;

            foreach (string group in engulfedObject.OriginalGroups)
            {
                throw new NotImplementedException();

                // if (group != Constants.RUNNABLE_MICROBE_GROUP)
                //     target.EntityNode.RemoveFromGroup(group);
            }

            StartBulkTransport(engulfedObject, animationSpeed);

            target.OnAttemptedToBeEngulfed();
        }

        private void CompleteIngestion(EngulfedObjectTemporaryState engulfed)
        {
            var engulfable = engulfed.Object.Value;
            if (engulfable == null)
                return;

            engulfable.PhagocytosisStep = PhagocytosisPhase.Ingested;

            engulfer.AttemptingToEngulf.Remove(engulfable);
            touchedEntities.Remove(engulfable);

            OnSuccessfulEngulfment?.Invoke(this, engulfable);
            engulfable.OnIngestedFromEngulfment();
        }

        /// <summary>
        ///   Expels an ingested object from this microbe out into the environment.
        /// </summary>
        private void EjectEngulfable(IEngulfable target, float animationSpeed = 2.0f)
        {
            if (PhagocytosisStep != PhagocytosisPhase.None || target.PhagocytosisStep is PhagocytosisPhase.Exocytosis or
                    PhagocytosisPhase.None)
            {
                return;
            }

            engulfer.AttemptingToEngulf.Remove(target);

            var body = target as RigidBody;
            if (body == null)
            {
                // Engulfable must be of rigidbody type to be ejected
                return;
            }

            var engulfedObject = engulfedObjects.Find(e => e.Object == target);
            if (engulfedObject == null)
                return;

            target.PhagocytosisStep = PhagocytosisPhase.Exocytosis;

            // The back of the microbe
            var exit = Hex.AxialToCartesian(new Hex(0, 1));
            var nearestPointOfMembraneToTarget = Membrane.GetVectorTowardsNearestPointOfMembrane(exit.x, exit.z);

            // The point nearest to the membrane calculation doesn't take being bacteria into account
            if (CellTypeProperties.IsBacteria)
                nearestPointOfMembraneToTarget *= 0.5f;

            // If engulfer cell is dead (us) or the engulfed is positioned outside any of our closest membrane, immediately
            // eject it without animation
            // TODO: Asses performance cost in massive cells?
            if (Dead || !Membrane.Contains(body.Translation.x, body.Translation.z))
            {
                CompleteEjection(engulfedObject);
                body.Scale = engulfedObject.OriginalScale;
                engulfedObjects.Remove(engulfedObject);
                return;
            }

            // Animate object move to the nearest point of the membrane
            engulfedObject.TargetValuesToLerp = (nearestPointOfMembraneToTarget, null, Vector3.One * Mathf.Epsilon);
            StartBulkTransport(engulfedObject, animationSpeed);

            // The rest of the operation is done in CompleteEjection
        }

        private void CompleteEjection(EngulfedObjectTemporaryState engulfed)
        {
            var engulfable = engulfed.Object.Value;
            if (engulfable == null)
                return;

            engulfer.AttemptingToEngulf.Remove(engulfable);
            engulfedObjects.Remove(engulfed);
            expelledObjects.Add(engulfed);

            engulfable.PhagocytosisStep = PhagocytosisPhase.None;

            foreach (string group in engulfed.OriginalGroups)
            {
                throw new NotImplementedException();

                // if (group != Constants.RUNNABLE_MICROBE_GROUP)
                //     engulfable.EntityNode.AddToGroup(group);
            }

            // Reset render priority
            engulfable.RenderPriority = engulfed.OriginalRenderPriority;

            engulfed.Phagosome.Value?.DestroyDetachAndQueueFree();

            // Ignore possible invalid cast as the engulfed node should be a rigidbody either way
            var body = (RigidBody)engulfable;

            body.Mode = AudioEffectDistortion.ModeEnum.Rigid;

            // Re-parent to world node
            body.ReParentWithTransform(GetStageAsParent());

            // Reset collision layer and mask
            body.CollisionLayer = engulfed.OriginalCollisionLayer;
            body.CollisionMask = engulfed.OriginalCollisionMask;

            var impulse = Transform.origin.DirectionTo(body.Transform.origin) * body.Mass *
                Constants.ENGULF_EJECTION_FORCE;

            // Apply outwards ejection force
            body.ApplyCentralImpulse(impulse + LinearVelocity);

            // We have our own engulfer and it wants to claim this object we've just expelled
            HostileEngulfer.Value?.IngestEngulfable(engulfable);

            engulfable.OnExpelledFromEngulfment();
            engulfable.HostileEngulfer.Value = null;
        }

        /// <summary>
        ///   Begins phagocytosis related lerp animation
        /// </summary>
        private void StartBulkTransport(EngulfedObjectTemporaryState engulfedObject, float duration,
            bool resetElapsedTime = true)
        {
            if (engulfedObject.Object.Value == null || engulfedObject.Phagosome.Value == null)
                return;

            if (resetElapsedTime)
                engulfedObject.AnimationTimeElapsed = 0;

            var body = (RigidBody)engulfedObject.Object.Value;
            engulfedObject.InitialValuesToLerp = (body.Translation, body.Scale, engulfedObject.Phagosome.Value.Scale);
            engulfedObject.LerpDuration = duration;
            engulfedObject.Interpolate = true;
        }

        /// <summary>
        ///   Stops phagocytosis related lerp animation
        /// </summary>
        private void StopBulkTransport(EngulfedObjectTemporaryState engulfedObject)
        {
            engulfedObject.AnimationTimeElapsed = 0;
            engulfedObject.Interpolate = false;
        }

        /// <summary>
        ///   Animates transporting objects from phagocytosis process with linear interpolation.
        /// </summary>
        /// <returns>True when Lerp finishes.</returns>
        private bool AnimateBulkTransport(float delta, EngulfedObjectTemporaryState engulfed)
        {
            if (engulfed.Object.Value == null || engulfed.Phagosome.Value == null)
                return false;

            var body = (RigidBody)engulfed.Object.Value;

            if (engulfed.AnimationTimeElapsed < engulfed.LerpDuration)
            {
                engulfed.AnimationTimeElapsed += delta;

                var fraction = engulfed.AnimationTimeElapsed / engulfed.LerpDuration;

                // Ease out
                fraction = Mathf.Sin(fraction * Mathf.Pi * 0.5f);

                if (engulfed.TargetValuesToLerp.Translation.HasValue)
                {
                    body.Translation = engulfed.InitialValuesToLerp.Translation.LinearInterpolate(
                        engulfed.TargetValuesToLerp.Translation.Value, fraction);
                }

                if (engulfed.TargetValuesToLerp.Scale.HasValue)
                {
                    body.Scale = engulfed.InitialValuesToLerp.Scale.LinearInterpolate(
                        engulfed.TargetValuesToLerp.Scale.Value, fraction);
                }

                if (engulfed.TargetValuesToLerp.EndosomeScale.HasValue)
                {
                    engulfed.Phagosome.Value.Scale = engulfed.InitialValuesToLerp.EndosomeScale.LinearInterpolate(
                        engulfed.TargetValuesToLerp.EndosomeScale.Value, fraction);
                }

                return false;
            }

            // Snap values
            if (engulfed.TargetValuesToLerp.Translation.HasValue)
                body.Translation = engulfed.TargetValuesToLerp.Translation.Value;

            if (engulfed.TargetValuesToLerp.Scale.HasValue)
                body.Scale = engulfed.TargetValuesToLerp.Scale.Value;

            if (engulfed.TargetValuesToLerp.EndosomeScale.HasValue)
                engulfed.Phagosome.Value.Scale = engulfed.TargetValuesToLerp.EndosomeScale.Value;

            StopBulkTransport(engulfed);

            return true;
        }

        // TODO: use this from somewhere
        private void SetPhagosomeColours()
        {
            foreach (var engulfed in engulfedObjects)
            {
                if (engulfed.Phagosome.Value != null)
                    engulfed.Phagosome.Value.Tint = CellTypeProperties.Colour;
            }
        }

        /// <summary>
        ///   Stores extra information to the objects that have been engulfed. All core data is saved on the entity
        ///   components and this data can be re-created on-demand
        /// </summary>
        private class EngulfedObjectTemporaryState
        {
            public EngulfedObjectTemporaryState(IEngulfable @object, Endosome phagosome)
            {
                Object = new EntityReference<IEngulfable>(@object);
                Phagosome = new EntityReference<Endosome>(phagosome);

                AdditionalEngulfableCompounds = @object.CalculateAdditionalDigestibleCompounds()?
                    .Where(c => c.Key.Digestible)
                    .ToDictionary(c => c.Key, c => c.Value);

                InitialTotalEngulfableCompounds = @object.Compounds.Compounds
                    .Where(c => c.Key.Digestible)
                    .Sum(c => c.Value);

                if (AdditionalEngulfableCompounds != null)
                    InitialTotalEngulfableCompounds += AdditionalEngulfableCompounds.Sum(c => c.Value);
            }

            /// <summary>
            ///   The solid matter that has been engulfed.
            /// </summary>
            public EntityReference<IEngulfable> Object { get; private set; }

            /// <summary>
            ///   A food vacuole containing the engulfed object. Only decorative.
            /// </summary>
            public EntityReference<Endosome> Phagosome { get; private set; }

            [JsonProperty]
            public Dictionary<Compound, float>? AdditionalEngulfableCompounds { get; private set; }

            [JsonProperty]
            public float? InitialTotalEngulfableCompounds { get; private set; }

            [JsonProperty]
            public Array OriginalGroups { get; private set; } = new();

            public bool Interpolate { get; set; }
            public float LerpDuration { get; set; }
            public float AnimationTimeElapsed { get; set; }
            public float TimeElapsedSinceEjection { get; set; }
            public (Vector3? Translation, Vector3? Scale, Vector3? EndosomeScale) TargetValuesToLerp { get; set; }
            public (Vector3 Translation, Vector3 Scale, Vector3 EndosomeScale) InitialValuesToLerp { get; set; }
            public Vector3 OriginalScale { get; set; }
            public int OriginalRenderPriority { get; set; }

            // These values (default microbe collision layer & mask) are here for save compatibility
            public uint OriginalCollisionLayer { get; set; } = 3;
            public uint OriginalCollisionMask { get; set; } = 3;
        }
    }
}
