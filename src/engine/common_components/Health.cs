namespace Components
{
    using System;
    using System.Collections.Generic;
    using Godot;

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
            MaxHealth = defaultHealth;
            CurrentHealth = MaxHealth;

            Invulnerable = false;
            Dead = false;
            DeathProcessed = false;
            RecentDamageReceived = null;
        }
    }

    public static class HealthHelpers
    {
        public static float CalculateMicrobeHealth(MembraneType membraneType, float membraneRigidity)
        {
            return membraneType.Hitpoints +
                (membraneRigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER);
        }

        /// <summary>
        ///   A general damage dealing method that doesn't apply any damage reductions or anything like that
        /// </summary>
        public static void DealDamage(this ref Health health, float damage, string damageSource)
        {
            if (health.Invulnerable)
            {
                // Just consume this damage event if the target is not taking damage
                return;
            }

            if (string.IsNullOrEmpty(damageSource))
                throw new ArgumentException("damage source is empty");

            // This is probably no longer needed, but just for safety this makes sure no negative damage is applied
            if (damage < 0)
            {
                GD.PrintErr("Trying to deal negative damage");
                return;
            }

            // Can't damage dead things (or deal no damage)
            if (health.Dead || damage == 0)
                return;

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
                    // Skip tracking damage after max number of events
                    if (damageList.Count >= Constants.MAX_DAMAGE_EVENTS)
                        return;

                    damageList.Add(damageEvent);

                    if (damageList.Count == Constants.MAX_DAMAGE_EVENTS)
                    {
                        // Print an error once per entity
                        GD.PrintErr("Damage event overflow for an entity, all entities should always have " +
                            "a system clearing their damage events");
                    }
                }
            }

            // TODO: probably need a separate system to trigger this
            // ModLoader.ModInterface.TriggerOnDamageReceived(this, amount, IsPlayerMicrobe);
        }

        /// <summary>
        ///   Applies damage but takes microbe damage resistances into account. This should be (almost always) be used
        ///   for microbes to calculate the right damage rather than <see cref="DealDamage"/>
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     TODO: would it be cleaner design to bake in resistances / a damage callback into the base Health type
        ///     so that no more entity type specific methods like this would be needed?
        ///   </para>
        /// </remarks>
        public static void DealMicrobeDamage(this ref Health health, ref CellProperties cellProperties, float damage,
            string damageSource)
        {
            // TODO: reimplement this (probably better to use the invulnerable health property and also make engulf
            // check that to prevent engulfing of the player)
            // if (IsPlayerMicrobe && CheatManager.GodMode)
            //    return;

            // Damage reduction is only wanted for non-starving damage
            bool canApplyDamageReduction = true;

            if (damageSource is "toxin" or "oxytoxy" or "injectisome")
            {
                // Divide damage by toxin resistance
                damage /= cellProperties.MembraneType.ToxinResistance;
            }
            else if (damageSource is "pilus" or "chunk" or "ice")
            {
                // Divide damage by physical resistance
                damage /= cellProperties.MembraneType.PhysicalResistance;
            }
            else if (damageSource == "atpDamage")
            {
                canApplyDamageReduction = false;
            }

            if (!cellProperties.IsBacteria && canApplyDamageReduction)
            {
                damage /= 2;
            }

            health.DealDamage(damage, damageSource);
        }

        /// <summary>
        ///   Immediately kills this entity
        /// </summary>
        /// <param name="health">The health to mark dead</param>
        /// <param name="goesThroughInvulnerability">If true also kills invulnerable entities</param>
        public static void Kill(this ref Health health, bool goesThroughInvulnerability = true)
        {
            if (health.Invulnerable && !goesThroughInvulnerability)
                return;

            health.CurrentHealth = 0;
            health.Invulnerable = false;
        }

        /// <summary>
        ///   Modifies the max health and rescales remaining health percentage to be the same with the new value than
        ///   it currently is
        /// </summary>
        /// <param name="health">Health to update max health for</param>
        /// <param name="newMaxHealth">New max health value to set</param>
        public static void RescaleMaxHealth(this ref Health health, float newMaxHealth)
        {
            // Safety check against bad data
            if (newMaxHealth <= 0)
                newMaxHealth = 1;

            float currentFraction = health.CurrentHealth / health.MaxHealth;

            health.MaxHealth = health.CurrentHealth;

            health.CurrentHealth = health.MaxHealth * currentFraction;
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
