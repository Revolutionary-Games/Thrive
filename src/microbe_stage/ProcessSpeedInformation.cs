using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Speed information of a process in specific patch. Used in the
///   editor to show info to the player.
/// </summary>
public class ProcessSpeedInformation : IProcessDisplayInfo
{
    public ProcessSpeedInformation(BioProcess process)
    {
        Process = process;
    }

    public BioProcess Process { get; }

    public string Name => Process.Name;

    public Dictionary<Compound, float> WritableInputs { get; } = new();
    public Dictionary<Compound, float> WritableOutputs { get; } = new();
    public List<Compound> WritableLimitingCompounds { get; } = new();

    public Dictionary<Compound, float> WritableFullSpeedRequiredEnvironmentalInputs { get; } = new();

    public Dictionary<Compound, float> AvailableAmounts { get; } = new();

    // ReSharper disable once CollectionNeverQueried.Global
    public Dictionary<Compound, float> AvailableRates { get; } = new();

    public IEnumerable<KeyValuePair<Compound, float>> Inputs =>
        WritableInputs.Where(p => !p.Key.IsEnvironmental);

    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs =>
        AvailableAmounts.Where(p => p.Key.IsEnvironmental);

    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs =>
        WritableFullSpeedRequiredEnvironmentalInputs;

    public IEnumerable<KeyValuePair<Compound, float>> Outputs => WritableOutputs;

    public float CurrentSpeed { get; set; }

    /// <summary>
    ///   Efficiency is a measure of how well the environment is favourable to the process.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     It is computed as the product of the available amounts of environmental compounds
    ///     at instantiation and stored here.
    ///   </para>
    /// </remarks>
    public float Efficiency { get; set; }

    public IReadOnlyList<Compound> LimitingCompounds => WritableLimitingCompounds;
}
