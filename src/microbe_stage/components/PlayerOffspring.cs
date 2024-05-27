namespace Components;

using DefaultEcs;

/// <summary>
///   Marks entities as being player reproduced copies
/// </summary>
[JSONDynamicTypeAllowed]
public struct PlayerOffspring
{
    /// <summary>
    ///   Which offspring this is in number of the player's offspring. Used to detect which is the latest offspring
    /// </summary>
    public int OffspringOrderNumber;
}

public static class PlayerOffspringHelpers
{
    /// <summary>
    ///   A pretty slow method to find the latest spawned offspring (fine for occasional calls)
    /// </summary>
    /// <returns>The latest offspring or invalid entity value if there are no offspring</returns>
    public static Entity FindLatestSpawnedOffspring(World entitySystem)
    {
        int highest = int.MinValue;
        Entity result = default;

        foreach (var entity in entitySystem.GetEntities().With<PlayerOffspring>().AsEnumerable())
        {
            var current = entity.Get<PlayerOffspring>().OffspringOrderNumber;

            if (current > highest)
            {
                highest = current;
                result = entity;
            }
        }

        return result;
    }
}
