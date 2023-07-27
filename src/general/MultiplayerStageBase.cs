using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Base stage for the stages where the player controls a single creature, supports online gameplay.
/// </summary>
/// <typeparam name="TPlayer">The type of the player object</typeparam>
/// <remarks>
///   <para>
///     TODO: perhaps this can be combined into the normal StageBase to remove redundancies and
///     to make singleplayer to multiplayer seamless.
///   </para>
/// </remarks>
public abstract class MultiplayerStageBase<TPlayer> : CreatureStageBase<TPlayer>
    where TPlayer : class, INetEntity
{
    private float networkTick;

    private uint netEntitiesIdCounter;

    /// <summary>
    ///   Dictionary of players that has joined the game.
    /// </summary>
    private readonly Dictionary<int, EntityReference<TPlayer>> players = new();
    private Dictionary<int, Species> playerSpeciesList = new();

    protected readonly Dictionary<int, float> playerRespawnTimers = new();

    /// <summary>
    ///   Dictionary of players that has joined the game.
    /// </summary>
    public IReadOnlyDictionary<int, EntityReference<TPlayer>> Players => players;

    public IReadOnlyDictionary<int, Species> PlayerSpeciesList => playerSpeciesList;

    public override void _Ready()
    {
        base._Ready();

        GetTree().Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
        GetTree().Connect("server_disconnected", this, nameof(OnServerDisconnected));

        NetworkManager.Instance.Connect(nameof(NetworkManager.PlayerJoined), this, nameof(RegisterPlayer));
        NetworkManager.Instance.Connect(nameof(NetworkManager.PlayerLeft), this, nameof(UnRegisterPlayer));
        NetworkManager.Instance.Connect(nameof(NetworkManager.Kicked), this, nameof(OnKicked));

        if (IsNetworkMaster())
        {
            SpawnHelpers.OnNetEntitySpawned = OnNetEntitySpawned;
            SpawnHelpers.OnNetEntityDespawned = OnNetEntityDespawned;
        }
    }

    public override void _Process(float delta)
    {
        networkTick += delta;

        if (!GetTree().HasNetworkPeer() || !GetTree().IsNetworkServer() || !IsNetworkMaster())
            return;

        var network = NetworkManager.Instance;

        if (!network.IsDedicated && network.Player!.Status != NetPlayerStatus.Active)
            return;

        if (network.GameInSession && networkTick > network.UpdateInterval)
        {
            NetworkUpdateGameState(delta + networkTick);
            NetworkHandleRespawns(delta + networkTick);
            networkTick = 0;
        }
    }

    public override void OnFinishLoading(Save save)
    {
    }

    protected override void OnGameStarted()
    {
        if (GetTree().IsNetworkServer())
        {
            if (!NetworkManager.Instance.IsDedicated)
                RegisterPlayer(GetTree().GetNetworkUniqueId());

            NotifyGameReady();
        }
    }

    protected virtual void NetworkUpdateGameState(float delta)
    {
        for (int i = 0; i < rootOfDynamicallySpawned.GetChildCount(); ++i)
        {
            var child = rootOfDynamicallySpawned.GetChild(i);

            if (child is not INetEntity netObject)
                continue;

            foreach (var player in NetworkManager.Instance.PlayerList)
            {
                if (player.Key == GetTree().GetNetworkUniqueId() || player.Value.Status != NetPlayerStatus.Active)
                    continue;

                RpcUnreliableId(player.Key, nameof(NetworkUpdateEntityState), netObject.NetEntityId, netObject.PackState());
            }
        }
    }

    [Puppet]
    protected virtual void NetworkUpdateEntityState(int entityId, Dictionary<string, string> data)
    {
        for (int i = 0; i < rootOfDynamicallySpawned.GetChildCount(); ++i)
        {
            var child = rootOfDynamicallySpawned.GetChild(i);

            if (child is INetEntity netEntity && netEntity.NetEntityId == entityId)
            {
                netEntity.NetSyncEveryFrame(data);
                break;
            }
        }
    }

    protected virtual void NetworkHandleRespawns(float delta)
    {
        foreach (var player in NetworkManager.Instance.PlayerList)
        {
            if (player.Value.Status != NetPlayerStatus.Active)
                continue;

            if (players.ContainsKey(player.Key) || !playerRespawnTimers.ContainsKey(player.Key))
                continue;

            var diff = playerRespawnTimers[player.Key] - delta;
            playerRespawnTimers[player.Key] = diff;

            // Respawn the player once the timer is up
            if (playerRespawnTimers[player.Key] <= 0)
            {
                SpawnPlayer(player.Key);
            }
        }
    }

    protected virtual void OnNetEntityReplicated(INetEntity entity)
    {
        rootOfDynamicallySpawned.AddChild(entity.EntityNode);

        if (int.TryParse(entity.EntityNode.Name, out int parsedId) && parsedId == GetTree().GetNetworkUniqueId())
            OnLocalPlayerSpawned((TPlayer)entity);

        entity.OnReplicated();
    }

    /// <summary>
    ///   Called when entity with the given id needs to be destroyed.
    /// </summary>
    protected virtual void OnNetEntityDestroy(INetEntity entity)
    {
        if (int.TryParse(entity.EntityNode.Name, out int parsedId) && parsedId == GetTree().GetNetworkUniqueId())
            OnLocalPlayerDespawn();

        entity.DestroyDetachAndQueueFree();
    }

    protected virtual void OnLocalPlayerSpawned(TPlayer player)
    {
        Player = player;
        spawnedPlayer = true;
    }

    protected virtual void OnLocalPlayerDespawn()
    {
        Player = null;
    }

    protected void SpawnPlayer(int peerId)
    {
        if (!IsNetworkMaster())
            return;

        if (!players.ContainsKey(peerId))
        {
            if (!HandlePlayerSpawn(peerId, out TPlayer? spawned))
                return;

            if (peerId == GetTree().GetNetworkUniqueId())
                OnLocalPlayerSpawned(spawned!);

            players.Add(peerId, new EntityReference<TPlayer>(spawned!));
            playerRespawnTimers[peerId] = Constants.PLAYER_RESPAWN_TIME;
        }
    }

    protected void DespawnPlayer(int peerId)
    {
        if (!IsNetworkMaster())
            return;

        if (players.TryGetValue(peerId, out EntityReference<TPlayer> peer))
        {
            if (peer.Value != null)
            {
                var entityId = peer.Value.NetEntityId;

                if (!HandlePlayerDespawn(peer.Value))
                    return;

                if (peerId == GetTree().GetNetworkUniqueId())
                    OnLocalPlayerDespawn();
            }

            players.Remove(peerId);
        }
    }

    /// <summary>
    ///   Returns true if successfully spawned.
    /// </summary>
    protected abstract bool HandlePlayerSpawn(int peerId, out TPlayer? spawned);

    /// <summary>
    ///   Returns true if successfully despawned.
    protected abstract bool HandlePlayerDespawn(TPlayer removed);

    [RemoteSync]
    protected override void SpawnPlayer()
    {
        SpawnPlayer(GetTree().GetNetworkUniqueId());
    }

    protected override void AutoSave()
    {
    }

    protected override void PerformQuickSave()
    {
    }

    private void RegisterPlayer(int peerId)
    {
        if (players.ContainsKey(peerId) || !GetTree().IsNetworkServer())
            return;

        if (!playerSpeciesList.TryGetValue(peerId, out Species species))
        {
            species = GameWorld.CreateMutatedSpecies(GameWorld.PlayerSpecies);
            playerSpeciesList[peerId] = species;
            Rpc(nameof(SyncPlayerSpeciesList), ThriveJsonConverter.Instance.SerializeObject(playerSpeciesList));
        }

        SpawnPlayer(peerId);

        if (peerId != GetTree().GetNetworkUniqueId())
        {
            var entities = DynamicEntities.OfType<INetEntity>().ToList();
            foreach (var entity in entities)
            {
                var oldName = entity.EntityNode.Name;

                // Obvious hack. Why does the json serializer keeps the entity's parent anyway,
                // now this need to be like this as I don't bother finding out
                rootOfDynamicallySpawned.RemoveChild(entity.EntityNode);

                // TODO: replace this with something far more efficient!!
                RpcId(peerId, nameof(ReplicateSpawnedEntity), ThriveJsonConverter.Instance.SerializeObject(entity), entities.Count);

                rootOfDynamicallySpawned.AddChild(entity.EntityNode);
                entity.EntityNode.Name = oldName;
            }
        }
    }

    private void UnRegisterPlayer(int peerId)
    {
        if (!GetTree().IsNetworkServer())
            return;

        if (playerSpeciesList.Remove(peerId))
            Rpc(nameof(SyncPlayerSpeciesList), ThriveJsonConverter.Instance.SerializeObject(playerSpeciesList));

        playerRespawnTimers.Remove(peerId);

        DespawnPlayer(peerId);
    }

    [Puppet]
    private void SyncPlayerSpeciesList(string data)
    {
        playerSpeciesList = ThriveJsonConverter.Instance.DeserializeObject<Dictionary<int, Species>>(data)!;
    }

    [Puppet]
    private void ReplicateSpawnedEntity(string data, int entitiesCount = -1)
    {
        var deserialized = ThriveJsonConverter.Instance.DeserializeObject<INetEntity>(data);
        if (deserialized == null)
            return;

        OnNetEntityReplicated(deserialized);

        ++netEntitiesIdCounter;

        if (entitiesCount > -1 && netEntitiesIdCounter == entitiesCount)
            NotifyGameReady();
    }

    [Puppet]
    private void DestroySpawnedEntity(uint entityId)
    {
        for (int i = rootOfDynamicallySpawned.GetChildCount() - 1; i >= 0; --i)
        {
            var child = rootOfDynamicallySpawned.GetChild(i);

            if (child is INetEntity netEntity && netEntity.NetEntityId == entityId)
            {
                OnNetEntityDestroy(netEntity);
                break;
            }
        }
    }

    private void OnNetEntitySpawned(INetEntity spawned)
    {
        spawned.NetEntityId = ++netEntitiesIdCounter;

        foreach (var player in NetworkManager.Instance.PlayerList)
        {
            if (player.Key == GetTree().GetNetworkUniqueId() || player.Value.Status != NetPlayerStatus.Active)
                continue;

            // TODO: replace this with something far more efficient!!
            RpcId(player.Key, nameof(ReplicateSpawnedEntity), ThriveJsonConverter.Instance.SerializeObject(spawned), -1);
        }
    }

    private void OnNetEntityDespawned(uint id)
    {
        Rpc(nameof(DestroySpawnedEntity), id);
    }

    private void OnPeerDisconnected(int peerId)
    {
        if (GetTree().IsNetworkServer())
            UnRegisterPlayer(peerId);
    }

    private void OnServerDisconnected()
    {
        var menu = SceneManager.Instance.ReturnToMenu();
        menu.OpenMultiplayerMenu(MultiplayerGUI.Submenu.Main);
    }

    private void OnKicked(string reason)
    {
        var menu = SceneManager.Instance.ReturnToMenu();
        menu.OpenMultiplayerMenu(MultiplayerGUI.Submenu.Main);
        menu.ShowKickedDialog(reason);
    }
}
