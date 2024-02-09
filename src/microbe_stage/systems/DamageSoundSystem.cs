namespace Systems
{
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
        public DamageSoundSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
        }

        protected override void Update(float state, in Entity entity)
        {
            ref var health = ref entity.Get<Health>();

            var receivedDamage = health.RecentDamageReceived;

            // We don't lock here before checking the count, it's probably fine as it should just read a single int
            // but in the future if we get random crashes add a "lock" statement around also the count access.
            if (receivedDamage == null || receivedDamage.Count < 1)
                return;

            ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

            lock (receivedDamage)
            {
                foreach (var damageEventNotice in receivedDamage)
                {
                    var damageSource = damageEventNotice.DamageSource;

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
                            0.5f);
                    }
                }

                receivedDamage.Clear();
            }
        }
    }
}
