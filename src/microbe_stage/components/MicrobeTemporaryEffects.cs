namespace Components;

using Newtonsoft.Json;

/// <summary>
///   Has some temporary effects that can affect microbes (like toxins)
/// </summary>
[JSONDynamicTypeAllowed]
public struct MicrobeTemporaryEffects
{
    /// <summary>
    ///   How long this microbe will have a base movement speed penalty (if 0 or less not currently debuffed).
    ///   Must set <see cref="StateApplied"/> to false after modification.
    /// </summary>
    public float SpeedDebuffDuration;

    /// <summary>
    ///   How long this microbe will have ATP generation debuff
    /// </summary>
    public float ATPDebuffDuration;

    /// <summary>
    ///   False when something needs to be performed. Must be set false when any other fields are modified in this
    ///   struct.
    /// </summary>
    [JsonIgnore]
    public bool StateApplied;
}
