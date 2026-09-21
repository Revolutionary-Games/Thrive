namespace ThriveTest.Networking.Tests;

using System;
using Xunit;

public class NetworkSessionTests
{
    [Fact]
    public static void NetworkSession_ClientCompletesHandshake()
    {
        using var setup = new SessionPair();

        setup.PumpUntil(() => setup.Client.State == NetworkSession.NetworkSessionState.Active);

        Assert.Equal(NetworkSession.NetworkSessionState.Active, setup.Client.State);
        Assert.NotEqual(NetworkConstants.INVALID_PEER_ID, setup.Client.LocalPeerId);

        // The server knows the client as a player, under the name it asked for
        Assert.Single(setup.Server.Peers);
        Assert.True(setup.Server.Peers[setup.Client.LocalPeerId].HandshakeComplete);
        Assert.Equal("Tester", setup.Server.Peers[setup.Client.LocalPeerId].Identity.Name);
    }

    [Fact]
    public static void NetworkSession_ServerRejectsWhenFull()
    {
        using var setup = new SessionPair(maxPlayers: 0);

        setup.PumpUntil(() => setup.Client.State == NetworkSession.NetworkSessionState.Failed);

        Assert.Equal(NetworkSession.NetworkSessionState.Failed, setup.Client.State);
        Assert.Equal("Server is full", setup.Client.FailureReason);
        Assert.Empty(setup.Server.Peers);
    }

    /// <summary>
    ///   A client built against a different set of replicated components must be rejected, as it would otherwise
    ///   misread every snapshot
    /// </summary>
    [Fact]
    public static void NetworkSession_MismatchedReplicationLayoutIsRejected()
    {
        using var setup = new SessionPair(clientGameMode: new StubGameMode("microbe_arena", 4, extraSpawnRecipe: true));

        setup.PumpUntil(() => setup.Client.State == NetworkSession.NetworkSessionState.Failed);

        Assert.Equal("Different game mode or build", setup.Client.FailureReason);
    }

    [Fact]
    public static void NetworkSession_DifferentGameModeIsRejected()
    {
        using var setup = new SessionPair(clientGameMode: new StubGameMode("something_else", 4));

        setup.PumpUntil(() => setup.Client.State == NetworkSession.NetworkSessionState.Failed);

        Assert.Equal("Different game mode or build", setup.Client.FailureReason);
    }

    [Fact]
    public static void NetworkSession_ChatIsRelayedWithTheRealSenderId()
    {
        using var setup = new SessionPair();
        setup.PumpUntil(() => setup.Client.State == NetworkSession.NetworkSessionState.Active);

        int receivedFrom = NetworkConstants.INVALID_PEER_ID;
        string? receivedMessage = null;

        setup.Server.ChatReceived += (peerId, message) =>
        {
            receivedFrom = peerId;
            receivedMessage = message;
        };

        setup.Client.SendChat("hello");
        setup.PumpUntil(() => receivedMessage != null);

        Assert.Equal("hello", receivedMessage);
        Assert.Equal(setup.Client.LocalPeerId, receivedFrom);
    }

    [Fact]
    public static void NetworkSession_KickReasonReachesTheClient()
    {
        using var setup = new SessionPair();
        setup.PumpUntil(() => setup.Client.State == NetworkSession.NetworkSessionState.Active);

        string? disconnectReason = null;
        setup.Client.Disconnected += reason => disconnectReason = reason;

        setup.Server.KickPeer(setup.Client.LocalPeerId, "Behave yourself");
        setup.PumpUntil(() => disconnectReason != null);

        Assert.Equal("Behave yourself", disconnectReason);
        Assert.Equal(NetworkSession.NetworkSessionState.Failed, setup.Client.State);
    }

    /// <summary>
    ///   Minimal game mode so that the session layer can be tested without any real gameplay
    /// </summary>
    private class StubGameMode(string internalName, int maxPlayers, bool extraSpawnRecipe = false)
        : INetworkedGameMode
    {
        public string InternalName { get; } = internalName;
        public MatchState CurrentState => MatchState.Lobby;
        public int MaxPlayers { get; } = maxPlayers;
        public bool AllowsJoiningInProgress => true;

        public void RegisterReplication(ReplicationRegistry registry)
        {
            if (extraSpawnRecipe)
                registry.RegisterSpawnRecipe(new StubSpawnRecipe());
        }

        public IInterestManager CreateInterestManager()
        {
            return new ReplicateEverythingInterestManager();
        }

        public IInputPayload CreateInputPayload()
        {
            throw new NotSupportedException("Stub mode has no input");
        }

        public void UpdateMatch(float delta)
        {
        }

        public void OnPlayerJoined(int peerId, in PlayerIdentity identity)
        {
        }

        public void OnPlayerLeft(int peerId)
        {
        }
    }

    private class StubSpawnRecipe : INetworkSpawnRecipe
    {
        public ushort ArchetypeId { get; set; }

        public void WriteSpawnData(in Arch.Core.Entity entity, NetworkWriter writer)
        {
            throw new NotSupportedException();
        }

        public Arch.Core.Entity Spawn(NetworkReader reader, in Components.NetworkEntity networkEntity)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    ///   A server and a client joined over loopback, pumped together
    /// </summary>
    private class SessionPair : IDisposable
    {
        private readonly LoopbackTransport serverTransport = new();
        private readonly LoopbackTransport clientTransport = new();

        public SessionPair(int maxPlayers = 4, INetworkedGameMode? clientGameMode = null)
        {
            var address = "session-test-" + Guid.NewGuid();

            Server = new NetworkSession(serverTransport, new GuestIdentityProvider(),
                new StubGameMode("microbe_arena", maxPlayers));
            Server.StartServer(address, "Host");

            Client = new NetworkSession(clientTransport, new GuestIdentityProvider(),
                clientGameMode ?? new StubGameMode("microbe_arena", maxPlayers));
            Client.StartClient(address, "Tester");
        }

        public NetworkSession Server { get; }
        public NetworkSession Client { get; }

        /// <summary>
        ///   Runs both sides until the condition holds, failing the test rather than hanging
        /// </summary>
        public void PumpUntil(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);

            while (DateTime.UtcNow < deadline)
            {
                Server.Process(1 / 60.0f);
                Client.Process(1 / 60.0f);

                if (condition.Invoke())
                    return;
            }

            throw new InvalidOperationException("Condition was not reached in time");
        }

        public void Dispose()
        {
            Server.Dispose();
            Client.Dispose();
        }
    }
}
