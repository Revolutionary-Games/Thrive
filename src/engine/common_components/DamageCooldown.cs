namespace Components;

using Godot;
using SharedBase.Archive;

/// <summary>
///   Entity keeps track of damage cooldown
/// </summary>
public struct DamageCooldown : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float CooldownRemaining;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentDamageCooldown;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(CooldownRemaining);
    }
}

public static class DamageCooldownHelpers
{
    public static DamageCooldown ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > DamageCooldown.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, DamageCooldown.SERIALIZATION_VERSION);

        return new DamageCooldown
        {
            CooldownRemaining = reader.ReadFloat(),
        };
    }

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
