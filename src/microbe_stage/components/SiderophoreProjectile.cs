namespace Components;

using DefaultEcs;
using Newtonsoft.Json;

/// <summary>
///   Defines toxin damage dealt by an entity
/// </summary>
[JSONDynamicTypeAllowed]
public struct SiderophoreProjectile
{
    /// <summary>
    ///   Scales the efficiency
    /// </summary>
    public float Amount;

    /// <summary>
    ///   Scales the efficiency
    /// </summary>
    public bool IsUsed;

    /// <summary>
    ///   Sender
    /// </summary>
    public Entity Sender;

    /// <summary>
    ///   Used by systems internally to know when they have processed the initial adding of a toxin. Should not be
    ///   modified from other places.
    /// </summary>
    [JsonIgnore]
    public bool ProjectileInitialized;
}
