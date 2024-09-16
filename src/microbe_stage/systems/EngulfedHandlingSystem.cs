namespace Systems;

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
[With(typeof(Engulfable))]
[With(typeof(Engulfer))]
[With(typeof(Health))]
[With(typeof(SoundEffectPlayer))]
[With(typeof(MicrobeControl))]
[WritesToComponent(typeof(CompoundAbsorber))]
[WritesToComponent(typeof(UnneededCompoundVenter))]
[WritesToComponent(typeof(RenderPriorityOverride))]
[WritesToComponent(typeof(SpatialInstance))]
[WritesToComponent(typeof(CompoundStorage))]
[WritesToComponent(typeof(CellProperties))]
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(EngulfingSystem))]
[RunsAfter(typeof(EngulfedDigestionSystem))]
[RuntimeCost(0.5f)]
public sealed class EngulfedHandlingSystem : AEntitySetSystem<float>
{
    private readonly IWorldSimulation worldSimulation;
    private readonly ISpawnSystem spawnSystem;

    // TODO: these two might be good to save to save the player some extra waiting time when loading an unluckily
    // timed save
    private float playerEngulfedDeathTimer;
    private float previousPlayerEngulfedDeathTimer;

    public EngulfedHandlingSystem(IWorldSimulation worldSimulation, ISpawnSystem spawnSystem, World world,
        IParallelRunner parallelRunner) : base(world, parallelRunner)
    {
        this.worldSimulation = worldSimulation;
        this.spawnSystem = spawnSystem;
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

                if (engulfable.DigestedAmount < 0)
                    engulfable.DigestedAmount = 0;
            }
        }
        else
        {
            // Disallow cells being in any state than normal while engulfed
            ref var control = ref entity.Get<MicrobeControl>();
            control.State = MicrobeState.Normal;

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

            // If the engulfing entity is dead, then this should have been ejected. The simulation world also has
            // an on entity destroy callback that should do this so things are going pretty wrong if this is
            // triggered
            if (!engulfable.HostileEngulfer.IsAlive)
            {
                GD.PrintErr("Entity is stuck inside a dead engulfer, force clearing state to rescue it");

                engulfable.OnExpelledFromEngulfment(entity, spawnSystem, worldSimulation);
                engulfable.PhagocytosisStep = PhagocytosisPhase.None;
                engulfable.HostileEngulfer = default;

                var recorder = worldSimulation.StartRecordingEntityCommands();

                var entityRecord = recorder.Record(entity);
                entityRecord.Remove<AttachedToEntity>();

                worldSimulation.FinishRecordingEntityCommands(recorder);
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
