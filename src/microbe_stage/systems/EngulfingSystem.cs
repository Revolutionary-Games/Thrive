namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles starting pulling in <see cref="Engulfable"/> to <see cref="Engulfer"/> entities and also expelling
    ///   things engulfers don't want to eat. Handles the endosome graphics as well.
    /// </summary>
    [With(typeof(Engulfer))]
    [With(typeof(CollisionManagement))]
    [With(typeof(MicrobeControl))]
    [RunsAfter(typeof(PilusDamageSystem))]
    public sealed class EngulfingSystem : AEntitySetSystem<float>
    {
#pragma warning disable CA2213
        private readonly PackedScene endosomeScene;
#pragma warning restore CA2213

        public EngulfingSystem(World world) : base(world, null)
        {
            endosomeScene = GD.Load<PackedScene>("res://src/microbe_stage/Endosome.tscn");
        }

        protected override void Update(float delta, in Entity entity)
        {
            var actuallyEngulfing = State == MicrobeState.Engulf && CanEngulf;

            if (actuallyEngulfing)
            {
                // Drain atp
                var cost = Constants.ENGULFING_ATP_COST_PER_SECOND * delta;

                if (Compounds.TakeCompound(atp, cost) < cost - 0.001f || PhagocytosisStep != PhagocytosisPhase.None)
                {
                    State = MicrobeState.Normal;
                }
            }
            else
            {
                attemptingToEngulf.Clear();
            }

            // Play sound
            if (actuallyEngulfing)
            {
                if (!engulfAudio.Playing)
                    engulfAudio.Play();

                // To balance loudness, here the engulfment audio's max volume is reduced to 0.6 in linear volume

                if (engulfAudio.Volume < 0.6f)
                {
                    engulfAudio.Volume += delta;
                }
                else if (engulfAudio.Volume >= 0.6f)
                {
                    engulfAudio.Volume = 0.6f;
                }
            }
            else
            {
                if (engulfAudio.Playing && engulfAudio.Volume > 0)
                {
                    engulfAudio.Volume -= delta;

                    if (engulfAudio.Volume <= 0)
                        engulfAudio.Stop();
                }
            }

            // Movement modifier
            if (actuallyEngulfing)
            {
                MovementFactor /= Constants.ENGULFING_MOVEMENT_DIVISION;
            }

            // Still considered to be chased for CREATURE_ESCAPE_INTERVAL milliseconds
            if (hasEscaped)
            {
                escapeInterval += delta;
                if (escapeInterval >= Constants.CREATURE_ESCAPE_INTERVAL)
                {
                    hasEscaped = false;
                    escapeInterval = 0;

                    GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
                        Constants.CREATURE_ESCAPE_POPULATION_GAIN,
                        TranslationServer.Translate("ESCAPE_ENGULFING"));
                }
            }

            for (int i = engulfedObjects.Count - 1; i >= 0; --i)
            {
                var engulfedObject = engulfedObjects[i];

                var engulfable = engulfedObject.Object.Value;

                // ReSharper disable once UseNullPropagation
                if (engulfable == null)
                    continue;

                var body = engulfable as RigidBody;
                if (body == null)
                {
                    attemptingToEngulf.Remove(engulfable);
                    engulfedObjects.Remove(engulfedObject);
                    continue;
                }

                body.Mode = ModeEnum.Static;

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
                            engulfable.DestroyAndQueueFree();
                            engulfedObjects.Remove(engulfedObject);
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

            foreach (var expelled in expelledObjects)
                expelled.TimeElapsedSinceEjection += delta;

            expelledObjects.RemoveAll(e => e.TimeElapsedSinceEjection >= Constants.ENGULF_EJECTED_COOLDOWN);
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
    }
}
