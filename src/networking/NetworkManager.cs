using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using Nito.Collections;
using Array = Godot.Collections.Array;

/// <summary>
///   Acting as a peer for networking. Manages high-level multiplayer mechanisms and provides support for
///   online game sessions. This is both the server and the client.
/// </summary>
public class NetworkManager : Node
{
    public const int DEFAULT_SERVER_ID = 1;
    public const int MAX_CHAT_HISTORY_COUNT = 200;

    /// <summary>
    ///   The higher the value, the more immune to short-time changes the resulting average RTT will be.
    /// </summary>
    public const float RTT_WEIGHTING_FACTOR = 0.7f;

    private static NetworkManager? instance;

    private readonly Dictionary<int, NetworkPlayerInfo> connectedPlayers = new();
    private readonly Deque<string> chatHistory = new();

    private PingPongData pingPong;

    private int bandwidthOutgoingPointer;
    private BandwidthFrame[]? bandwidthOutgoingData;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private NetworkedMultiplayerENet? peer;
#pragma warning restore CA2213 // Disposable fields should be disposed

    private UPNP? upnp;

    private float tickTimer;

    /// <summary>
    ///   Accumulates decimals from the truncated delta milliseconds.
    /// </summary>
    private float truncatedDecimals;

    private NetworkManager()
    {
        instance = this;
    }

    [Signal]
    public delegate void UpnpCallResultReceived(UPNP.UPNPResult result, UpnpJobStep step);

    [Signal]
    public delegate void RegistrationResultReceived(int peerId, RegistrationResult result);

    [Signal]
    public delegate void ConnectionFailed(string reason);

    [Signal]
    public delegate void ServerStateUpdated();

    [Signal]
    public delegate void Kicked(string reason);

    [Signal]
    public delegate void ChatReceived();

    /// <summary>
    ///   Emitted when a player has joined the current game session and is done setting things up.
    /// </summary>
    [Signal]
    public delegate void PlayerJoined(int peerId);

    /// <summary>
    ///   Emitted when a player with the specified ID has left the current game session.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: Does not account for cases of unexpected disconnections.
    ///   </para>
    /// </remarks>
    [Signal]
    public delegate void PlayerLeft(int peerId);

    [Signal]
    public delegate void LobbyReadyStateReceived(int peerId, bool ready);

    [Signal]
    public delegate void PlayerStatusChanged(int peerId, NetworkPlayerStatus status);

    [Signal]
    public delegate void LatencyUpdated(int peerId, int miliseconds);

    public enum UpnpJobStep
    {
        Discovery,
        PortMapping,
    }

    public enum RegistrationResult
    {
        Success,
        ServerFull,
        DuplicateName,
    }

    public static NetworkManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public NetworkedMultiplayerPeer.ConnectionStatus Status => peer?.GetConnectionStatus() ??
        NetworkedMultiplayerPeer.ConnectionStatus.Disconnected;

    /// <summary>
    ///   Settings for the current server.
    /// </summary>
    public Vars ServerSettings { get; set; } = new();

    public float TickIntervalMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///   The current latency in milliseconds between server and client.
    /// </summary>
    public long Latency => pingPong.AverageRoundTripTime;

    public int RawPacketBytesSent
    {
        get
        {
            if (bandwidthOutgoingData == null)
                return 0;

            var totalBandwidth = 0;

            var timestamp = OS.GetTicksMsec();
            var finalTimestamp = timestamp - 1000;

            var i = (bandwidthOutgoingPointer + bandwidthOutgoingData.Length - 1) % bandwidthOutgoingData.Length;

            while (i != bandwidthOutgoingPointer && bandwidthOutgoingData[i].PacketSize > 0)
            {
                if (bandwidthOutgoingData[i].Timestamp < finalTimestamp)
                    return totalBandwidth;

                totalBandwidth += bandwidthOutgoingData[i].PacketSize;
                i = (i + bandwidthOutgoingData.Length - 1) % bandwidthOutgoingData.Length;
            }

            if (i == bandwidthOutgoingPointer)
                PrintError("Reached the end of the bandwidth profiler buffer, values might be inaccurate.");

            return totalBandwidth;
        }
    }

    public float TimePassedConnecting { get; private set; }

    public int PeerId { get; private set; }

    /// <summary>
    ///   Whether this instance is a peer in a network (in multiplayer mode).
    /// </summary>
    public bool IsMultiplayer => peer != null;

    /// <summary>
    ///   All peers connected in the network (INCLUDING SELF), stored by network ID.
    /// </summary>
    public IReadOnlyDictionary<int, NetworkPlayerInfo> ConnectedPlayers => connectedPlayers;

    /// <summary>
    ///   Session information regarding the local player in the network.
    /// </summary>
    public NetworkPlayerInfo? LocalPlayer => IsMultiplayer ? GetPlayerInfo(PeerId) : null;

