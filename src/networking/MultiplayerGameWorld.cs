using System;
using System.Collections.Generic;

/// <summary>
///   <inheritdoc /> Contains the necessary informations for multiplayer.
/// </summary>
public class MultiplayerGameWorld : GameWorld
{
    private readonly Dictionary<uint, EntityReference<INetworkEntity>> entities = new();

    private readonly List<uint> entityIds = new();

    private uint entityIdCounter;

    public MultiplayerGameWorld(WorldGenerationSettings settings) : base(settings)
    {
    }

    public MultiplayerGameWorld(PatchMap map)
    {
        PlayerSpecies = CreatePlayerSpecies();

        if (!PlayerSpecies.PlayerSpecies)
            throw new Exception("PlayerSpecies flag for being player species is not set");

        Map = map;

        // Apply initial populations
        Map.UpdateGlobalPopulations();
    }

    /// <summary>
    ///   Stores information of registered players in the current game session.
    /// </summary>
    public Dictionary<int, NetworkPlayerVars> PlayerVars { get; set; } = new();

    /// <summary>
    ///   Stores references to all networked entities.
    /// </summary>
    public IReadOnlyDictionary<uint, EntityReference<INetworkEntity>> Entities => entities;

    public IReadOnlyList<uint> EntityIDs => entityIds;

    public int EntityCount => entities.Count;

    public void ClearMultiplayer()
    {
        PlayerVars.Clear();
        entities.Clear();
    }

    /// <summary>
    ///   Make the game world track the given entity.
    /// </summary>
    /// <param name="id">An explicit ID to be given to the entity.</param>
    /// <param name="entity">The entity itself.</param>
    public void RegisterNetworkEntity(uint id, INetworkEntity entity)
    {
        entity.NetworkEntityId = id;
        entities[id] = new EntityReference<INetworkEntity>(entity);

        if (!entityIds.Contains(id))
            entityIds.Add(id);
    }

    /// <summary>
    ///   Make the game world track the given entity.
    /// </summary>
    /// <param name="entity">The entity itself.</param>
    /// <returns>The entity's incrementally assigned sequential ID.</returns>
    public uint RegisterNetworkEntity(INetworkEntity entity)
    {
        RegisterNetworkEntity(++entityIdCounter, entity);
        return entityIdCounter;
    }

    /// <summary>
    ///   Untrack an entity from the game world.
    /// </summary>
    /// <param name="id">The entity's ID.</param>
    public void UnregisterNetworkEntity(uint id)
    {
        entities.Remove(id);
        entityIds.Remove(id);
    }

    /// <summary>
    ///   Gets the networked entity associated with the given <paramref name="entityId"/>.
    /// </summary>
    /// <returns>True if value exists and is not freed; false otherwise.</returns>
    public bool TryGetNetworkEntity(uint entityId, out INetworkEntity entity)
    {
        try
        {
            var retrieved = entities[entityId].Value;

            if (retrieved == null)
                throw new Exception();

            entity = retrieved;
        }
        catch
        {
            entity = null!;
            return false;
        }

        return true;
    }

    /// <summary>
    ///   Gets the player-character associated with the given <paramref name="peerId"/>.
    /// </summary>
    /// <returns>True if value exists, not freed, and is a <see cref="NetworkCharacter"/>; false otherwise.</returns>
    public bool TryGetPlayerCharacter(int peerId, out NetworkCharacter player)
    {
        player = default!;

        if (!PlayerVars.TryGetValue(peerId, out NetworkPlayerVars vars))
            return false;

        if (!TryGetNetworkEntity(vars.EntityId, out INetworkEntity entity))
            return false;

        if (entity is not NetworkCharacter casted)
            return false;

        player = casted;
        return true;
    }

    public void SetSpecies(int peerId, Species species)
    {
        worldSpecies[(uint)peerId] = species;
    }
}
