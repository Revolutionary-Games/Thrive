using System;
using System.Collections.Generic;

/// <summary>
///   Runs a multiplayer session: connections, handshake, peers and message dispatch
/// </summary>
/// <remarks>
///   <para>
///     This owns the parts that every mode needs, and hands anything mode-specific to the layers above through
///     <see cref="MessageReceived"/>. It deliberately does not know about entities or the game world.
///   </para>
///   <para>
///     Nothing here may use Godot, so that a headless server and the tests can run a session.
///   </para>
/// </remarks>
public class NetworkSession : IDisposable
{
    private readonly ITransport transport;
    private readonly IIdentityProvider identityProvider;

    private readonly Dictionary<int, NetworkPeer> peers = new();
    private readonly List<int> peersToRemove = new();

    private readonly NetworkWriter writer = new();
    private readonly NetworkReader reader = new();

    private string localPlayerName = string.Empty;
    private bool disposed;

    public NetworkSession(ITransport transport, IIdentityProvider identityProvider, INetworkedGameMode gameMode)
    {
        this.transport = transport;
        this.identityProvider = identityProvider;

        GameMode = gameMode;

        gameMode.RegisterReplication(ReplicationRegistry);
        ReplicationRegistry.Seal();
    }

    /// <summary>
    ///   Called for messages this class doesn't handle itself, which is everything to do with the game world.
    ///   The reader is positioned after the message type byte and is only valid during the call.
    /// </summary>
    public event Action<int, MessageType, NetworkReader>? MessageReceived;

    public event Action<NetworkPeer>? PeerJoined;
    public event Action<NetworkPeer>? PeerLeft;

    /// <summary>
    ///   Chat message received, with the peer that sent it
    /// </summary>
    public event Action<int, string>? ChatReceived;

    /// <summary>
    ///   Client only: the session ended, with the reason when there is one
    /// </summary>
    public event Action<string?>? Disconnected;

    /// <summary>
    ///   State a NetworkSession can be in
    /// </summary>
    public enum NetworkSessionState
    {
        Inactive = 0,

        /// <summary>
        ///   Client only: connected but waiting for the handshake to be accepted
        /// </summary>
        Connecting = 1,

        Active = 2,

        /// <summary>
        ///   The session ended or was rejected, see <see cref="NetworkSession.FailureReason"/>
        /// </summary>
        Failed = 3,
    }

    public NetworkSessionState State { get; private set; } = NetworkSessionState.Inactive;

    public bool IsServer => transport.IsServer;

    public int LocalPeerId { get; private set; } = NetworkConstants.INVALID_PEER_ID;

    public string? FailureReason { get; private set; }

    public ReplicationRegistry ReplicationRegistry { get; } = new();

    public IReadOnlyDictionary<int, NetworkPeer> Peers => peers;

    public INetworkedGameMode GameMode { get; }

    public bool StartServer(string address, string playerName)
    {
        if (State != NetworkSessionState.Inactive)
            throw new InvalidOperationException("Session is already started");

        localPlayerName = playerName;

        if (!transport.StartServer(address, GameMode.MaxPlayers))
        {
            FailureReason = "Failed to start the server";
            State = NetworkSessionState.Failed;
            return false;
        }

        LocalPeerId = NetworkConstants.SERVER_PEER_ID;
        State = NetworkSessionState.Active;
        return true;
    }

    public bool StartClient(string address, string playerName)
    {
        if (State != NetworkSessionState.Inactive)
            throw new InvalidOperationException("Session is already started");

        localPlayerName = playerName;

        if (!transport.StartClient(address))
        {
            FailureReason = "Failed to connect";
            State = NetworkSessionState.Failed;
            return false;
        }

        State = NetworkSessionState.Connecting;
        return true;
    }

    /// <summary>
    ///   Pumps the transport and handles everything that arrived. Must be called regularly.
    /// </summary>
    public void Process(float delta)
    {
        if (State is NetworkSessionState.Inactive or NetworkSessionState.Failed)
            return;

        while (transport.PollEvent(out var transportEvent))
        {
            switch (transportEvent.Type)
            {
                case TransportEventType.PeerConnected:
                    HandlePeerConnected(transportEvent.PeerId);
                    break;

                case TransportEventType.PeerDisconnected:
                    HandlePeerDisconnected(transportEvent.PeerId, transportEvent.Reason);
                    break;

                case TransportEventType.DataReceived:
                    HandleData(transportEvent);
                    break;
            }
        }

        if (IsServer)
            UpdateServerPeers(delta);
    }

    /// <summary>
    ///   Sends a message built by the caller to one peer
    /// </summary>
    public void Send(int peerId, ReadOnlySpan<byte> data, NetworkDelivery delivery, NetworkChannel channel)
    {
        transport.Send(peerId, data, delivery, channel);
    }

