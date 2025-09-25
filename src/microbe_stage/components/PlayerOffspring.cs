namespace Components;

using System.Runtime.CompilerServices;
using Arch.Core;

/// <summary>
///   Marks entities as being player reproduced copies
/// </summary>
[JSONDynamicTypeAllowed]
public struct PlayerOffspring
{
    /// <summary>
    ///   Which offspring this is in the number of the player's offspring. Used to detect which is the latest offspring
    /// </summary>
    public int OffspringOrderNumber;
}

public static class PlayerOffspringHelpers
{
    /// <summary>
    ///   A somewhat slow method to find the latest spawned offspring (fine for occasional calls)
    /// </summary>
    /// <returns>The latest offspring or an invalid-entity-value if there are no offspring</returns>
    public static Entity FindLatestSpawnedOffspring(World entitySystem)
    {
        var query = new PlayerOffspringQuery(int.MinValue);

        entitySystem.InlineEntityQuery<PlayerOffspringQuery, PlayerOffspring>(
            new QueryDescription().WithAll<PlayerOffspring>(), ref query);

        return query.Entity;
    }

    private struct PlayerOffspringQuery : IForEachWithEntity<PlayerOffspring>
    {
        public Entity Entity = Entity.Null;
        private int highestFound;

        public PlayerOffspringQuery(int searchStart)
        {
            highestFound = searchStart;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(Entity entity, ref PlayerOffspring offspring)
        {
            var current = offspring.OffspringOrderNumber;

            if (current > highestFound)
            {
                highestFound = current;
                Entity = entity;
            }
        }
    }
}
