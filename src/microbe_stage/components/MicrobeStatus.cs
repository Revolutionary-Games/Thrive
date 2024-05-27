namespace Components;

using Godot;

/// <summary>
///   A collection place for various microbe status flags and variables that don't have more sensible components
///   to put them in
/// </summary>
[JSONDynamicTypeAllowed]
public struct MicrobeStatus
{
    // Variables related to movement sound playing
    public Vector3 LastLinearVelocity;
    public Vector3 LastLinearAcceleration;
    public float MovementSoundCooldownTimer;

    public float LastCheckedATPDamage;

    public float LastCheckedOxytoxyDigestionDamage;

    // TODO: remove if rate limited reproduction is not needed
    // public float LastCheckedReproduction;

    public float TimeUntilChemoreceptionUpdate;

    /// <summary>
    ///   Flips every reproduction update. Used to make compound use for reproduction distribute more evenly between
    ///   the compound types.
    /// </summary>
    public bool ConsumeReproductionCompoundsReverse;
}