    public IReadOnlyList<string> ChatHistory => chatHistory;

    /// <summary>
    ///   Dedicated server is a separate build of the game and has no local player set.
    /// </summary>
    public bool IsDedicated => IsServer && !HasPlayer(DEFAULT_SERVER_ID);

    /// <summary>
    ///   Checks whether we're hosting a game.
    /// </summary>
    /// <returns>
    ///   True, if <see cref="PeerId"/> equals to <see cref="DEFAULT_SERVER_ID"/>.
    /// </returns>
    public bool IsServer => IsMultiplayer && PeerId == DEFAULT_SERVER_ID && IsNetworkMaster();

    /// <summary>
    ///   Checks whether we're connected to a host.
    /// </summary>
    public bool IsClient => IsMultiplayer && !IsServer && !IsNetworkMaster();

    /// <summary>
    ///   Whether a networked game is in session.
    /// </summary>
    [PuppetSync]
    public bool GameInSession { get; private set; }

    /// <summary>
    ///   The elapsed time in milliseconds since the start of current game session.
    /// </summary>
    /// <remarks>
    ///   This is synchronized with the server if we're a client.
    /// </remarks>
    public long ElapsedGameTime { get; private set; }

    public int ElapsedGameTimeMinutes => Mathf.FloorToInt((ElapsedGameTime * 0.001f) / 60);

    public int ElapsedGameTimeSeconds => Mathf.FloorToInt((ElapsedGameTime * 0.001f) % 60);

    /// <summary>
    ///   Returns the elapsed game time in a short format (MM:SS).
    /// </summary>
    public string GameTimeFormatted => StringUtils.FormatShortMinutesSeconds(ElapsedGameTimeMinutes, ElapsedGameTimeSeconds);

    /// <summary>
    ///   Returns the elapsed game time in a more readable format (with explicit minutes and seconds).
    /// </summary>
    public string GameTimeHumanized => StringUtils.FormatLongMinutesSeconds(
        ElapsedGameTimeMinutes, ElapsedGameTimeSeconds);

