namespace Components;

/// <summary>
///   Special actions to perform on time to live expiring and fading out
/// </summary>
[JSONDynamicTypeAllowed]
public struct FadeOutActions
{
    public float FadeTime;

    public bool DisableCollisions;
    public bool RemoveVelocity;
    public bool RemoveAngularVelocity;

    /// <summary>
    ///   Disables a particles emitter if there is one on the entity spatial root or the first child. Will print an
    ///   error if missing.
    /// </summary>
    public bool DisableParticles;

    public bool UsesMicrobialDissolveEffect;

    /// <summary>
    ///   If true then <see cref="CompoundStorage"/> is emptied on fade out
    /// </summary>
    public bool VentCompounds;

    /// <summary>
    ///   Internal variable for use by the managing system
    /// </summary>
    public bool CallbackRegistered;
}
