namespace Components
{
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

        public static void StartInjectisomeCooldown(this ref DamageCooldown damageCooldown)
        {
            damageCooldown.StartCooldown(Constants.PILUS_INVULNERABLE_TIME);
        }
    }
}