    public override void _Ready()
    {
        GetTree().Connect("network_peer_connected", this, nameof(OnPeerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
        GetTree().Connect("server_disconnected", this, nameof(OnServerDisconnected));
        GetTree().Connect("connection_failed", this, nameof(OnConnectionFailed));

        Multiplayer.Connect("network_peer_packet", this, nameof(OnPeerPacket));

        Connect(nameof(UpnpCallResultReceived), this, nameof(OnUpnpCallResultReceived));

        ProcessPriority = -1;
        PauseMode = PauseModeEnum.Process;
    }

    public override void _Process(float delta)
    {
        if (!IsMultiplayer)
            return;

        if (Status == NetworkedMultiplayerPeer.ConnectionStatus.Connecting)
        {
            TimePassedConnecting += delta;
        }

        if (Status == NetworkedMultiplayerPeer.ConnectionStatus.Connected)
            HandlePing();

        if (GameInSession)
        {
            var deltaMilliseconds = delta * 1000f;
            ElapsedGameTime += (long)deltaMilliseconds;
            truncatedDecimals += deltaMilliseconds - (long)deltaMilliseconds;

            // Regain back precision
            if (truncatedDecimals >= 1.0f)
            {
                ++ElapsedGameTime;
                truncatedDecimals = 0;
            }

            Engine.IterationsPerSecond = (int)(ServerSettings.GetVar<int>("TickRate") * TickIntervalMultiplier);
        }
    }

    /// <summary>
    ///   Sends the given raw bytes to a specific peer identified by id (see
    ///   <see cref="NetworkedMultiplayerPeer.SetTargetPeer(int)"/>).
    ///   Default ID is 0, i.e. broadcast to all peers.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This tracks the total packet bytes sent, unlike
    ///     <see cref="MultiplayerAPI.SendBytes(byte[], int, NetworkedMultiplayerPeer.TransferModeEnum)"/>
    ///     which does not for some reason.
    ///   </para>
    /// </remarks>
    public Error SendBytes(byte[] bytes, int id = 0, NetworkedMultiplayerPeer.TransferModeEnum mode =
        NetworkedMultiplayerPeer.TransferModeEnum.Reliable)
    {
        if (bytes.Length <= 0)
        {
            PrintError("Trying to send an empty raw packet.");
            return Error.InvalidData;
        }

        if (peer == null)
        {
            PrintError("Trying to send a raw packet while no network peer is active.");
            return Error.Unconfigured;
        }

        if (Status != NetworkedMultiplayerPeer.ConnectionStatus.Connected)
        {
            PrintError("Trying to send a raw packet via a network peer which is not connected.");
            return Error.Unconfigured;
        }

        const int RAW_PACKET_SIGNATURE = 4;

        var packet = new byte[bytes.Length + 1];
        packet[0] = RAW_PACKET_SIGNATURE;
        Buffer.BlockCopy(bytes, 0, packet, 1, bytes.Length);

        if (bandwidthOutgoingData != null)
        {
            bandwidthOutgoingData[bandwidthOutgoingPointer].Timestamp = OS.GetTicksMsec();
            bandwidthOutgoingData[bandwidthOutgoingPointer].PacketSize = packet.Length;
            bandwidthOutgoingPointer = (bandwidthOutgoingPointer + 1) % bandwidthOutgoingData.Length;
        }

        peer.SetTargetPeer(id);
        peer.TransferMode = mode;

        return peer.PutPacket(packet);
    }

    /// <summary>
    ///   Creates a server without creating player for this peer i.e. dedicated server.
    /// </summary>
    public Error CreateServer(Vars settings)
    {
        TimePassedConnecting = 0;
        peer = new NetworkedMultiplayerENet();
        peer.SetBindIp(settings.GetVar<string>("Address"));

        // TODO: enable DTLS for secure transport?

        var error = peer.CreateServer(settings.GetVar<int>("Port"), Constants.NETWORK_DEFAULT_MAX_CLIENTS);
        if (error != Error.Ok)
        {
            PrintError("An error occurred while trying to create server: ", error);
            return error;
        }

        ServerSettings = settings;

        // Could have just set this to 1 but juuust in case
        PeerId = peer.GetUniqueId();

        GetTree().NetworkPeer = peer;

        Print("Created server with the following settings\n", settings);

        return error;
    }

    /// <summary>
    ///   Creates a server hosted on the player's machine i.e. listen-server.
    /// </summary>
    public Error CreatePlayerHostedServer(string playerName, Vars settings)
    {
        var result = CreateServer(settings);
        if (result != Error.Ok)
            return result;

        if (settings.GetVar<bool>("UseUpnp"))
        {
            // Automatic port mapping/forwarding with UPnP
            TaskExecutor.Instance.AddTask(new Task(() => SetupUpnp()));
        }

        OnConnectedToServer(playerName);
        NotifyLobbyReadyStateChange(DEFAULT_SERVER_ID, true);

        Print("Server is player hosted");

        return result;
    }

    public Error ConnectToServer(string address, int port, string playerName)
    {
        TimePassedConnecting = 0;

        peer = new NetworkedMultiplayerENet();

        // TODO: enable DTLS for secure transport?

        GetTree().CheckAndConnect(
            "connected_to_server", this, nameof(OnConnectedToServer), new Array { playerName },
            (int)ConnectFlags.Oneshot);

        var result = peer.CreateClient(address, port);
        if (result != Error.Ok)
        {
            PrintError("An error occurred while trying to create client: ", result);
            return result;
        }

        GetTree().NetworkPeer = peer;

        return result;
    }

    public void Disconnect()
    {
        Print("Disconnecting...");
        peer?.CloseConnection();
        Cleanup();
    }

    public bool HasPlayer(int peerId)
    {
        return connectedPlayers.ContainsKey(peerId);
    }

    public NetworkPlayerInfo? GetPlayerInfo(int peerId)
    {
        connectedPlayers.TryGetValue(peerId, out NetworkPlayerInfo result);
        return result;
    }

    /// <summary>
    ///   Sets a player's server-wide variable.
    /// </summary>
    public void SetVar(int peerId, string what, object variant)
    {
        if (IsClient)
            return;

        Rpc(nameof(SyncVars), peerId, what, variant);
    }

    public void Join()
    {
        if (!IsDedicated && LocalPlayer!.Status == NetworkPlayerStatus.Active)
            return;

        NotifyGameLoad();

        if (IsServer)
        {
            foreach (var player in connectedPlayers)
            {
                if (player.Key == DEFAULT_SERVER_ID)
                    continue;

                if (player.Value.Status == NetworkPlayerStatus.Lobby)
                    RpcId(player.Key, nameof(NotifyGameLoad));
            }
        }
    }

    public void Exit()
    {
        if (!IsDedicated && LocalPlayer!.Status == NetworkPlayerStatus.Lobby)
            return;

        if (IsServer && GameInSession)
            Rset(nameof(GameInSession), false);

        NotifyGameExit();

        if (IsServer)
        {
            foreach (var player in connectedPlayers)
            {
                if (player.Key == DEFAULT_SERVER_ID)
                    continue;

                if (player.Value.Status == NetworkPlayerStatus.Active)
                    RpcId(player.Key, nameof(NotifyGameExit));
            }
        }
    }

    public void SetLobbyReadyState(bool ready)
    {
        if (IsServer)
            return;

        RpcId(DEFAULT_SERVER_ID, nameof(NotifyLobbyReadyStateChange), PeerId, ready);
    }

    public void Kick(int id, string reason)
    {
        RpcId(id, nameof(NotifyKick), reason);
    }

    /// <summary>
    ///   Parses a message and if appropriate sends that message to all peers.
    /// </summary>
    public void Chat(string message)
    {
        if (!IsMultiplayer)
            return;

        if (message.BeginsWith("/") && ParseCommand(message.TrimStart('/')))
        {
            // Is a valid command, don't send this
            return;
        }

        Rpc(nameof(SendChat), message);
    }

    /// <summary>
    ///   Outputs a chat with the sender marked as "system" to the chat history.
    /// </summary>
    public void SystemChatNotification(string message)
    {
        PutChat(TranslationServer.Translate("SYSTEM_CHAT").FormatSafe(message));
    }

    public void ClearChatHistory()
    {
        chatHistory.Clear();
    }

    /// <summary>
    ///   Differentiates between print call from server or client. We do this so that they will stand out more
    ///   on the output log.
    /// </summary>
    public void Print(params object[] what)
    {
        var str = string.Concat(System.Array.ConvertAll(what, x => x?.ToString() ?? "null"));
        var serverOrHost = IsDedicated ? "[Server] " : "[Host] ";
        GD.Print(IsServer ? serverOrHost : "[Client] ", str);
    }

    public void PrintError(params object[] what)
    {
        var str = string.Concat(System.Array.ConvertAll(what, x => x?.ToString() ?? "null"));
        var serverOrHost = IsDedicated ? "[Server] " : "[Host] ";
        GD.PrintErr(IsServer ? serverOrHost : "[Client] ", str);
    }

    public void InitProfiling()
    {
        bandwidthOutgoingPointer = 0;
        bandwidthOutgoingData = new BandwidthFrame[16384];
    }

    public void ClearProfiling()
    {
        bandwidthOutgoingPointer = 0;
        bandwidthOutgoingData = null;
    }

    /// <summary>
    ///   Clears network session data.
    /// </summary>
    private void Cleanup()
    {
        if (upnp?.GetDeviceCount() > 0)
        {
            upnp?.DeletePortMapping(ServerSettings.GetVar<int>("Port"));
            upnp = null;
        }

        peer = null;
        connectedPlayers.Clear();
        GameInSession = false;
        ElapsedGameTime = 0;
        ServerSettings.Clear();

        ClearChatHistory();
    }

    private void SetupUpnp()
    {
        upnp ??= new UPNP();

        var result = (UPNP.UPNPResult)upnp.Discover();

        if (result != UPNP.UPNPResult.Success)
            PrintError("UPnP devices discovery failed: ", result);

        EmitSignal(nameof(UpnpCallResultReceived), result, UpnpJobStep.Discovery);
    }

    private void AddPortMapping(int port)
    {
        if (upnp == null)
            return;

        if (upnp.GetDeviceCount() <= 0)
            return;

        if (upnp.GetGateway()?.IsValidGateway() == true)
        {
            // May not work in some cases even if the gateway supports UPNP/port forwarding. The player's network
            // might be on CGNAT or double NAT, preventing this from working succesfully
            // I can't test this because of the aforementioned issue - kasterisk
            var pmResult = (UPNP.UPNPResult)upnp.AddPortMapping(port, 0, "ThriveGame");

            // TODO: error handling

            EmitSignal(nameof(UpnpCallResultReceived), pmResult, UpnpJobStep.PortMapping);
            return;
        }

        EmitSignal(nameof(UpnpCallResultReceived), UPNP.UPNPResult.Success, UpnpJobStep.PortMapping);
    }

    /// <summary>
    ///   Only a temporary way to parse commands.
    /// </summary>
    private bool ParseCommand(string input)
    {
        var args = System.Array.Empty<string>();
        var command = input;

        if (input.Contains(' '))
        {
            var chunks = input.Split(' ');
            args = chunks.Skip(1).ToArray();
            command = chunks[0];
        }

        switch (command)
        {
            case "clear":
                ClearChatHistory();

                // Update chatboxes
                EmitSignal(nameof(ChatReceived));

                break;
            case "tickrate":
                if (!int.TryParse(args.FirstOrDefault(), out int tickRate) || !IsServer)
                    return false;

                Rpc(nameof(NotifyServerTickRateChange), tickRate);

                break;
            case "sendrate":
                if (!int.TryParse(args.FirstOrDefault(), out int sendRate) || !IsServer)
                    return false;

                Rpc(nameof(NotifyServerSendRateChange), sendRate);

                break;
            case "end":
                ElapsedGameTime = ServerSettings.GetVar<uint>("SessionLength") * 60000;
                break;
            case "interp":
                if (!bool.TryParse(args.FirstOrDefault(), out bool interp))
                    return false;

                ServerSettings.SetVar("Interpolate", interp);
                PutChat("Interpolate: " + interp);
                break;
        }

        return true;
    }

    private void PutChat(string message)
    {
        if (chatHistory.Count > MAX_CHAT_HISTORY_COUNT)
            chatHistory.RemoveFromFront();

        chatHistory.AddToBack(message);

        EmitSignal(nameof(ChatReceived));
    }

    private void HandlePing()
    {
        // Must be client-initiated
        if (!IsClient)
            return;

        var currentTime = Time.GetTicksMsec();

        // Check if timed out
        if (pingPong.IsWaitingAck && currentTime > pingPong.TimeSent + Constants.NETWORK_PING_TIMEOUT_MILLISECONDS)
        {
            ++pingPong.PacketLost;
            pingPong.IsWaitingAck = false;
        }

        // Ping server
        if (!pingPong.IsWaitingAck && currentTime > pingPong.TimeSent + Constants.NETWORK_PING_INTERVAL_MILLISECONDS)
        {
            SendPing();
        }

        DebugOverlays.Instance.ReportPingPong(pingPong);
    }

    private void SendPing()
    {
        // Must be client-initiated
        if (!IsClient)
            return;

        ++pingPong.Id;
        pingPong.TimeSent = Time.GetTicksMsec();
        pingPong.IsWaitingAck = true;

        var msg = new BytesBuffer(3);
        msg.Write((byte)RawPacketFlag.Ping);
        msg.Write((ushort)pingPong.Id);

        SendBytes(msg.Data, DEFAULT_SERVER_ID, NetworkedMultiplayerPeer.TransferModeEnum.Unreliable);
    }

    /// <summary>
    ///   Replies ping.
    /// </summary>
    private void ProcessReceivedPing(int fromId, BytesBuffer pingBuffer)
    {
        // Must be received by the server
        if (!IsServer)
            return;

        var pingId = pingBuffer.ReadUInt16();

        var msg = new BytesBuffer(3);
        msg.Write((byte)RawPacketFlag.Pong);
        msg.Write(pingId);
        msg.Write(ElapsedGameTime);

        // Send back to the client (pong)
        SendBytes(msg.Data, fromId, NetworkedMultiplayerPeer.TransferModeEnum.Unreliable);
    }

    private void ProcessReceivedPong(ulong timeReceived, BytesBuffer pongBuffer)
    {
        var receivedId = pongBuffer.ReadUInt16();
        var serverTime = pongBuffer.ReadInt64();

        if (receivedId != pingPong.Id)
        {
            PrintError("Invalid pong packet received");
            return;
        }

        pingPong.IsWaitingAck = false;

        var roundTripTime = timeReceived - pingPong.TimeSent;

        var oldRtt = pingPong.AverageRoundTripTime;

        // Smooth it out (TCP RTT calculation)
        pingPong.AverageRoundTripTime = (long)(pingPong.AverageRoundTripTime <= 0 ?
            roundTripTime :
            (RTT_WEIGHTING_FACTOR * pingPong.AverageRoundTripTime) + ((1 - RTT_WEIGHTING_FACTOR) * roundTripTime));

        pingPong.DeltaRoundTripTime = oldRtt - pingPong.AverageRoundTripTime;

        pingPong.EstimatedTimeOffset = serverTime + (pingPong.AverageRoundTripTime / 2) - ElapsedGameTime;
        ElapsedGameTime += pingPong.EstimatedTimeOffset + pingPong.DeltaRoundTripTime;

        Rpc(nameof(NotifyPeerLatency), (ushort)pingPong.AverageRoundTripTime);
    }

    private void OnPeerConnected(int id)
    {
        _ = id;

        // Will probaby be useful later.
    }

    private void OnPeerDisconnected(int id)
    {
        if (!HasPlayer(id))
            return;

        Print("User \"", GetPlayerInfo(id)!.Nickname, "\" ID: ", id, " has disconnected");

        SystemChatNotification(
            TranslationServer.Translate("PLAYER_HAS_DISCONNECTED").FormatSafe(GetPlayerInfo(id)!.Nickname));

        NotifyPlayerDisconnected(id);
    }

    /// <summary>
    ///   Process received raw packets.
    /// </summary>
    private void OnPeerPacket(int fromId, byte[] data)
    {
        var timeReceived = Time.GetTicksMsec();

        var msg = new BytesBuffer(data);

        var packetType = (RawPacketFlag)msg.ReadByte();

        switch (packetType)
        {
            case RawPacketFlag.Ping:
                ProcessReceivedPing(fromId, msg);
                break;
            case RawPacketFlag.Pong:
                ProcessReceivedPong(timeReceived, msg);
                break;
            default:
                // Unknown packet, possibly malformed or not handled by us
                break;
        }
    }

    /// <summary>
    ///   Client-side handler once connected to the server.
    /// </summary>
    private void OnConnectedToServer(string playerName)
    {
        PeerId = peer!.GetUniqueId();

        var msg = new BytesBuffer();
        var info = new NetworkPlayerInfo { Nickname = playerName };
        info.NetworkSerialize(msg);

        // Send our info
        RpcId(DEFAULT_SERVER_ID, nameof(OnPlayerConnected), PeerId, msg.Data);

        TimePassedConnecting = 0;
    }

    private void OnServerDisconnected()
    {
        Print("Disconnected from server");
        Cleanup();
        EmitSignal(nameof(ServerStateUpdated));
    }

    private void OnConnectionFailed()
    {
        var reason = TranslationServer.Translate("BAD_CONNECTION");

        // TODO: This is a bit flaky
        if (Mathf.RoundToInt(TimePassedConnecting) >= Constants.NETWORK_DEFAULT_TIMEOUT_LIMIT_SECONDS)
            reason = TranslationServer.Translate("TIMEOUT");

        EmitSignal(nameof(ConnectionFailed), reason);

        TimePassedConnecting = 0;

        Print("Connection failed");
    }

    private void OnUpnpCallResultReceived(UPNP.UPNPResult result, UpnpJobStep step)
    {
        switch (step)
        {
            case UpnpJobStep.Discovery:
            {
                if (result == UPNP.UPNPResult.Success)
                    TaskExecutor.Instance.AddTask(new Task(() => AddPortMapping(ServerSettings.GetVar<int>("Port"))));

                break;
            }
        }
    }

    private void OnGameReady(object sender, EventArgs args)
    {
        if (IsServer && !GameInSession)
            Rset(nameof(GameInSession), true);

        Print("Local game is now ready, notifying the host");

        RpcId(DEFAULT_SERVER_ID, nameof(NotifyWorldReady), PeerId);
    }

    /// <summary>
    ///   Serves as an entry point for inbound clients (players).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     How this works from the server's perspective: a newly connected client sends their data to us (the server),
    ///     we then do validity checks and if succeed we register that client and give it our current state.
    ///   </para>
    ///   <para>
    ///     How this works from the client's perspective: the server sends other client(s) data to us which we'll then
    ///     store locally.
    ///   </para>
    /// </remarks>
    [RemoteSync]
    private void OnPlayerConnected(int id, byte[] data)
    {
        var inboundPlayerMsg = new BytesBuffer(data);
        var inboundPlayer = new NetworkPlayerInfo();
        inboundPlayer.NetworkDeserialize(inboundPlayerMsg);

        if (IsServer)
        {
            if (connectedPlayers.Count >= ServerSettings.GetVar<int>("MaxPlayers"))
            {
                // No need to notify everyone about this
                RpcId(id, nameof(NotifyRegistrationResult), id, RegistrationResult.ServerFull);

                peer!.DisconnectPeer(id);
                return;
            }

            if (connectedPlayers.Values.Any(p => p.Nickname == inboundPlayer.Nickname))
            {
                // No need to notify everyone about this
                RpcId(id, nameof(NotifyRegistrationResult), id, RegistrationResult.DuplicateName);

                peer!.DisconnectPeer(id);
                return;
            }

            // TODO: authentication

            if (id != DEFAULT_SERVER_ID)
            {
                // Validated, now give the client our server configuration
                RpcId(id, nameof(SendServerConfigs), ThriveJsonConverter.Instance.SerializeObject(ServerSettings));
            }

            if (GameInSession)
                RsetId(id, nameof(GameInSession), true);

            foreach (var player in connectedPlayers)
            {
                if (player.Key != DEFAULT_SERVER_ID)
                {
                    // Forward newly connected player info to the other players
                    RpcId(player.Key, nameof(OnPlayerConnected), id, data);
                }

                // Send other players' info to the newly connected player, including us if we're a listen-server
                var otherPlayerMsg = new BytesBuffer();
                player.Value.NetworkSerialize(otherPlayerMsg);
                RpcId(id, nameof(OnPlayerConnected), player.Key, otherPlayerMsg.Data);
                RpcId(id, nameof(NotifyPlayerStatusChange), player.Key, player.Value.Status);
            }

            if (id != DEFAULT_SERVER_ID)
            {
                // Return sender client's own info so they can have themselves registered locally
                RpcId(id, nameof(OnPlayerConnected), id, data);
            }

            // TODO: might not be true...
            Print("User \"", inboundPlayer.Nickname, "\" ID: ", id, " has connected");
        }

        if (HasPlayer(id))
            return;

        connectedPlayers.Add(id, inboundPlayer);
        EmitSignal(nameof(ServerStateUpdated));

        SystemChatNotification(
            TranslationServer.Translate("PLAYER_HAS_CONNECTED").FormatSafe(inboundPlayer.Nickname));

        if (IsServer)
        {
            // Tell all peers (and ourselves if this is client hosted) that a new peer have
            // been successfully registered to the server
            NotifyRegistrationResult(id, RegistrationResult.Success);
        }
    }

    [Remote]
    private void NotifyPlayerDisconnected(int id)
    {
        if (!HasPlayer(id))
            return;

        connectedPlayers.Remove(id);
        EmitSignal(nameof(ServerStateUpdated));
    }

    [Remote]
    private void SendServerConfigs(string settings)
    {
        ServerSettings = ThriveJsonConverter.Instance.DeserializeObject<Vars>(settings) ??
            throw new Exception("deserialized value is null");

        ServerSettings.SetVar("Prediction", false);
        ServerSettings.SetVar("Interpolate", true);

        Print("Received server settings:\n", JsonConvert.SerializeObject(ServerSettings, Formatting.Indented));
    }

    [RemoteSync]
    private void NotifyRegistrationResult(int peerId, RegistrationResult result)
    {
        if (IsServer)
        {
            foreach (var player in connectedPlayers)
            {
                if (player.Key != DEFAULT_SERVER_ID)
                    RpcId(player.Key, nameof(NotifyRegistrationResult), peerId, result);
            }
        }

        EmitSignal(nameof(RegistrationResultReceived), peerId, result);
    }

    [RemoteSync]
    private void NotifyPlayerStatusChange(int id, NetworkPlayerStatus environment)
    {
        if (IsServer)
        {
            foreach (var player in connectedPlayers)
            {
                if (player.Key == DEFAULT_SERVER_ID)
                    continue;

                RpcId(player.Key, nameof(NotifyPlayerStatusChange), id, environment);
            }
        }

        var info = GetPlayerInfo(id);
        if (info != null)
        {
            info.Status = environment;
            EmitSignal(nameof(PlayerStatusChanged), id, environment);
        }
    }

    [Remote]
    private void NotifyKick(string reason)
    {
        if (GetTree().GetRpcSenderId() != DEFAULT_SERVER_ID)
        {
            PrintError("Kicking is only permissible from host/server");
            return;
        }

        Disconnect();
        EmitSignal(nameof(Kicked), reason);
    }

    [RemoteSync]
    private void NotifyLobbyReadyStateChange(int peerId, bool ready)
    {
        if (IsServer)
        {
            foreach (var player in connectedPlayers)
            {
                if (player.Key == DEFAULT_SERVER_ID)
                    continue;

                RpcId(player.Key, nameof(NotifyLobbyReadyStateChange), peerId, ready);
            }
        }

        var info = GetPlayerInfo(peerId)!;
        info.LobbyReady = ready;

        EmitSignal(nameof(LobbyReadyStateReceived), peerId, ready);
    }

    [RemoteSync]
    private void NotifyServerTickRateChange(int tickRate)
    {
        ServerSettings.SetVar("TickRate", tickRate);
        SystemChatNotification(TranslationServer.Translate("SERVER_TICK_RATE_CHANGED").FormatSafe(tickRate));
        Print("Tick rate changed to: ", tickRate);
    }

    [RemoteSync]
    private void NotifyServerSendRateChange(int sendRate)
    {
        ServerSettings.SetVar("SendRate", sendRate);
        SystemChatNotification(TranslationServer.Translate("SERVER_SEND_RATE_CHANGED").FormatSafe(sendRate));
        Print("Send rate changed to: ", sendRate);
    }

    [RemoteSync]
    private void NotifyPeerLatency(ushort latency)
    {
        var sender = GetTree().GetRpcSenderId();

        var info = GetPlayerInfo(sender);

        if (info != null)
        {
            info.Latency = latency;
            EmitSignal(nameof(LatencyUpdated), sender, latency);
        }
    }

    [Remote]
    private void NotifyGameLoad()
    {
        var gameMode = SimulationParameters.Instance.GetMultiplayerGameMode(ServerSettings.GetVar<string>("GameMode"));

        Rpc(nameof(NotifyWorldPreLoad), PeerId);

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.2f, () =>
        {
            var scene = SceneManager.Instance.LoadScene(gameMode.MainScene);
            var stage = (IMultiplayerStage)scene.Instance();

            stage.GameReady += OnGameReady;
            SceneManager.Instance.SwitchToScene(stage.GameStateRoot);
        });

        Print("Starting the game");
    }

    [Remote]
    private void NotifyGameExit()
    {
        Rpc(nameof(NotifyWorldPreExit), PeerId);

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.3f, () =>
        {
            var menu = SceneManager.Instance.ReturnToMenu();
            menu.OpenMultiplayerMenu(MultiplayerGUI.SubMenu.Lobby);
            Rpc(nameof(NotifyWorldPostExit), PeerId);
        });

        ElapsedGameTime = 0;

        Print("Exiting current game session");
    }

