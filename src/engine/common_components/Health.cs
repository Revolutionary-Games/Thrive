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

        public bool Invulnerable;
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
