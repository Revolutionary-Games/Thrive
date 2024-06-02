/// <summary>
///   Balance mode to use when showing things in the GUI. These values are directly used in Godot scenes, do not
///   reorder or change.
/// </summary>
public enum BalanceDisplayType
{
    /// <summary>
    ///   Calculate things as if all processes run at max speed
    /// </summary>
    MaxSpeed = 0,

    /// <summary>
    ///   Displaying things when setting ATP balance to be 0 (so only run things as fast as they need to be to live)
    /// </summary>
    EnergyEquilibrium = 1,
}
