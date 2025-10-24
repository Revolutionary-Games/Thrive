namespace Components;

using Arch.Core;
using SharedBase.Archive;

/// <summary>
///   Entities that despawn after a certain amount of time
/// </summary>
public struct TimedLife : IArchivableComponent
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
    public OnTimeOver? CustomTimeOverCallback;

    public float TimeToLiveRemaining;

    /// <summary>
    ///   When <see cref="FadeTimeRemainingSet"/> is true, this entity is fading out and the timed despawn system
    ///   will wait until this time is up as well
    /// </summary>
    public float FadeTimeRemaining;

    public bool FadeTimeRemainingSet;

    public bool OnTimeOverTriggered;

    public TimedLife(float timeToLiveRemaining)
    {
        CustomTimeOverCallback = null;
        TimeToLiveRemaining = timeToLiveRemaining;

        FadeTimeRemainingSet = false;
        FadeTimeRemaining = -1;
        OnTimeOverTriggered = false;
    }

    public delegate bool OnTimeOver(Entity entity, ref TimedLife timedLife);

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentTimedLife;

    public void WriteToArchive(ISArchiveWriter writer)
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
