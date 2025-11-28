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
        writer.Write(FadeTime);
        writer.Write(DisableCollisions);
        writer.Write(RemoveVelocity);
        writer.Write(RemoveAngularVelocity);
        writer.Write(DisableParticles);
        writer.Write(UsesMicrobialDissolveEffect);
        writer.Write(VentCompounds);

        // There was probably a bug in the JSON version with it writing CallbackRegistered,
        // even though that is a flag for non-persistent state which needs reapplying after loading
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
            FadeTime = reader.ReadFloat(),
            DisableCollisions = reader.ReadBool(),
            RemoveVelocity = reader.ReadBool(),
            RemoveAngularVelocity = reader.ReadBool(),
            DisableParticles = reader.ReadBool(),
            UsesMicrobialDissolveEffect = reader.ReadBool(),
            VentCompounds = reader.ReadBool(),
        };
    }
}
