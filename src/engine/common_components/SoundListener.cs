namespace Components;

using SharedBase.Archive;

/// <summary>
///   Places a <see cref="Godot.AudioListener3D"/> at this entity. Requires a <see cref="WorldPosition"/> to function.
/// </summary>
public struct SoundListener : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   When set to true, sound is set to come from the side of the screen relative to the
    ///   camera rather than using the entity's rotation.
    /// </summary>
    public bool UseTopDownRotation;

    public bool Disabled;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSoundListener;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(UseTopDownRotation);
        writer.Write(Disabled);
    }
}

public static class SoundListenerHelpers
{
    public static SoundListener ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SoundListener.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SoundListener.SERIALIZATION_VERSION);

        return new SoundListener
        {
            UseTopDownRotation = reader.ReadBool(),
            Disabled = reader.ReadBool(),
        };
    }
}
