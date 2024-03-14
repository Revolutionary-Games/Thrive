namespace Components;

/// <summary>
///   Limit for how many entities can exist in the configured group. Requires <see cref="WorldPosition"/> as
///   despawning is done far away from the player.
/// </summary>
[JSONDynamicTypeAllowed]
public struct CountLimited
{
    public LimitGroup Group;
}