    [RemoteSync]
    private void NotifyWorldPreLoad(int peerId)
    {
        var playerInfo = GetPlayerInfo(peerId);

        if (playerInfo == null)
            return;

        playerInfo.Status = NetworkPlayerStatus.Joining;
        EmitSignal(nameof(PlayerStatusChanged), peerId, playerInfo.Status);
    }

    [RemoteSync]
    private void NotifyWorldReady(int peerId)
    {
        var sender = GetTree().GetRpcSenderId();

        if (IsServer && sender != DEFAULT_SERVER_ID &&
            LocalPlayer?.Status != NetworkPlayerStatus.Active)
        {
            Print("Not yet ready. \"World ready\" notification from ", sender, " is rejected");
            return;
        }

        if (IsClient && sender != DEFAULT_SERVER_ID)
            return;

        var playerInfo = GetPlayerInfo(peerId);
        if (playerInfo == null)
            return;

        if (IsServer)
        {
            Print("Received \"world ready\" notification from ", peerId, ", forwarding to others");

            foreach (var player in ConnectedPlayers)
            {
                if (player.Key != DEFAULT_SERVER_ID)
                    RpcId(player.Key, nameof(NotifyWorldReady), peerId);
            }
        }

        playerInfo.Status = NetworkPlayerStatus.Active;
        EmitSignal(nameof(PlayerJoined), peerId);
        EmitSignal(nameof(PlayerStatusChanged), peerId, playerInfo.Status);

        SystemChatNotification(TranslationServer.Translate("PLAYER_HAS_JOINED").FormatSafe(playerInfo.Nickname));
    }

