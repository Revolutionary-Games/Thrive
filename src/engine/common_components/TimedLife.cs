namespace Components;

using Arch.Core;
using SharedBase.Archive;

/// <summary>
///   Entities that despawn after a certain amount of time
/// </summary>
public struct TimedLife(float timeToLiveRemaining) : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Custom callback to be triggered when the timed life is over. If this returns false, then the entity won't
    ///   be automatically destroyed. If this callback sets <see cref="FadeTimeRemaining"/> then this also won't
    ///   be automatically destroyed.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is save-ignored with the intention that any systems that use the time over callback will
    ///     re-apply it after the save is loaded.
    ///   </para>
    /// </remarks>
    public OnTimeOver? CustomTimeOverCallback = null;

    public float TimeToLiveRemaining = timeToLiveRemaining;

    /// <summary>
    ///   When <see cref="FadeTimeRemainingSet"/> is true, this entity is fading out and the timed despawn system
    ///   will wait until this time is up as well
    /// </summary>
    public float FadeTimeRemaining = -1;

    public bool FadeTimeRemainingSet = false;

    public bool OnTimeOverTriggered = false;

    /// <summary>
    ///   Pre-stored fade time from <see cref="FadeOutActions"/>, set when the callback is registered.
    ///   Not serialized (transient state re-applied after load, like <see cref="CustomTimeOverCallback"/>).
    /// </summary>
    public float PreStoredFadeTime = -1;

    public delegate bool OnTimeOver(Entity entity, ref TimedLife timedLife);

    public readonly ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public readonly ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentTimedLife;

    public readonly void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(TimeToLiveRemaining);
        writer.Write(FadeTimeRemaining);
        writer.Write(FadeTimeRemainingSet);
        writer.Write(OnTimeOverTriggered);
    }
}

public static class TimedLifeHelpers
{
    public static TimedLife ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > TimedLife.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, TimedLife.SERIALIZATION_VERSION);

        return new TimedLife
        {
            TimeToLiveRemaining = reader.ReadFloat(),
            FadeTimeRemaining = reader.ReadFloat(),
            FadeTimeRemainingSet = reader.ReadBool(),
            OnTimeOverTriggered = reader.ReadBool(),
        };
    }
}
