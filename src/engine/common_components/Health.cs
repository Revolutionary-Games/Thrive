namespace Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   Things that have a health and can be damaged
    /// </summary>
    public struct Health
    {
        public List<DamageEventNotice>? RecentDamageReceived;

        public float CurrentHealth;
        public float MaxHealth;

        // TODO: an invulnerability duration to automatically turn of invulnerability? (needed to reimplement pilus
        // damage cooldown)

        public bool Invulnerable;

        /// <summary>
        ///   Simple flag to check if this entity has died. A stage specific death system will set this flag when
        ///   an entity runs out of health (or some other condition is fulfilled for death)
        /// </summary>
        public bool Dead;

        /// <summary>
        ///   This health class is stage agnostic, so each stage needs its own entity death system to handle dying.
        ///   To at least make that easier this flag exists for such a system to store the info on if it has already
        ///   handled a dead entity or not.
        /// </summary>
        public bool DeathProcessed;

        public Health(float defaultHealth)
        {
            CurrentHealth = defaultHealth;
            MaxHealth = defaultHealth;
            Invulnerable = false;
            Dead = false;
            DeathProcessed = false;
            RecentDamageReceived = null;
        }
    }

    public static class HealthHelpers
    {
        public static void DealDamage(ref this Health health, float damage, string damageSource)
        {
            if (health.Invulnerable)
            {
                // Consume this damage event if the target is not taking damage
                return;
            }

            // This should result in at least reasonable health even if thread race conditions hit here
            health.CurrentHealth = Math.Max(0, health.CurrentHealth - damage);

            var damageEvent = new DamageEventNotice(damageSource, damage);
            var damageList = health.RecentDamageReceived;

            if (damageList == null)
            {
                // Create new damage list, don't really care if due to data race some info is lost here so we don't
                // immediately set the list here and lock it
                damageList = new List<DamageEventNotice> { damageEvent };

                health.RecentDamageReceived = damageList;
            }
            else
            {
                lock (damageList)
                {
                    damageList.Add(damageEvent);
                }
            }
        }
    }

    /// <summary>
    ///   Notice to an entity that it took damage. Used for example to play sounds or other feedback about taking
    ///   damage
    /// </summary>
    public class DamageEventNotice
    {
        public string DamageSource;
        public float Amount;

        public DamageEventNotice(string damageSource, float amount)
        {
            DamageSource = damageSource;
            Amount = amount;
        }
    }
}
