using System.Collections.ObjectModel;

/// <summary>
///   Info needed to show a process in a process list
/// </summary>
public interface IProcessDisplayInfo
{
    /// <summary>
    ///   User readable name
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Input compounds that aren't environmental
    /// </summary>
    public ReadOnlyDictionary<Compound, float> Inputs { get; }

    /// <summary>
    ///   Current environmental input values
    /// </summary>
    public ReadOnlyDictionary<Compound, float> EnvironmentalInputs { get; }

    /// <summary>
    ///   Environment inputs that result in process running at maximum speed
    /// </summary>
    public ReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs { get; }

    /// <summary>
    ///   All of the output compounds
    /// </summary>
    public ReadOnlyDictionary<Compound, float> Outputs { get; }

    /// <summary>
    ///   The current speed of the process (if known)
    /// </summary>
    public float CurrentSpeed { get; }

    /// <summary>
    ///   The limiting compound in speed. Or null if not set
    /// </summary>
    public Compound LimitingCompound { get; }
}
