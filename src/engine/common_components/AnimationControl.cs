namespace Components;

using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Allows controlling <see cref="Godot.AnimationPlayer"/> in a <see cref="SpatialInstance"/> (note that if
///   spatial is recreated <see cref="AnimationApplied"/> needs to be set to false for the animation to reapply)
/// </summary>
public struct AnimationControl : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    // TODO: add speed / animation to play fields to make this generally useful

    /// <summary>
    ///   If not null will try to find the animation player to control based on this path starting from the
    ///   graphics instance of this entity
    /// </summary>
    public string? AnimationPlayerPath;

    /// <summary>
    ///   If set to true, all animations are stopped
    /// </summary>
    public bool StopPlaying;

    /// <summary>
    ///   Set to false when any properties change in this component to re-apply them
    /// </summary>
    [JsonIgnore]
    public bool AnimationApplied;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentAnimationControl;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(A PROPERTY);
        writer.WriteObject(A PROPERTY OF COMPLEX TYPE);
    }
}

public static class AnimationControlHelpers
{
    public static AnimationControl ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > AnimationControl.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, AnimationControl.SERIALIZATION_VERSION);

        return new AnimationControl
        {
            AProperty = reader.ReadFloat(),
            AnotherProperty = reader.ReadObject<PropertyTypeGoesHere>(),
        };
    }
}
