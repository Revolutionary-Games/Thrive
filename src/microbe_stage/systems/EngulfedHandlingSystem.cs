namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles <see cref="Engulfable"/> entities that are currently engulfed or have been engulfed before and should
    ///   heal
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: implement safety against being engulfed by a dead entity at which point the engulfable should be
    ///     automatically freed from engulfment
    ///   </para>
    /// </remarks>
    [With(typeof(Engulfable))]
    [With(typeof(Engulfer))]
    [With(typeof(Health))]
    [With(typeof(SoundEffectPlayer))]
    [RunsAfter(typeof(EngulfedDigestionSystem))]
    public sealed class EngulfedHandlingSystem : AEntitySetSystem<float>
    {
        private float playerEngulfedDeathTimer;
        private float previousPlayerEngulfedDeathTimer;

        public EngulfedHandlingSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
        }

        protected override void PreUpdate(float delta)
        {
            base.PreUpdate(delta);

            previousPlayerEngulfedDeathTimer = playerEngulfedDeathTimer;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var engulfable = ref entity.Get<Engulfable>();

            // Handle logic if the cell that's being/has been digested is us
            if (engulfable.PhagocytosisStep == PhagocytosisPhase.None)
            {
                if (engulfable.DigestedAmount >= 1 || (engulfable.DestroyIfPartiallyDigested &&
                        engulfable.DigestedAmount >= Constants.PARTIALLY_DIGESTED_THRESHOLD))
                {
                    // Too digested to live anymore
                    // Note that the microbe equivalent of this is handled in OnExpelledFromEngulfment
                    ref var health = ref entity.Get<Health>();
                    KillEngulfed(entity, ref health, ref engulfable);
                }
                else if (engulfable.DigestedAmount > 0)
                {
                    // Cell is not too damaged, can heal itself in open environment and continue living
                    engulfable.DigestedAmount -= delta * Constants.ENGULF_COMPOUND_ABSORBING_PER_SECOND;
                }
            }
            else
            {
                // TODO: it seems that this code is always ran, though with the PARTIALLY_DIGESTED_THRESHOLD check
                // maybe this shouldn't always run?

                // Species handling for the player microbe in case the process into partial digestion took too long
                // so here we want to limit how long the player should wait until they respawn
                if (engulfable.PhagocytosisStep == PhagocytosisPhase.Ingested && entity.Has<PlayerMarker>())
                    playerEngulfedDeathTimer += delta;

                // TODO: the old system probably used to have:
                // engulfable.DigestedAmount >= Constants.PARTIALLY_DIGESTED_THRESHOLD here which is now gone to stop
                // things being destroyed too soon when being digested
                if (playerEngulfedDeathTimer >= Constants.PLAYER_ENGULFED_DEATH_DELAY_MAX && entity.Has<PlayerMarker>())
                {
                    // Microbe is beyond repair, might as well consider it as dead
                    ref var health = ref entity.Get<Health>();

                    if (!health.Dead)
                        KillEngulfed(entity, ref health, ref engulfable);
                }

                // If the engulfing entity is dead, then this should have been ejected
                // See the TODO in the remarks section
                if (!engulfable.HostileEngulfer.IsAlive)
                {
                    GD.PrintErr("Entity is stuck inside a dead engulfer!");

#if DEBUG

                    // Disabled for now as the likely root cause of this is the spawn system despawning an entity
                    // so a system needs to be updated to remove engulfables from inside dead engulfers
                    // throw new InvalidOperationException("Entity is inside a dead engulfer (not ejected)");
#endif
                }
            }
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

            // If there's no player digestion progress reset the timer
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (previousPlayerEngulfedDeathTimer == playerEngulfedDeathTimer)
            {
                // Just in case player is engulfed again after escaping to make sure the player doesn't die faster
                playerEngulfedDeathTimer = 0;
            }
        }

        private void KillEngulfed(Entity entity, ref Health health, ref Engulfable engulfable)
        {
            health.Kill();

            if (entity.Has<PlayerMarker>())
            {
                playerEngulfedDeathTimer = 0;

                ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

                soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-death-2.ogg", 0.5f);
            }

            var hostile = engulfable.HostileEngulfer;
            if (!hostile.IsAlive || !hostile.Has<Engulfer>())
                return;

            ref var engulfer = ref entity.Get<Engulfer>();

            if (engulfer.EngulfedObjects is not { Count: > 0 })
            {
                // We haven't engulfed anything
                return;
            }

            ref var hostileEngulfer = ref hostile.Get<Engulfer>();

            // Transfer ownership of all the objects we engulfed to our engulfer (otherwise we'd spill them out
            // when we are processed as dead)
            engulfer.TransferEngulferObjectsToAnotherEngulfer(ref hostileEngulfer, hostile);
        }
    }
}