    [RemoteSync]
    private void NotifyWorldPreExit(int peerId)
    {
        var playerInfo = GetPlayerInfo(peerId);

        if (playerInfo == null)
            return;

        playerInfo.Status = NetworkPlayerStatus.Leaving;
        EmitSignal(nameof(PlayerStatusChanged), peerId, playerInfo.Status);
        EmitSignal(nameof(PlayerLeft), peerId);
    }

    [RemoteSync]
    private void NotifyWorldPostExit(int peerId)
    {
        var playerInfo = GetPlayerInfo(peerId);

        if (playerInfo == null)
            return;

        playerInfo.Status = NetworkPlayerStatus.Lobby;
        EmitSignal(nameof(PlayerStatusChanged), peerId, playerInfo.Status);
        EmitSignal(nameof(LobbyReadyStateReceived), peerId, playerInfo.LobbyReady);
        EmitSignal(nameof(ServerStateUpdated));

        SystemChatNotification(TranslationServer.Translate("PLAYER_HAS_LEFT").FormatSafe(playerInfo.Nickname));
    }

    [RemoteSync]
    private void SendChat(string message)
    {
        var senderId = GetTree().GetRpcSenderId();
        var senderInfo = GetPlayerInfo(senderId);

        var taggedMessage = $"[b][color=#d7ff73][lb]{senderInfo?.Nickname}[rb]: [/color][/b] {message}";

        PutChat(taggedMessage);
    }

