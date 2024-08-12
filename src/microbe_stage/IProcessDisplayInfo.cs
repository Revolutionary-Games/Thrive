using System;
using System.Collections.Generic;

/// <summary>
///   Info needed to show a process in a process list
/// </summary>
/// <remarks>
///   <para>
///     This requires the <see cref="IEquatable{T}"/> interface as comparing the process display info must match when
///     the objects are for the same <b>process</b> (and not the same display info object).
///   </para>
/// </remarks>
public interface IProcessDisplayInfo : IEquatable<IProcessDisplayInfo>
{
    /// <summary>
    ///   User readable name. Do not use to match process information, use <see cref="MatchesUnderlyingProcess"/>
    ///   instead.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Input compounds that aren't environmental
    /// </summary>
    public IEnumerable<KeyValuePair<Compound, float>> Inputs { get; }

    /// <summary>
    ///   Current environmental input values
    /// </summary>
    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs { get; }

    /// <summary>
    ///   Environment inputs that result in process running at maximum speed
    /// </summary>
    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs { get; }

    /// <summary>
    ///   All the output compounds
    /// </summary>
    public IReadOnlyDictionary<Compound, float> Outputs { get; }

    /// <summary>
    ///   The current speed of the process (if known)
    /// </summary>
    public float CurrentSpeed { get; }

    /// <summary>
    ///   The limiting compounds in speed. Or null if not set
    /// </summary>
    public IReadOnlyList<Compound>? LimitingCompounds { get; }

    /// <summary>
    ///   Checks if this process info is for the given underlying process
    /// </summary>
    /// <returns>True if matches, false if this info is for some other process type</returns>
    public bool MatchesUnderlyingProcess(BioProcess process);
}
