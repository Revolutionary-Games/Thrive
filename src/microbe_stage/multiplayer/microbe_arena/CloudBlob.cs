using System.Collections.Generic;
using Godot;
using Environment = System.Environment;

/// <summary>
///   Represents a networkable version of the compound cloud with a predefined form and is deterministic.
/// </summary>
public class CloudBlob : Spatial, INetworkEntity, ISpawned, ITimedLife
{
    private CompoundCloudSystem? clouds;
    private string? cloudsPath;

    private List<Chunk> chunks = new();

    public Compound Compound { get; private set; } = null!;

    public IReadOnlyList<Chunk> Chunks => chunks;

    public int DespawnRadiusSquared { get; set; }

    public float EntityWeight => 1.0f;

    public AliveMarker AliveMarker => new();

    public Spatial EntityNode => this;

    public string ResourcePath => "res://src/microbe_stage/multiplayer/microbe_arena/CloudBlob.tscn";

    public uint NetworkEntityId { get; set; }

    public float TimeToLiveRemaining { get; set; }

    public void Init(CompoundCloudSystem clouds, Compound compound, Vector3 position, float radius, float amount)
    {
        this.clouds = clouds;
        Compound = compound;
        Translation = position;

        int resolution = Settings.Instance.CloudResolution;

        // Circle drawing algorithm borrowed from https://www.redblobgames.com/grids/circle-drawing/

        var center = new Int2((int)position.x, (int)position.z);
        var top = Mathf.CeilToInt(center.y - radius);
        var bottom = Mathf.FloorToInt(center.y + radius);
        var left = Mathf.CeilToInt(center.x - radius);
        var right = Mathf.FloorToInt(center.x + radius);

        var noise = new FastNoiseLite(Environment.TickCount);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFrequency(0.015f);
        noise.SetFractalOctaves(5);
        noise.SetDomainWarpAmp(50.0f);

        for (int y = top; y <= bottom; ++y)
        {
            for (int x = left; x <= right; ++x)
            {
                var dx = center.x - x;
                var dy = center.y - y;
                var distanceSqr = dx * dx + dy * dy;

                var weight = Mathf.InverseLerp(radius, radius * 0.8f, Mathf.Sqrt(distanceSqr));
                var noisePos = Mathf.Clamp(noise.GetNoise(x, y), 0, 1);
                chunks.Add(new Chunk(new Vector3(x + resolution, 0, y + resolution), amount * weight * noisePos));
            }
        }
    }

    public override void _Ready()
    {
        // Kind of hackish I guess??
        clouds ??= GetNode<CompoundCloudSystem>(cloudsPath);
        cloudsPath ??= clouds.GetPath();

        foreach (var chunk in Chunks)
        {
            clouds.AddCloud(Compound, chunk.InitialAmount, chunk.Position);
        }
    }

    public void NetworkTick(float delta)
    {
    }

    public void NetworkSerialize(BytesBuffer buffer)
    {
        // For now just hope everything sync nicely by themselves on the client side.
        // Can we even feasibly replicate the amount in the clouds anyway? ...not unless clever tricks were employed.
    }

    public void NetworkDeserialize(BytesBuffer buffer)
    {
    }

    public void PackSpawnState(BytesBuffer buffer)
    {
        buffer.Write((byte)SimulationParameters.Instance.CompoundToIndex(Compound));

        buffer.Write((short)Chunks.Count);
        foreach (var chunk in Chunks)
        {
            chunk.InitialAmount = clouds!.AmountAvailable(Compound, chunk.Position, 1.0f);

            var chunkMsg = new BytesBuffer();
            chunk.NetworkSerialize(chunkMsg);
            buffer.Write(chunkMsg);
        }

        buffer.Write(GlobalTranslation.x);
        buffer.Write(GlobalTranslation.z);
        buffer.Write(cloudsPath!);
    }

    public void OnRemoteSpawn(BytesBuffer buffer, GameProperties currentGame)
    {
        Compound = SimulationParameters.Instance.IndexToCompound(buffer.ReadByte());

        var chunksCount = buffer.ReadInt16();
        for (int i = 0; i < chunksCount; ++i)
        {
            var packed = buffer.ReadBuffer();
            chunks.Add(new Chunk(packed));
        }

        Translation = new Vector3(buffer.ReadSingle(), 0, buffer.ReadSingle());
        cloudsPath = buffer.ReadString();
    }

    public void OnTimeOver()
    {
        this.DestroyDetachAndQueueFree();
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    private void OnTreeExiting()
    {
        foreach (var chunk in Chunks)
        {
            clouds?.TakeCompound(Compound, chunk.Position, 1.0f);
        }
    }

    public class Chunk : INetworkSerializable
    {
        public Vector3 Position;
        public float InitialAmount;

        public Chunk(BytesBuffer buffer)
        {
            NetworkDeserialize(buffer);
        }

        public Chunk(Vector3 position, float amount)
        {
            Position = position;
            InitialAmount = amount;
        }

        public void NetworkSerialize(BytesBuffer buffer)
        {
            buffer.Write(Position.x);
            buffer.Write(Position.z);
            buffer.Write(InitialAmount);
        }

        public void NetworkDeserialize(BytesBuffer buffer)
        {
            Position = new Vector3(buffer.ReadSingle(), Position.y, buffer.ReadSingle());
            InitialAmount = buffer.ReadSingle();
        }
    }
}
