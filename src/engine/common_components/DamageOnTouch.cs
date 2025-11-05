namespace Components;

using SharedBase.Archive;

/// <summary>
///   Damages any entities touched by this entity. Requires <see cref="CollisionManagement"/>
/// </summary>
public struct DamageOnTouch : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   The name of the caused damage type this deals
    /// </summary>
    public string DamageType;

    /// <summary>
    ///   The amount of damage this causes. This is allowed to be 0 to implement entities that just get destroyed
    ///   on touch. When <see cref="DestroyOnTouch"/> is true this is the inflicted damage, otherwise this is the
    ///   damage per second.
    /// </summary>
    public float DamageAmount;

    /// <summary>
    ///   If true, then this is destroyed when this collides with something this could deal damage to
    /// </summary>
    public bool DestroyOnTouch;

    /// <summary>
    ///   Uses a microbe stage dissolve effect on the visuals when being destroyed
    /// </summary>
    public bool UsesMicrobialDissolveEffect;

    /// <summary>
    ///   Internal variable, don't modify
    /// </summary>
    public bool StartedDestroy;

    /// <summary>
    ///   Internal variable, don't modify
    /// </summary>
    public bool RegisteredWithCollisions;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentDamageOnTouch;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(DamageType);
        writer.Write(DamageAmount);
        writer.Write(DestroyOnTouch);
        writer.Write(UsesMicrobialDissolveEffect);
    }
}

public static class DamageOnTouchHelpers
{
    public static DamageOnTouch ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > DamageOnTouch.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, DamageOnTouch.SERIALIZATION_VERSION);

        return new DamageOnTouch
        {
            DamageType = reader.ReadString()!,
            DamageAmount = reader.ReadFloat(),
            DestroyOnTouch = reader.ReadBool(),
            UsesMicrobialDissolveEffect = reader.ReadBool(),
        };
    }
}
