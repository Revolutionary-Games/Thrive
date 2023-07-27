using System;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   The networking part of Microbe class for state synchronization.
/// </summary>
public partial class Microbe
{
    private MeshInstance tagBox = null!;

    private string? cloudSystemPath;

    private float lastHitpoints;

    [Flags]
    public enum InputFlag : byte
    {
        EmitToxin = 1 << 0,
        SecreteSlime = 1 << 1,
        Engulf = 1 << 2,
    }

    public MultiplayerGameWorld MultiplayerGameWorld => (MultiplayerGameWorld)GameWorld;

    public override string ResourcePath => "res://src/microbe_stage/Microbe.tscn";

    public Action<int>? OnNetworkDeathFinished { get; set; }

    public Action<int, int, string>? OnKilledByAnotherPlayer { get; set; }

    public override void SetupNetworkCharacter()
    {
        if (!IsInsideTree())
            return;

        base.SetupNetworkCharacter();

        Name = PeerId.ToString(CultureInfo.CurrentCulture);

        Compounds.LockInputAndOutput = NetworkManager.Instance.IsClient;

        // Kind of hackish I guess??
        if (cloudSystemPath != null)
            cloudSystem ??= GetNode<CompoundCloudSystem>(cloudSystemPath);
    }

    public override void NetworkSerialize(BytesBuffer buffer)
    {
        // TODO: Find a way to compress this even further, look into delta encoding

        // TODO: Don't sync transform when engulfed

        base.NetworkSerialize(buffer);

        buffer.Write((byte)Compounds.UsefulCompounds.Count());
        foreach (var compound in Compounds.UsefulCompounds)
            buffer.Write((byte)SimulationParameters.Instance.CompoundToIndex(compound));

        buffer.Write(Compounds.Capacity);

        buffer.Write((byte)Compounds.Compounds.Count);
        foreach (var compound in compounds.Compounds)
        {
            buffer.Write((byte)SimulationParameters.Instance.CompoundToIndex(compound.Key));
            buffer.Write(compound.Value);
        }

        requiredCompoundsForBaseReproduction.TryGetValue(ammonia, out float ammoniaAmount);
        requiredCompoundsForBaseReproduction.TryGetValue(phosphates, out float phosphatesAmount);
        buffer.Write(ammoniaAmount);
        buffer.Write(phosphatesAmount);

        buffer.Write(Hitpoints);
        buffer.Write((byte)State);
        buffer.Write(DigestedAmount);

        var bools = new bool[3]
        {
            HostileEngulfer.Value != null,
            PhagocytosisStep is PhagocytosisPhase.Ingestion or PhagocytosisPhase.Ingested or
                PhagocytosisPhase.Digested,
            Dead,
        };

        buffer.Write(bools.ToByte());

        if (HostileEngulfer.Value != null)
            buffer.Write(HostileEngulfer.Value.NetworkEntityId);
    }

    public override void NetworkDeserialize(BytesBuffer buffer)
    {
        base.NetworkDeserialize(buffer);

        Compounds.ClearUseful();
        var usefulCompoundsCount = buffer.ReadByte();
        for (int i = 0; i < usefulCompoundsCount; ++i)
        {
            Compounds.SetUseful(SimulationParameters.Instance.IndexToCompound(buffer.ReadByte()));
        }

        Compounds.Capacity = buffer.ReadSingle();

        var compoundsCount = buffer.ReadByte();
        for (int i = 0; i < compoundsCount; ++i)
        {
            var compound = SimulationParameters.Instance.IndexToCompound(buffer.ReadByte());
            Compounds.Compounds[compound] = buffer.ReadSingle();
        }

        requiredCompoundsForBaseReproduction[ammonia] = buffer.ReadSingle();
        requiredCompoundsForBaseReproduction[phosphates] = buffer.ReadSingle();

        Hitpoints = buffer.ReadSingle();
        State = (MicrobeState)buffer.ReadByte();
        DigestedAmount = buffer.ReadSingle();

        var bools = buffer.ReadByte();

        if (bools.ToBoolean(0) && MultiplayerGameWorld.TryGetNetworkEntity(buffer.ReadUInt32(),
            out INetworkEntity entity) && entity is Microbe engulfer)
        {
            if (bools.ToBoolean(1))
            {
                engulfer.IngestEngulfable(this);

                // TODO: Floating chunks doesn't seem to get visually engulfed client-side
                // TODO: Engulfee inflates back after ingested but this doesn't happen in singleplayer, why?
            }
            else
            {
                engulfer.EjectEngulfable(this);
            }
        }
        else
        {
            HostileEngulfer.Value?.EjectEngulfable(this);
        }

        if (bools.ToBoolean(2))
            Kill();

        if (Hitpoints < lastHitpoints)
            Flash(1.0f, new Color(1, 0, 0, 0.5f), 1);

        lastHitpoints = Hitpoints;
    }

    public override void PackSpawnState(BytesBuffer buffer)
    {
        base.PackSpawnState(buffer);

        buffer.Write(randomSeed);
        buffer.Write(cloudSystem!.GetPath());

        // Sending 2-byte unsigned int... means our max deserialized organelle count will be 65535
        buffer.Write((ushort)organelles!.Count);
        foreach (var organelle in organelles!)
            organelle.NetworkSerialize(buffer);

        buffer.Write(allOrganellesDivided);
    }

    public override void OnRemoteSpawn(BytesBuffer buffer, GameProperties currentGame)
    {
        base.OnRemoteSpawn(buffer, currentGame);

        randomSeed = buffer.ReadInt32();
        cloudSystemPath = buffer.ReadString();

        AddToGroup(Constants.AI_TAG_MICROBE);
        AddToGroup(Constants.PROCESS_GROUP);
        AddToGroup(Constants.RUNNABLE_MICROBE_GROUP);

        Init(null!, null!, currentGame, true);

        ApplySpecies(MultiplayerGameWorld.GetSpecies((uint)PeerId));

        organelles?.Clear();
        var organellesCount = buffer.ReadUInt16();
        for (int i = 0; i < organellesCount; ++i)
        {
            if (organelles == null)
                break;

            var organelle = new PlacedOrganelle();
            organelle.NetworkDeserialize(buffer);
            organelles.Add(organelle);
        }

        allOrganellesDivided = buffer.ReadBoolean();
    }

    private void UpdateNametag()
    {
        if (!NetworkManager.Instance.IsMultiplayer)
            return;

        var tagBoxMesh = (QuadMesh)tagBox.Mesh;
        var tagBoxMaterial = (SpatialMaterial)tagBox.MaterialOverride;

        var tag = tagBox.GetChild<Label3D>(0);

        tagBox.Visible = !Dead && PeerId != NetworkManager.Instance.PeerId;

        var name = NetworkManager.Instance.GetPlayerInfo(PeerId)?.Nickname;
        tag.Text = name;

        tagBoxMesh.Size = tag.Font.GetStringSize(name) * tag.PixelSize * 1.2f;
        tagBoxMaterial.RenderPriority = RenderPriority + 1;
        tag.RenderPriority = tagBoxMaterial.RenderPriority + 1;

        // Always offset tag above the membrane (Z - axis)
        tagBox.GlobalTranslation = GlobalTranslation + Vector3.Forward * (Radius + 1.0f);
    }
}
