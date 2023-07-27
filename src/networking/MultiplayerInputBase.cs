using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles sending client inputs to the server.
/// </summary>
public abstract class MultiplayerInputBase : PlayerInputBase
{
    private Dictionary<int, PeerInputs> peersInputs = new();

    private NetworkInputVars[] localPlayerInputBuffer = new NetworkInputVars[Constants.BUFFER_MAX_TICKS];

    /// <summary>
    ///   Contain inputs for the local player, see <see cref="NetworkCharacter.IsLocal"/>.
    /// </summary>
    public PeerInputs LocalInputs
    {
        get
        {
            peersInputs.TryGetValue(NetworkManager.Instance.PeerId, out var peerInput);
            return peerInput;
        }
    }

    public NetworkInputVars PreviousInput { get; protected set; }

    /// <summary>
    ///   Dictionary of player inputs by <see cref="NetworkManager.PeerId"/>.
    /// </summary>
    public IReadOnlyDictionary<int, PeerInputs> PeersInputs => peersInputs;

    protected IMultiplayerStage MultiplayerStage => stage as IMultiplayerStage ??
        throw new InvalidOperationException("Stage hasn't been set");

    public override void _Ready()
    {
        base._Ready();

        if (NetworkManager.Instance.IsServer)
        {
            GetTree().Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
            NetworkManager.Instance.Connect(nameof(NetworkManager.PlayerJoined), this, nameof(OnPlayerJoined));
            NetworkManager.Instance.Connect(nameof(NetworkManager.PlayerLeft), this, nameof(OnPlayerLeft));
        }
        else if (NetworkManager.Instance.IsClient)
        {
            OnPlayerJoined(NetworkManager.Instance.PeerId);
        }
    }

    public void Tick(float delta)
    {
        var peer = NetworkManager.Instance;

        if (peer.LocalPlayer?.Status == NetworkPlayerStatus.Active)
            DebugOverlays.Instance.ReportUnackedInputs(LocalInputs.Buffer.Count);

        if (peer.IsServer)
            ProcessIncomingInputs();

        var sampled = SampleInput();

        localPlayerInputBuffer[MultiplayerStage.TickCount % Constants.BUFFER_MAX_TICKS] = sampled;

        if (peer.IsClient && peer.LocalPlayer?.Status == NetworkPlayerStatus.Active)
        {
            // Send redundant inputs
            if (MultiplayerStage.TickCount % 2 == 0)
                SendInputs();
        }

        if ((peer.ServerSettings.HasVar("Prediction") && peer.ServerSettings.GetVar<bool>("Prediction")) ||
            peer.IsServer)
        {
            // For client, this is client-side prediction
            ProcessInput(peer.PeerId, sampled);
        }

        PreviousInput = sampled;
    }

    public NetworkInputVars GetInputAtBuffer(uint position)
    {
        return localPlayerInputBuffer[position];
    }

    /// <summary>
    ///   Returns whether the local player's sampled input should be applied (and sent to the server).
    /// </summary>
    protected virtual bool ShouldApplyInput(NetworkInputVars sampled)
    {
        return NetworkManager.Instance.LocalPlayer?.Status == NetworkPlayerStatus.Active &&
            sampled != PreviousInput;
    }

    protected abstract NetworkInputVars SampleInput();

    /// <summary>
    ///   Exclusively server-side.
    /// </summary>
    protected virtual void ProcessIncomingInputs()
    {
        foreach (var peerInputs in peersInputs)
        {
            if (peerInputs.Key == NetworkManager.DEFAULT_SERVER_ID)
                continue;

            var buffer = peerInputs.Value.Buffer;

            var input = peerInputs.Value.LatestSnapshot.Value;

            if (buffer.TryGetValue(MultiplayerStage.TickCount, out NetworkInputVars newestInput))
                input = newestInput;

            ProcessInput(peerInputs.Key, input);
        }
    }

    /// <summary>
    ///   Shared by both server and client.
    /// </summary>
    protected virtual void ProcessInput(int peerId, NetworkInputVars input)
    {
        if (!MultiplayerStage.MultiplayerWorld.TryGetPlayerCharacter(peerId, out NetworkCharacter character))
            return;

        if (NetworkManager.Instance.GetPlayerInfo(peerId)?.Status != NetworkPlayerStatus.Active)
            return;

        character.ApplyNetworkedInput(input);
    }

    private void SendInputs()
    {
        var batch = new NetworkInputBatch
        {
            StartTick = MultiplayerStage.LastReceivedServerTick,
        };

        for (uint tick = MultiplayerStage.LastReceivedServerTick; tick <= MultiplayerStage.TickCount; ++tick)
        {
            batch.Inputs.Add(localPlayerInputBuffer[tick % Constants.BUFFER_MAX_TICKS]);
        }

        var msg = new BytesBuffer();
        batch.NetworkSerialize(msg);

        // We don't send reliably because another will be resent each network tick anyway
        RpcUnreliableId(NetworkManager.DEFAULT_SERVER_ID, nameof(InputReceived), msg.Data);
    }

    private void OnPlayerJoined(int peerId)
    {
        peersInputs[peerId] = new PeerInputs();
    }

    private void OnPlayerLeft(int peerId)
    {
        OnPeerDisconnected(peerId);
    }

    private void OnPeerDisconnected(int peerId)
    {
        peersInputs.Remove(peerId);
    }

    [Remote]
    private void InputReceived(byte[] data)
    {
        if (!NetworkManager.Instance.IsServer)
            return;

        var msg = new BytesBuffer(data);
        var batch = new NetworkInputBatch();
        batch.NetworkDeserialize(msg);

        var sender = GetTree().GetRpcSenderId();

        if (!peersInputs.ContainsKey(sender))
            OnPlayerJoined(sender);

        var player = peersInputs[sender];

        var lastAckedInputTick = player.LatestSnapshot.Key;
        var startPos = lastAckedInputTick >= batch.StartTick ? lastAckedInputTick - batch.StartTick + 1 : 0;

        for (var i = (int)startPos; i < batch.Inputs.Count; ++i)
        {
            var tick = (uint)(batch.StartTick + i);
            player.Buffer[tick] = batch.Inputs[i];
            player.LatestSnapshot = new(tick, batch.Inputs[i]);
        }
    }

    public class PeerInputs
    {
        public Dictionary<uint, NetworkInputVars> Buffer { get; set; } = new();
        public KeyValuePair<uint, NetworkInputVars> LatestSnapshot { get; set; }
    }
}