    [PuppetSync]
    private void SyncVars(int peerId, string what, object variant)
    {
        var sender = GetTree().GetRpcSenderId();
        if (sender != DEFAULT_SERVER_ID)
            return;

        var info = GetPlayerInfo(peerId);
        info?.SetVar(what, variant);
    }

    /// <summary>
    ///   Information derived by pinging the server.
    /// </summary>
    public struct PingPongData
    {
        /// <summary>
        ///   The current id of a ping packet being transmitted.
        /// </summary>
        public int Id;

        /// <summary>
        ///   A timestamp of when a ping is transmitted.
        /// </summary>
        public ulong TimeSent;

        /// <summary>
        ///   The time it takes in milliseconds for a packet to be transmitted from the client (in this
        ///   case a ping/pong packet) to the server and back.
        /// </summary>
        public long AverageRoundTripTime;

        /// <summary>
        ///   The difference between previous and newly calculated average round trip time.
        /// </summary>
        public long DeltaRoundTripTime;

        /// <summary>
        ///   The offset between the client's and the server's clock <see cref="ElapsedGameTime"/>.
        /// </summary>
        public long EstimatedTimeOffset;

        /// <summary>
        ///   Returns true if a pong is expected from the server.
        /// </summary>
        public bool IsWaitingAck;

        /// <summary>
        ///   Increments if current time > timeout factor.
        /// </summary>
        public uint PacketLost;
    }

    public struct BandwidthFrame
    {
        public ulong Timestamp;
        public int PacketSize;
    }
}