    /// <summary>
    ///   Sends to every peer that finished the handshake
    /// </summary>
    public void Broadcast(ReadOnlySpan<byte> data, NetworkDelivery delivery, NetworkChannel channel,
        int exceptPeerId = NetworkConstants.INVALID_PEER_ID)
    {
        if (!IsServer)
            throw new InvalidOperationException("Only a server can broadcast");

        foreach (var entry in peers)
        {
            if (entry.Key == exceptPeerId || !entry.Value.HandshakeComplete)
                continue;

            transport.Send(entry.Key, data, delivery, channel);
        }
    }

    public void SendChat(string message)
    {
        if (State != NetworkSessionState.Active)
            return;

        if (message.Length > NetworkConstants.MAX_CHAT_LENGTH)
            message = message.Substring(0, NetworkConstants.MAX_CHAT_LENGTH);

        writer.Reset();
        writer.Write(MessageType.Chat);
        writer.Write(LocalPeerId);
        writer.Write(message);

        if (IsServer)
        {
            Broadcast(writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);
            ChatReceived?.Invoke(LocalPeerId, message);
        }
        else
        {
            transport.Send(NetworkConstants.SERVER_PEER_ID, writer.WrittenData, NetworkDelivery.ReliableOrdered,
                NetworkChannel.Control);
        }
    }

    public void SetLocalReadyState(bool ready)
    {
        writer.Reset();
        writer.Write(MessageType.PlayerReadyState);
        writer.Write(LocalPeerId);
        writer.Write(ready);

        if (IsServer)
        {
            if (peers.TryGetValue(LocalPeerId, out var self))
                self.Ready = ready;

            Broadcast(writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);
        }
        else
        {
            transport.Send(NetworkConstants.SERVER_PEER_ID, writer.WrittenData, NetworkDelivery.ReliableOrdered,
                NetworkChannel.Control);
        }
    }

    /// <summary>
    ///   Starts a new message for an upper layer to fill in. The returned writer already has the type byte.
    /// </summary>
    public NetworkWriter BeginMessage(MessageType type)
    {
        writer.Reset();
        writer.Write(type);
        return writer;
    }

    /// <summary>
    ///   Leaves or shuts down the session, telling the other side why first
    /// </summary>
    /// <param name="reason">Reason shown to the others, for example "Returned to the menu"</param>
    public void Disconnect(string reason)
    {
        if (State is NetworkSessionState.Inactive or NetworkSessionState.Failed)
            return;

        writer.Reset();
        writer.Write(MessageType.Disconnect);
        writer.Write(reason);

        if (IsServer)
        {
            Broadcast(writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);
        }
        else
        {
            transport.Send(NetworkConstants.SERVER_PEER_ID, writer.WrittenData, NetworkDelivery.ReliableOrdered,
                NetworkChannel.Control);
        }

        Stop();
    }

    /// <summary>
    ///   Removes a peer from the session. Server only.
    /// </summary>
    public void KickPeer(int peerId, string reason)
    {
        if (!IsServer)
            throw new InvalidOperationException("Only a server can kick peers");

        writer.Reset();
        writer.Write(MessageType.Disconnect);
        writer.Write(reason);
        transport.Send(peerId, writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);

        transport.DisconnectPeer(peerId, reason);
    }

    public void Stop()
    {
        if (State == NetworkSessionState.Inactive)
            return;

        transport.Stop();
        peers.Clear();
        State = NetworkSessionState.Inactive;
        LocalPeerId = NetworkConstants.INVALID_PEER_ID;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            Stop();
            transport.Dispose();
        }

