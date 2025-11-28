namespace Components;

using Arch.Core;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Sends and receivers command signals (signaling agent). Requires a <see cref="WorldPosition"/> to function
///   as the origin of the signaling command.
/// </summary>
public struct CommandSignaler : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Stores the position the command signal was received from. Only valid if <see cref="ReceivedCommand"/> is
    ///   not <see cref="MicrobeSignalCommand.None"/>.
    /// </summary>
    public Vector3 ReceivedCommandSource;

    /// <summary>
    ///   Entity that sent the detected signal. Not valid if <see cref="ReceivedCommand"/> is not set (see
    ///   documentation on <see cref="ReceivedCommandSource"/>).
    /// </summary>
    public Entity ReceivedCommandFromEntity;

    /// <summary>
    ///   Used to limit signals reaching entities that they shouldn't.
    ///   In the microbe stage this contains the entity's species ID to allow species-wide signaling.
    /// </summary>
    public ulong SignalingChannel;

    /// <summary>
    ///   Because AI is run in parallel thread, if it wants to change the signaling, it needs to do it through this
    /// </summary>
    public MicrobeSignalCommand? QueuedSignalingCommand;

    public MicrobeSignalCommand Command;

    public MicrobeSignalCommand ReceivedCommand;

    // TODO: should this have a bool flag to disable this component when the microbe doesn't have a signaling agent?

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCommandSignaler;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(ReceivedCommandSource);
        writer.WriteAnyRegisteredValueAsObject(ReceivedCommandFromEntity);
        writer.Write(SignalingChannel);

        writer.Write(QueuedSignalingCommand.HasValue);
        if (QueuedSignalingCommand.HasValue)
            writer.Write((int)QueuedSignalingCommand.Value);

        writer.Write((int)Command);
        writer.Write((int)ReceivedCommand);
    }
}

public static class CommandSignalerHelpers
{
    public static CommandSignaler ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CommandSignaler.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CommandSignaler.SERIALIZATION_VERSION);

        var instance = new CommandSignaler
        {
            ReceivedCommandSource = reader.ReadVector3(),
            ReceivedCommandFromEntity = reader.ReadObject<Entity>(),
            SignalingChannel = reader.ReadUInt64(),
        };

        if (reader.ReadBool())
        {
            instance.QueuedSignalingCommand = (MicrobeSignalCommand)reader.ReadInt32();
        }

        instance.Command = (MicrobeSignalCommand)reader.ReadInt32();
        instance.ReceivedCommand = (MicrobeSignalCommand)reader.ReadInt32();

        return instance;
    }
}
