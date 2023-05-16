using Newtonsoft.Json;

/// <summary>
///   Interface for all strategic stage units that the player can command to move around implement
/// </summary>
public interface IStrategicUnit
{
    public string UnitName { get; }

    /// <summary>
    ///   The name of the unit shown by <see cref="StrategicUnitScreen{T}"/>
    /// </summary>
    [JsonIgnore]
    public string UnitScreenTitle { get; }
}
