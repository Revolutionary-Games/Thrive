namespace ThriveTest.Networking.Tests;

using System;
using Xunit;

public class LoopbackTransportTests
{
    [Fact]
    public static void LoopbackTransport_ClientConnectsToServer()
    {
        using var server = new LoopbackTransport();
        using var client = new LoopbackTransport();

        Assert.True(server.StartServer(NextAddress(), 4));
        Assert.True(client.StartClient(ServerAddressOf(server)));

        Assert.Equal(TransportEventType.PeerConnected, NextEvent(server).Type);
        Assert.Equal(TransportEventType.PeerConnected, NextEvent(client).Type);
    }

    [Fact]
    public static void LoopbackTransport_ConnectingToMissingServerFails()
    {
        using var client = new LoopbackTransport();

        Assert.False(client.StartClient("a server that does not exist"));
    }

    [Fact]
    public static void LoopbackTransport_MessageArrivesUnchanged()
    {
        using var server = new LoopbackTransport();
        using var client = new LoopbackTransport();

        server.StartServer(NextAddress(), 4);
        client.StartClient(ServerAddressOf(server));

        var connect = NextEvent(server);

        var payload = new byte[] { 1, 2, 3, 250 };
        client.Send(NetworkConstants.SERVER_PEER_ID, payload, NetworkDelivery.ReliableOrdered,
            NetworkChannel.Control);

        var received = NextEvent(server);

        Assert.Equal(TransportEventType.DataReceived, received.Type);
        Assert.Equal(connect.PeerId, received.PeerId);
        Assert.Equal(NetworkChannel.Control, received.Channel);
        Assert.Equal(payload.Length, received.DataLength);

        for (int i = 0; i < payload.Length; ++i)
        {
            Assert.Equal(payload[i], received.Data![i]);
        }
    }

    /// <summary>
    ///   Simulated loss must only drop unreliable messages, otherwise testing with loss enabled would break the
    ///   handshake and other control traffic
    /// </summary>
    [Fact]
    public static void LoopbackTransport_LossOnlyAffectsUnreliableMessages()
    {
        using var server = new LoopbackTransport();
        using var client = new LoopbackTransport { SimulatedPacketLoss = 1 };

        server.StartServer(NextAddress(), 4);
        client.StartClient(ServerAddressOf(server));
        NextEvent(server);

        var payload = new byte[] { 42 };

        client.Send(NetworkConstants.SERVER_PEER_ID, payload, NetworkDelivery.Unreliable, NetworkChannel.Snapshot);
        Assert.False(server.PollEvent(out _));

        client.Send(NetworkConstants.SERVER_PEER_ID, payload, NetworkDelivery.ReliableOrdered,
            NetworkChannel.Control);
        Assert.Equal(TransportEventType.DataReceived, NextEvent(server).Type);
    }

    [Fact]
    public static void LoopbackTransport_DisconnectReasonReachesTheClient()
    {
        using var server = new LoopbackTransport();
        using var client = new LoopbackTransport();

        server.StartServer(NextAddress(), 4);
        client.StartClient(ServerAddressOf(server));

        var connect = NextEvent(server);
        NextEvent(client);

        server.DisconnectPeer(connect.PeerId, "Server is full");

        var disconnect = NextEvent(client);

        Assert.Equal(TransportEventType.PeerDisconnected, disconnect.Type);
        Assert.Equal("Server is full", disconnect.Reason);
    }

    [Fact]
    public static void LoopbackTransport_ServerSeesClientLeaving()
    {
        using var server = new LoopbackTransport();
        var client = new LoopbackTransport();

        server.StartServer(NextAddress(), 4);
        client.StartClient(ServerAddressOf(server));

        var connect = NextEvent(server);

        client.Dispose();

        var disconnect = NextEvent(server);

        Assert.Equal(TransportEventType.PeerDisconnected, disconnect.Type);
        Assert.Equal(connect.PeerId, disconnect.PeerId);
    }

    /// <summary>
    ///   Each test needs its own address as the loopback server registry is shared by the whole process
    /// </summary>
    private static string NextAddress()
    {
        return "test-server-" + Guid.NewGuid();
    }

    private static string ServerAddressOf(LoopbackTransport server)
    {
        return server.ServerAddress ?? throw new InvalidOperationException("Server was not started");
    }

    /// <summary>
    ///   Polls until an event arrives, failing the test rather than hanging when nothing does
    /// </summary>
    private static TransportEvent NextEvent(LoopbackTransport transport)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);

        while (DateTime.UtcNow < deadline)
        {
            if (transport.PollEvent(out var transportEvent))
                return transportEvent;
        }

        throw new InvalidOperationException("No transport event arrived in time");
    }
}
