namespace Components;

using SharedBase.Archive;
using Newtonsoft.Json;

/// <summary>
///   Has some temporary effects that can affect microbes (like toxins)
/// </summary>
public struct MicrobeTemporaryEffects : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   How long this microbe will have a base movement speed penalty (if 0 or less not currently debuffed).
    ///   Must set <see cref="StateApplied"/> to false after modification.
    /// </summary>
    public float SpeedDebuffDuration;

    /// <summary>
    ///   How long this microbe will have ATP generation debuff
    /// </summary>
    public float ATPDebuffDuration;

    /// <summary>
    ///   False when something needs to be performed. Must be set false when any other fields are modified in this
    ///   struct.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This doesn't absolutely have to be JSON ignored as the debuff states applied are in other components that
    ///     should be saved, but this doesn't do much harm to re-apply temporary effects each time a save is loaded to
    ///     all microbes.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public bool StateApplied;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeTemporaryEffects;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(A PROPERTY);
        writer.WriteObject(A PROPERTY OF COMPLEX TYPE);
    }
}

public static class MicrobeTemporaryEffectsHelpers
{
    public static MicrobeTemporaryEffects ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeTemporaryEffects.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeTemporaryEffects.SERIALIZATION_VERSION);

        return new MicrobeTemporaryEffects
        {
            AProperty = reader.ReadFloat(),
            AnotherProperty = reader.ReadObject<PropertyTypeGoesHere>(),
        };
    }
}
