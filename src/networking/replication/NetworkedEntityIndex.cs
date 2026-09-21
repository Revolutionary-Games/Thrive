using System.Collections.Generic;
using Arch.Core;

/// <summary>
///   Maps network IDs to the local entities that represent them
/// </summary>
public class NetworkedEntityIndex
{
    private readonly Dictionary<uint, Entity> entitiesById = new();

    private uint nextId = 1;

    public IReadOnlyDictionary<uint, Entity> Entities => entitiesById;

    /// <summary>
    ///   Takes the next free network ID. Server only.
    /// </summary>
    public uint GenerateId()
    {
        return nextId++;
    }

    public void Register(uint networkId, in Entity entity)
    {
        entitiesById[networkId] = entity;
    }

    public bool TryGet(uint networkId, out Entity entity)
    {
        return entitiesById.TryGetValue(networkId, out entity);
    }

    public void Remove(uint networkId)
    {
        entitiesById.Remove(networkId);
    }

    public void Clear()
    {
        entitiesById.Clear();
        nextId = 1;
    }
}
