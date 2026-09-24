using System;
using System.Collections.Generic;

/// <summary>
///   Holds what a session replicates: component types and spawn recipes
/// </summary>
/// <remarks>
///   <para>
///     A game mode fills this in when a session starts, which is the main extension point of the networking
///     stack. A different game mode replicates a different set without any change to the layers below.
///   </para>
///   <para>
///     Both sides must register the same things in the same order, as the assigned IDs are positional. A mismatch
///     is caught by the handshake through <see cref="CalculateLayoutHash"/>.
///   </para>
/// </remarks>
public class ReplicationRegistry
{
    private readonly List<IComponentReplicator> replicators = new();
    private readonly List<INetworkSpawnRecipe> spawnRecipes = new();

    private bool sealedForUse;

    public IReadOnlyList<IComponentReplicator> Replicators => replicators;
    public IReadOnlyList<INetworkSpawnRecipe> SpawnRecipes => spawnRecipes;

    /// <summary>
    ///   Adds a component type to replicate and assigns its network ID
    /// </summary>
    public void RegisterComponent(IComponentReplicator replicator)
    {
        if (sealedForUse)
            throw new InvalidOperationException("Cannot register more components after the registry is in use");

        if (replicators.Count >= byte.MaxValue)
            throw new InvalidOperationException("Too many replicated component types");

        replicator.ComponentNetworkId = (byte)replicators.Count;
        replicators.Add(replicator);
    }

    public void RegisterSpawnRecipe(INetworkSpawnRecipe recipe)
    {
        if (sealedForUse)
            throw new InvalidOperationException("Cannot register more spawn recipes after the registry is in use");

        if (spawnRecipes.Count >= ushort.MaxValue)
            throw new InvalidOperationException("Too many spawn recipes");

        recipe.ArchetypeId = (ushort)spawnRecipes.Count;
        spawnRecipes.Add(recipe);
    }

    public IComponentReplicator GetComponentReplicator(byte componentNetworkId)
    {
        if (componentNetworkId >= replicators.Count)
            throw new EndOfNetworkMessageException();

        return replicators[componentNetworkId];
    }

    public INetworkSpawnRecipe GetSpawnRecipe(ushort archetypeId)
    {
        if (archetypeId >= spawnRecipes.Count)
            throw new EndOfNetworkMessageException();

        return spawnRecipes[archetypeId];
    }

    /// <summary>
    ///   Stops further registration. Called when a session starts using this.
    /// </summary>
    public void Seal()
    {
        sealedForUse = true;
    }

    /// <summary>
    ///   Hash of what is registered, exchanged during the handshake so that a client with a different build is
    ///   rejected instead of misreading snapshots
    /// </summary>
    public int CalculateLayoutHash()
    {
        var hash = default(HashCode);
        hash.Add(NetworkConstants.PROTOCOL_VERSION);
        hash.Add(replicators.Count);
        hash.Add(spawnRecipes.Count);

        for (int i = 0; i < replicators.Count; ++i)
        {
            hash.Add(replicators[i].GetType().Name);
        }

        for (int i = 0; i < spawnRecipes.Count; ++i)
        {
            hash.Add(spawnRecipes[i].GetType().Name);
        }

        return hash.ToHashCode();
    }
}