        disposed = true;
    }

    private void HandlePeerConnected(int peerId)
    {
        if (IsServer)
        {
            // The peer is not a player yet, it only becomes one once the handshake passes
            peers[peerId] = new NetworkPeer(peerId);
        }
        else
        {
            SendHandshake();
        }
    }

    private void HandlePeerDisconnected(int peerId, string? reason)
    {
        if (!IsServer)
        {
            State = NetworkSessionState.Failed;

            // A rejection reason received before the disconnect is kept, as it says more than what the transport
            // reports
            if (string.IsNullOrEmpty(FailureReason))
                FailureReason = string.IsNullOrEmpty(reason) ? "Disconnected from the server" : reason;

            Disconnected?.Invoke(FailureReason);
            return;
        }

        if (!peers.Remove(peerId, out var peer))
            return;

        if (!peer.HandshakeComplete)
            return;

        GameMode.OnPlayerLeft(peerId);
        PeerLeft?.Invoke(peer);

        writer.Reset();
        writer.Write(MessageType.PeerLeft);
        writer.Write(peerId);
        Broadcast(writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);
    }

    private void HandleData(in TransportEvent transportEvent)
    {
        if (transportEvent.Data == null)
            return;

        reader.SetData(transportEvent.Data, transportEvent.DataLength);

        try
        {
            var type = reader.ReadMessageType();

            // Anything but a handshake from a peer that hasn't passed one is ignored
            if (IsServer && type != MessageType.Handshake)
            {
                if (!peers.TryGetValue(transportEvent.PeerId, out var sender) || !sender.HandshakeComplete)
                    return;
            }

            switch (type)
            {
                case MessageType.Handshake:
                    HandleHandshake(transportEvent.PeerId);
                    break;

                case MessageType.HandshakeAccepted:
                    HandleHandshakeAccepted();
                    break;

                case MessageType.HandshakeRejected:
                    HandleHandshakeRejected();
                    break;

                case MessageType.Disconnect:
                    HandleDisconnectMessage(transportEvent.PeerId);
                    break;

                case MessageType.PeerJoined:
                    HandleRemotePeerJoined();
                    break;

                case MessageType.PeerLeft:
                    HandleRemotePeerLeft();
                    break;

                case MessageType.Chat:
                    HandleChat(transportEvent.PeerId);
                    break;

                case MessageType.PlayerReadyState:
                    HandleReadyState(transportEvent.PeerId);
                    break;

                default:
                    MessageReceived?.Invoke(transportEvent.PeerId, type, reader);
                    break;
            }
        }
        catch (EndOfNetworkMessageException e)
        {
            // A truncated or malformed message is dropped.
            if (IsServer)
                transport.DisconnectPeer(transportEvent.PeerId, e.Message);
        }
    }

    private void SendHandshake()
    {
        var ticket = identityProvider.CreateJoinTicket();

        writer.Reset();
        writer.Write(MessageType.Handshake);
        writer.Write(NetworkConstants.PROTOCOL_VERSION);
        writer.Write(ReplicationRegistry.CalculateLayoutHash());
        writer.Write(GameMode.InternalName);
        writer.Write(localPlayerName);
        writer.Write((byte)identityProvider.Kind);
        writer.Write(new ReadOnlySpan<byte>(ticket));

        transport.Send(NetworkConstants.SERVER_PEER_ID, writer.WrittenData, NetworkDelivery.ReliableOrdered,
            NetworkChannel.Control);
    }

    private void HandleHandshake(int peerId)
    {
        if (!IsServer)
            return;

        if (!peers.TryGetValue(peerId, out var peer) || peer.HandshakeComplete)
            return;

        var protocolVersion = reader.ReadUInt16();
        var layoutHash = reader.ReadInt32();
        var modeName = reader.ReadString();
        var playerName = reader.ReadString();

        reader.ReadByte();

        var ticket = reader.ReadBytes();

        if (protocolVersion != NetworkConstants.PROTOCOL_VERSION)
        {
            RejectPeer(peerId, "Different game version");
            return;
        }

        if (layoutHash != ReplicationRegistry.CalculateLayoutHash() || modeName != GameMode.InternalName)
        {
            RejectPeer(peerId, "Different game mode or build");
            return;
        }

        if (playerName.Length > NetworkConstants.MAX_PLAYER_NAME_LENGTH)
        {
            RejectPeer(peerId, "Player name is too long");
            return;
        }

        if (CountPlayers() >= GameMode.MaxPlayers)
        {
            RejectPeer(peerId, "Server is full");
            return;
        }

        if (GameMode.CurrentState != MatchState.Lobby && !GameMode.AllowsJoiningInProgress)
        {
            RejectPeer(peerId, "Match is already in progress");
            return;
        }

        identityProvider.VerifyJoinTicket(ticket, playerName, (identity, error) =>
        {
            // The peer may have left while the ticket was being verified
            if (!peers.TryGetValue(peerId, out var waitingPeer) || waitingPeer.HandshakeComplete)
                return;

            if (identity == null)
            {
                RejectPeer(peerId, error ?? "Identity was not accepted");
                return;
            }

            AcceptPeer(waitingPeer, identity.Value);
        });
    }

    private void AcceptPeer(NetworkPeer peer, PlayerIdentity identity)
    {
        peer.Identity = identity;
        peer.HandshakeComplete = true;

        writer.Reset();
        writer.Write(MessageType.HandshakeAccepted);
        writer.Write(peer.PeerId);
        WritePeerList(writer);
        transport.Send(peer.PeerId, writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);

        writer.Reset();
        writer.Write(MessageType.PeerJoined);
        writer.Write(peer.PeerId);
        writer.Write(identity.Name);
        Broadcast(writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control, peer.PeerId);

        GameMode.OnPlayerJoined(peer.PeerId, identity);
        PeerJoined?.Invoke(peer);
    }

    private void RejectPeer(int peerId, string reason)
    {
        writer.Reset();
        writer.Write(MessageType.HandshakeRejected);
        writer.Write(reason);
        transport.Send(peerId, writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);

        transport.DisconnectPeer(peerId, reason);
        peers.Remove(peerId);
    }

    private void HandleHandshakeAccepted()
    {
        LocalPeerId = reader.ReadInt32();

        int peerCount = reader.ReadUInt16();

        for (int i = 0; i < peerCount; ++i)
        {
            int peerId = reader.ReadInt32();
            var name = reader.ReadString();

            var peer = new NetworkPeer(peerId)
            {
                Identity = new PlayerIdentity(IdentityKind.Guest, 0, name),
                HandshakeComplete = true,
            };

            peers[peerId] = peer;
            PeerJoined?.Invoke(peer);
        }

        State = NetworkSessionState.Active;
    }

    private void HandleDisconnectMessage(int senderPeerId)
    {
        var reason = reader.ReadString();

        if (IsServer)
        {
            // A client saying goodbye, the transport event that follows completes the removal
            transport.DisconnectPeer(senderPeerId, reason);
            return;
        }

        // Kept so that the disconnect event that follows reports why the server dropped this client
        FailureReason = reason;
    }

    private void HandleHandshakeRejected()
    {
        FailureReason = reader.ReadString();
        State = NetworkSessionState.Failed;
        Disconnected?.Invoke(FailureReason);
    }

    private void HandleRemotePeerJoined()
    {
        int peerId = reader.ReadInt32();
        var name = reader.ReadString();

        var peer = new NetworkPeer(peerId)
        {
            Identity = new PlayerIdentity(IdentityKind.Guest, 0, name),
            HandshakeComplete = true,
        };

        peers[peerId] = peer;
        PeerJoined?.Invoke(peer);
    }

    private void HandleRemotePeerLeft()
    {
        int peerId = reader.ReadInt32();

        if (!peers.Remove(peerId, out var peer))
            return;

        PeerLeft?.Invoke(peer);
    }

    private void HandleChat(int senderPeerId)
    {
        // The claimed peer ID in the message is ignored on the server, a client cannot speak for someone else
        int claimedPeerId = reader.ReadInt32();
        var message = reader.ReadString();

        if (IsServer)
        {
            if (message.Length > NetworkConstants.MAX_CHAT_LENGTH)
                return;

            writer.Reset();
            writer.Write(MessageType.Chat);
            writer.Write(senderPeerId);
            writer.Write(message);
            Broadcast(writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);

            ChatReceived?.Invoke(senderPeerId, message);
        }
        else
        {
            ChatReceived?.Invoke(claimedPeerId, message);
        }
    }

    private void HandleReadyState(int senderPeerId)
    {
        int claimedPeerId = reader.ReadInt32();
        bool ready = reader.ReadBool();

        int peerId = IsServer ? senderPeerId : claimedPeerId;

        if (peers.TryGetValue(peerId, out var peer))
            peer.Ready = ready;

        if (IsServer)
        {
            writer.Reset();
            writer.Write(MessageType.PlayerReadyState);
            writer.Write(peerId);
            writer.Write(ready);
            Broadcast(writer.WrittenData, NetworkDelivery.ReliableOrdered, NetworkChannel.Control);
        }
    }

    private void WritePeerList(NetworkWriter target)
    {
        ushort count = 0;
        int countPosition = target.Length;
        target.Write(count);

        foreach (var entry in peers)
        {
            if (!entry.Value.HandshakeComplete)
                continue;

            target.Write(entry.Key);
            target.Write(entry.Value.Identity.Name);
            ++count;
        }

        target.OverwriteUInt16(countPosition, count);
    }

    private int CountPlayers()
    {
        int count = 0;

        foreach (var entry in peers)
        {
            if (entry.Value.HandshakeComplete)
                ++count;
        }

        return count;
    }

    private void UpdateServerPeers(float delta)
    {
        peersToRemove.Clear();

        foreach (var entry in peers)
        {
            var peer = entry.Value;

            peer.RoundTripTime = transport.GetRoundTripTime(entry.Key);

            if (peer.HandshakeComplete)
                continue;

            peer.TimeWaitingForHandshake += delta;

            if (peer.TimeWaitingForHandshake > NetworkConstants.HANDSHAKE_TIMEOUT)
                peersToRemove.Add(entry.Key);
        }

        for (int i = 0; i < peersToRemove.Count; ++i)
        {
            transport.DisconnectPeer(peersToRemove[i], "Handshake timed out");
            peers.Remove(peersToRemove[i]);
        }
    }
}
