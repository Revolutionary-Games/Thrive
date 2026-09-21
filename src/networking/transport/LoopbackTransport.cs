using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
///   In-memory <see cref="ITransport"/> that connects transports living in the same process
/// </summary>
/// <remarks>
///   <para>
///     This is used to test the network feature within the same process, and is not built on top of Godot. It can also
///     simulate latency and packet loss.
///   </para>
///   <para>
///     Servers register themselves under an address string and clients connect by that same string, so a test can
///     use any name it likes as long as both sides agree.
///   </para>
/// </remarks>
public class LoopbackTransport : ITransport
{
    private static readonly Dictionary<string, LoopbackTransport> Servers = new();
    private static readonly Stopwatch Time = Stopwatch.StartNew();

    private readonly Queue<PendingMessage> incoming = new();
    private readonly Queue<TransportEvent> pendingConnectionEvents = new();

    private readonly Dictionary<int, LoopbackTransport> connectedClients = new();

    private readonly object queueLock = new();

    private byte[]? bufferToReturn;

    private LoopbackTransport? connectedServer;
    private string? serverAddress;
    private int ownPeerId;
    private int nextPeerId = NetworkConstants.SERVER_PEER_ID + 1;
    private bool disposed;

    public bool IsRunning { get; private set; }
    public bool IsServer { get; private set; }

    /// <summary>
    ///   Simulated one way delay in seconds applied to all messages sent through this transport
    /// </summary>
    public float SimulatedLatency { get; set; }

    /// <summary>
    ///   Random variation added to <see cref="SimulatedLatency"/>, in seconds
    /// </summary>
    public float SimulatedJitter { get; set; }

    /// <summary>
    ///   Fraction of unreliable messages to drop, from 0 to 1. Reliable messages are never dropped.
    /// </summary>
    public float SimulatedPacketLoss { get; set; }

    /// <summary>
    ///   Random source for the simulated loss and jitter. Seeded explicitly so that a test can reproduce a run.
    /// </summary>
    public Random LossRandom { get; set; } = new(1234);

    private static double CurrentTime => Time.Elapsed.TotalSeconds;

    public bool StartServer(string address, int maxPeers)
    {
        if (IsRunning)
            throw new InvalidOperationException("Transport is already running");

        lock (Servers)
        {
            if (Servers.ContainsKey(address))
                return false;

            Servers[address] = this;
        }

        serverAddress = address;
        IsServer = true;
        IsRunning = true;
        ownPeerId = NetworkConstants.SERVER_PEER_ID;

        return true;
    }

    public bool StartClient(string address)
    {
        if (IsRunning)
            throw new InvalidOperationException("Transport is already running");

        LoopbackTransport? server;

        lock (Servers)
        {
            if (!Servers.TryGetValue(address, out server))
                return false;
        }

        IsServer = false;
        IsRunning = true;
        connectedServer = server;
        ownPeerId = server.RegisterClient(this);

        lock (queueLock)
        {
            pendingConnectionEvents.Enqueue(new TransportEvent
            {
                Type = TransportEventType.PeerConnected,
                PeerId = NetworkConstants.SERVER_PEER_ID,
            });
        }

        return true;
    }

    public void Send(int peerId, ReadOnlySpan<byte> data, NetworkDelivery delivery, NetworkChannel channel)
    {
        if (!IsRunning)
            throw new InvalidOperationException("Transport is not running");

        if (data.Length > NetworkConstants.MAX_MESSAGE_SIZE)
            throw new ArgumentException("Message is too large to send", nameof(data));

        var target = ResolveTarget(peerId);

        if (target == null)
            return;

        if (delivery != NetworkDelivery.ReliableOrdered && SimulatedPacketLoss > 0)
        {
            if (LossRandom.NextDouble() < SimulatedPacketLoss)
                return;
        }

        // The sender's peer ID is what the receiving side sees as the source of this message
        target.Receive(IsServer ? NetworkConstants.SERVER_PEER_ID : ownPeerId, data, channel, CalculateDelay());
    }

    public bool PollEvent(out TransportEvent transportEvent)
    {
        if (bufferToReturn != null)
        {
            ArrayPool<byte>.Shared.Return(bufferToReturn);
            bufferToReturn = null;
        }

        lock (queueLock)
        {
            if (pendingConnectionEvents.Count > 0)
            {
                transportEvent = pendingConnectionEvents.Dequeue();
                return true;
            }

            if (incoming.Count > 0)
            {
                var next = incoming.Peek();

                if (next.DeliveryTime <= CurrentTime)
                {
                    incoming.Dequeue();
                    bufferToReturn = next.Buffer;

                    transportEvent = new TransportEvent
                    {
                        Type = TransportEventType.DataReceived,
                        PeerId = next.SourcePeerId,
                        Data = next.Buffer,
                        DataLength = next.Length,
                        Channel = next.Channel,
                    };

                    return true;
                }
            }
        }

        transportEvent = default(TransportEvent);
        return false;
    }

