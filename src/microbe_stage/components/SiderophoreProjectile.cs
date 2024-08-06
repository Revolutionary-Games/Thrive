namespace Components;

using DefaultEcs;
using Newtonsoft.Json;

/// <summary>
///   Defines how siderophore projectile behaves
/// </summary>
[JSONDynamicTypeAllowed]
public struct SiderophoreProjectile
{
    /// <summary>
    ///   Sender
    /// </summary>
    public Entity Sender;

    /// <summary>
    ///   Scales the efficiency
    /// </summary>
    public float Amount;

    /// <summary>
    ///   Is already used and to be disposed
    /// </summary>
    public bool IsUsed;

    /// <summary>
    ///   Used by systems internally to know when they have processed the initial adding of a siderophore. Should not be
    ///   modified from other places.
    /// </summary>
    [JsonIgnore]
    public bool ProjectileInitialized;
}
