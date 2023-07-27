using Godot;

/// <summary>
///   Describes player information in relation to the connected server (and should be shared with other peers).
/// </summary>
public class NetworkPlayerInfo : Vars
{
    public string Nickname { get; set; } = string.Empty;

    public NetworkPlayerStatus Status { get; set; } = NetworkPlayerStatus.Lobby;

    public bool LobbyReady { get; set; }

    /// <inheritdoc cref="NetworkManager.PingPongData.AverageRoundTripTime"/>
    public int Latency { get; set; }

    public override void NetworkSerialize(BytesBuffer buffer)
    {
        base.NetworkSerialize(buffer);

        buffer.Write(Nickname);
        buffer.Write((byte)Status);
        buffer.Write(LobbyReady);
        buffer.Write((ushort)Latency);
    }

    public override void NetworkDeserialize(BytesBuffer buffer)
    {
        base.NetworkDeserialize(buffer);

        Nickname = buffer.ReadString();
        Status = (NetworkPlayerStatus)buffer.ReadByte();
        LobbyReady = buffer.ReadBoolean();
        Latency = buffer.ReadUInt16();
    }

    public string GetStatusReadable()
    {
        switch (Status)
        {
            case NetworkPlayerStatus.Active:
                return TranslationServer.Translate("IN_GAME_LOWERCASE");
            case NetworkPlayerStatus.Lobby:
                return TranslationServer.Translate("IN_LOBBY_LOWERCASE");
            case NetworkPlayerStatus.Joining:
                return TranslationServer.Translate("JOINING_LOWERCASE");
            case NetworkPlayerStatus.Leaving:
                return TranslationServer.Translate("LEAVING_LOWERCASE");
            default:
                return TranslationServer.Translate("N_A");
        }
    }
}
