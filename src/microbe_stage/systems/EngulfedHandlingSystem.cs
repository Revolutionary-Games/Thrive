namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles <see cref="Engulfable"/> entities that are currently engulfed or have been engulfed before and should
///   heal
/// </summary>
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
public partial class EngulfedHandlingSystem : BaseSystem<World, float>
{
    private readonly IWorldSimulation worldSimulation;
    private readonly ISpawnSystem spawnSystem;

    // TODO: these two might be good to save to save the player some extra waiting time when loading an unluckily
    // timed save
    private float playerEngulfedDeathTimer;
    private float previousPlayerEngulfedDeathTimer;

    public EngulfedHandlingSystem(IWorldSimulation worldSimulation, ISpawnSystem spawnSystem, World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
        this.spawnSystem = spawnSystem;
    }

    public override void BeforeUpdate(in float delta)
    {
        previousPlayerEngulfedDeathTimer = playerEngulfedDeathTimer;
    }

    public override void AfterUpdate(in float delta)
    {
        // If there's no player digestion progress, reset the timer
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (previousPlayerEngulfedDeathTimer == playerEngulfedDeathTimer)
        {
            // Just in case the player is engulfed again after escaping to make sure the player doesn't die faster
            playerEngulfedDeathTimer = 0;
        }
    }

    [Query]
    [All<Health, Engulfer, SoundEffectPlayer>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref Engulfable engulfable, ref MicrobeControl control, in Entity entity)
    {
        // Handle logic if the cell that's being/has been digested is us
        if (engulfable.PhagocytosisStep == PhagocytosisPhase.None)
        {
            if (engulfable.DigestedAmount >= 1 || (engulfable.DestroyIfPartiallyDigested &&
                    engulfable.DigestedAmount >= Constants.PARTIALLY_DIGESTED_THRESHOLD))
            {
                // Too digested to live any more
                // Note that the microbe equivalent of this is handled in OnExpelledFromEngulfment
                ref var health = ref entity.Get<Health>();
                KillEngulfed(entity, ref health, ref engulfable);
            }
            else if (engulfable.DigestedAmount > 0)
            {
                // Cell is not too damaged, can heal itself in an open environment and continue living
                engulfable.DigestedAmount -= delta * Constants.ENGULF_COMPOUND_ABSORBING_PER_SECOND;

                if (engulfable.DigestedAmount < 0)
                    engulfable.DigestedAmount = 0;
            }
        }
        else
        {
            // Disallow cells being in any state than normal while engulfed
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
            // an on entity destroy callback that should do this, so things are going pretty wrong if this is
            // triggered
            if (!engulfable.HostileEngulfer.IsAlive())
            {
                GD.PrintErr("Entity is stuck inside a dead engulfer, force clearing state to rescue it");

                engulfable.OnExpelledFromEngulfment(entity, spawnSystem, worldSimulation);
                engulfable.PhagocytosisStep = PhagocytosisPhase.None;
                engulfable.HostileEngulfer = Entity.Null;

                var recorder = worldSimulation.StartRecordingEntityCommands();
                recorder.Remove<AttachedToEntity>(entity);

                worldSimulation.FinishRecordingEntityCommands(recorder);
            }
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
        if (!hostile.IsAliveAndHas<Engulfer>())
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