    public void DisconnectPeer(int peerId)
    {
        if (!IsServer)
            throw new InvalidOperationException("Only a server can disconnect other peers");

        LoopbackTransport? client;

        lock (queueLock)
        {
            if (!connectedClients.TryGetValue(peerId, out client))
                return;

            connectedClients.Remove(peerId);
        }

        client.OnServerDisconnected();

        lock (queueLock)
        {
            pendingConnectionEvents.Enqueue(new TransportEvent
            {
                Type = TransportEventType.PeerDisconnected,
                PeerId = peerId,
            });
        }
    }

    public float GetRoundTripTime(int peerId)
    {
        _ = peerId;

        if (!IsRunning)
            return -1;

        return SimulatedLatency * 2;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        if (IsServer)
        {
            List<LoopbackTransport> clients;

            lock (queueLock)
            {
                clients = new List<LoopbackTransport>(connectedClients.Values);
                connectedClients.Clear();
            }

            for (int i = 0; i < clients.Count; ++i)
            {
                clients[i].OnServerDisconnected();
            }

            if (serverAddress != null)
            {
                lock (Servers)
                {
                    Servers.Remove(serverAddress);
                }

                serverAddress = null;
            }
        }
        else
        {
            connectedServer?.OnClientLeft(ownPeerId);
            connectedServer = null;
        }

        lock (queueLock)
        {
            while (incoming.Count > 0)
            {
                ArrayPool<byte>.Shared.Return(incoming.Dequeue().Buffer);
            }

            pendingConnectionEvents.Clear();
        }

        IsRunning = false;
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

            if (bufferToReturn != null)
            {
                ArrayPool<byte>.Shared.Return(bufferToReturn);
                bufferToReturn = null;
            }
        }

        disposed = true;
    }

    private int RegisterClient(LoopbackTransport client)
    {
        int peerId;

        lock (queueLock)
        {
            peerId = nextPeerId++;
            connectedClients[peerId] = client;

            pendingConnectionEvents.Enqueue(new TransportEvent
            {
                Type = TransportEventType.PeerConnected,
                PeerId = peerId,
            });
        }

        return peerId;
    }

    private void OnClientLeft(int peerId)
    {
        lock (queueLock)
        {
            if (!connectedClients.Remove(peerId))
                return;

            pendingConnectionEvents.Enqueue(new TransportEvent
            {
                Type = TransportEventType.PeerDisconnected,
                PeerId = peerId,
            });
        }
    }

    private void OnServerDisconnected()
    {
        lock (queueLock)
        {
            connectedServer = null;

            pendingConnectionEvents.Enqueue(new TransportEvent
            {
                Type = TransportEventType.PeerDisconnected,
                PeerId = NetworkConstants.SERVER_PEER_ID,
            });
        }
    }

    private LoopbackTransport? ResolveTarget(int peerId)
    {
        if (!IsServer)
        {
            if (peerId != NetworkConstants.SERVER_PEER_ID)
                throw new ArgumentException("A client can only send to the server", nameof(peerId));

            return connectedServer;
        }

        lock (queueLock)
        {
            connectedClients.TryGetValue(peerId, out var client);
            return client;
        }
    }

    private void Receive(int sourcePeerId, ReadOnlySpan<byte> data, NetworkChannel channel, double delay)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
        data.CopyTo(buffer);

        lock (queueLock)
        {
            incoming.Enqueue(new PendingMessage(sourcePeerId, buffer, data.Length, channel, CurrentTime + delay));
        }
    }

    private double CalculateDelay()
    {
        if (SimulatedLatency <= 0 && SimulatedJitter <= 0)
            return 0;

        return SimulatedLatency + LossRandom.NextDouble() * SimulatedJitter;
    }

    private readonly struct PendingMessage(int sourcePeerId, byte[] buffer, int length, NetworkChannel channel,
        double deliveryTime)
    {
        public readonly int SourcePeerId = sourcePeerId;
        public readonly byte[] Buffer = buffer;
        public readonly int Length = length;
        public readonly NetworkChannel Channel = channel;
        public readonly double DeliveryTime = deliveryTime;
    }
}
