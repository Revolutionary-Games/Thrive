namespace Components;

using Newtonsoft.Json;

/// <summary>
///   Defines toxin damage dealt by an entity
/// </summary>
[JSONDynamicTypeAllowed]
public struct ToxinDamageSource
{
    /// <summary>
    ///   Scales the damage
    /// </summary>
    public float ToxinAmount;

    public AgentProperties ToxinProperties;

    /// <summary>
    ///   Set to true when this projectile has hit and can't no longer deal damage
    /// </summary>
    public bool ProjectileUsed;

    /// <summary>
    ///   Used by systems internally to know when they have processed the initial adding of a toxin. Should not be
    ///   modified from other places.
    /// </summary>
    [JsonIgnore]
    public bool ProjectileInitialized;
}
