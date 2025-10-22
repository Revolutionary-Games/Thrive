namespace Components;

using SharedBase.Archive;

/// <summary>
///   Special actions to perform on time to live expiring and fading out
/// </summary>
public struct FadeOutActions : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float FadeTime;

    public bool DisableCollisions;
    public bool RemoveVelocity;
    public bool RemoveAngularVelocity;

    /// <summary>
    ///   Disables a particle emitter if there is one on the entity spatial root or the first child. Will print an
    ///   error if missing.
    /// </summary>
    public bool DisableParticles;

    public bool UsesMicrobialDissolveEffect;

    /// <summary>
    ///   If true then <see cref="CompoundStorage"/> is emptied on fade out
    /// </summary>
    public bool VentCompounds;

    /// <summary>
    ///   Internal variable for use by the managing system
    /// </summary>
    public bool CallbackRegistered;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentFadeOutActions;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(A PROPERTY);
        writer.WriteObject(A PROPERTY OF COMPLEX TYPE);
    }
}

public static class FadeOutActionsHelpers
{
    public static FadeOutActions ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > FadeOutActions.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, FadeOutActions.SERIALIZATION_VERSION);

        return new FadeOutActions
        {
            AProperty = reader.ReadFloat(),
            AnotherProperty = reader.ReadObject<PropertyTypeGoesHere>(),
        };
    }
}
