namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   Handles playing microbe damage sounds and clearing the list of received damage on a microbe
/// </summary>
[With(typeof(Health))]
[With(typeof(SoundEffectPlayer))]
[WritesToComponent(typeof(SoundEffectPlayer))]
[RunsBefore(typeof(SoundEffectSystem))]
[RuntimeCost(0.5f)]
public sealed class DamageSoundSystem : AEntitySetSystem<float>
{
    private GameWorld? gameWorld;

    public DamageSoundSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
    {
    }

    public void SetWorld(GameWorld world)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        gameWorld = world;
    }

    protected override void PreUpdate(float state)
    {
        base.PreUpdate(state);

        if (gameWorld == null)
            throw new InvalidOperationException("GameWorld not set");
    }

    protected override void Update(float state, in Entity entity)
    {
        ref var health = ref entity.Get<Health>();

        var receivedDamage = health.RecentDamageReceived;

        // We don't lock here before checking the count, it's probably fine as it should just read a single int,
        // but in the future if we get random crashes, add a "lock" statement around also the count access.
        if (receivedDamage == null || receivedDamage.Count < 1)
            return;

        ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

        bool isPlayer = entity.Has<PlayerMarker>();

        lock (receivedDamage)
        {
            foreach (var damageEventNotice in receivedDamage)
            {
                var damageSource = damageEventNotice.DamageSource;

                // This is probably the best place to track damage by source so that not all damage sources have to
                // handle this separately
                if (isPlayer)
                {
                    // This doesn't use locking as there should only ever be a single player entity
                    gameWorld!.StatisticsTracker.PlayerReceivedDamage.IncrementDamage(damageSource,
                        damageEventNotice.Amount);
                }

                // TODO: different injectisome sound effect?
                if (damageSource is "toxin" or "oxytoxy" or "injectisome")
                {
                    // Play the toxin sound
                    soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-toxin-damage.ogg");
                }
                else if (damageSource == "pilus")
                {
                    // Play the pilus sound
                    soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/pilus_puncture_stab.ogg",
                        4.0f);
                }
                else if (damageSource == "chunk")
                {
                    // TODO: Replace this take damage sound with a more appropriate one.

                    soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-toxin-damage.ogg");
                }
                else if (damageSource == "atpDamage")
                {
                    soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-atp-damage.ogg");
                }
                else if (damageSource == "ice")
                {
                    // TODO: check the volume here as this was before set to play non-positionally
                    soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-ice-damage.ogg",
                        0.8f);
                }
                else if (damageSource == "radiation")
                {
                    // Doesn't make a ton of sense if other cells play Geiger-counter sounds...
                    if (isPlayer)
                    {
                        soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/radiation.ogg", 28.0f);
                    }
                }
            }

            receivedDamage.Clear();
        }
    }
}
