namespace Components
{
    using Godot;

    /// <summary>
    ///   Entity keeps track of damage cooldown
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct DamageCooldown
    {
        public float CooldownRemaining;
    }

    public static class DamageCooldownHelpers
    {
        public static bool IsInCooldown(this ref DamageCooldown damageCooldown)
        {
            return damageCooldown.CooldownRemaining > 0;
        }

        public static void StartCooldown(this ref DamageCooldown damageCooldown, float cooldownTime)
        {
            damageCooldown.CooldownRemaining = cooldownTime;
        }

        /// <summary>
        ///   Starts a cooldown time if <see cref="damage"/> is above <see cref="minCooldownTime"/> and scales the
        ///   cooldown time based on how close the damage is to <see cref="maxDamage"/>
        /// </summary>
        /// <returns>True when cooldown was started</returns>
        public static bool StartDamageScaledCooldown(this ref DamageCooldown damageCooldown, float damage,
            float minDamageToCooldown, float maxDamage, float minCooldownTime, float maxCooldownTime)
        {
            if (damage < minDamageToCooldown)
                return false;

            // Scale the cooldown from the damage range to the cooldown time range
            float cooldown = minCooldownTime + (maxCooldownTime - minCooldownTime) *
                (damage - minDamageToCooldown) / (maxDamage - minDamageToCooldown);

            if (float.IsNaN(cooldown))
            {
                GD.PrintErr("Calculated damage cooldown is NaN");
                return false;
            }

            damageCooldown.StartCooldown(cooldown);
            return true;
        }

        public static void StartInjectisomeCooldown(this ref DamageCooldown damageCooldown)
        {
            damageCooldown.StartCooldown(Constants.INJECTISOME_INVULNERABLE_TIME);
        }
    }
}
