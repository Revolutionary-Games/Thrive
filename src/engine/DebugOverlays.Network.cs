using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Displays network debug.
/// </summary>
public partial class DebugOverlays
{
    [Export]
    public NodePath NetworkMetricsTextPath = null!;

    private Label networkMetricsText = null!;

    private NetworkManager.PingPongData pingPong;

    private Dictionary<NetworkCharacter, StateView> stateViews = new();

    private int unackedInputsCount;
    private uint tickCount;
    private float tickLead;

    private bool showActualState;

    public bool NetworkDebugVisible
    {
        get => networkDebug.Visible;
        private set
        {
            if (networkCheckbox.Pressed == value)
                return;

            networkCheckbox.Pressed = value;
        }
    }

    public MultiplayerGameWorld? MultiplayerWorld { get; set; }

    public void ReportPingPong(NetworkManager.PingPongData pingPong)
    {
        this.pingPong = pingPong;
    }

    public void ReportUnackedInputs(int count)
    {
        unackedInputsCount = count;
    }

    public void ReportTickCount(uint count)
    {
        tickCount = count;
    }

    public void ReportTickLead(float lead)
    {
        tickLead = lead;
    }

    private void UpdateNetworkDebug(float delta)
    {
        UpdateStateView();

        NetworkManager.Instance.ServerSettings.TryGetVar("TickRate", out int tickRate);

        networkMetricsText.Text = $"Tick count: {tickCount}\nTick rate: {Engine.IterationsPerSecond}\nTick interval multiplier: {NetworkManager.Instance.TickIntervalMultiplier}\nTick offset: {tickLead}\nAverage RTT: {pingPong.AverageRoundTripTime}\nDelta RTT: {pingPong.DeltaRoundTripTime}\nEstimated time offset: {pingPong.EstimatedTimeOffset}\nPacket lost: {pingPong.PacketLost}\nBytes sent (excluding RPCs): {NetworkManager.Instance.RawPacketBytesSent} B/s\nUnacknowledged inputs: {unackedInputsCount}";
    }

    private void UpdateStateView()
    {
        if (MultiplayerWorld == null)
        {
            ClearStateView();
            return;
        }

        if (!showActualState)
            return;

        foreach (var player in MultiplayerWorld.PlayerVars)
        {
            if (!MultiplayerWorld.TryGetPlayerCharacter(player.Key, out NetworkCharacter character))
            {
                var existing = stateViews.Keys.FirstOrDefault(c => c.PeerId == player.Key);
                if (existing != null)
                    stateViews.Remove(existing);

                continue;
            }

            if (!stateViews.TryGetValue(character, out StateView view))
            {
                view = new StateView();
                stateViews[character] = view;
                character.AddChild(view);

                continue;
            }

            if (!view.IsInsideTree())
                continue;

            view.ServerPositionVisual.GlobalTranslation = character.LastReceivedState.Position;
        }
    }

    private void ClearStateView()
    {
        if (MultiplayerWorld != null)
        {
            foreach (var shadow in stateViews)
                shadow.Value.DetachAndQueueFree();
        }

        stateViews.Clear();
    }

    private void OnActualStateViewToggled(bool state)
    {
        if (MultiplayerWorld == null)
            return;

        showActualState = state;

        if (!state)
            ClearStateView();
    }

    private class StateView : Spatial
    {
        public StateView()
        {
            var sphereMesh = new SphereMesh();

            ServerPositionVisual = new MeshInstance
            {
                Mesh = sphereMesh,
                MaterialOverride = new SpatialMaterial
                {
                    FlagsTransparent = true,
                    FlagsUnshaded = true,
                    AlbedoColor = new Color(Colors.Red, 0.5f),
                },
            };

            ClientPositionVisual = new MeshInstance
            {
                Mesh = sphereMesh,
                MaterialOverride = new SpatialMaterial
                {
                    FlagsTransparent = true,
                    FlagsUnshaded = true,
                    AlbedoColor = new Color(Colors.Green, 0.5f),
                },
            };

            AddChild(ServerPositionVisual);
            AddChild(ClientPositionVisual);
        }

        public MeshInstance ServerPositionVisual;
        public MeshInstance ClientPositionVisual;
    }
}
