using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Speed information of a process in specific patch. Used in the
///   editor to show info to the player.
/// </summary>
public class ProcessSpeedInformation : IProcessDisplayInfo
{
    public readonly Dictionary<Compound, float> WritableInputs = new Dictionary<Compound, float>();
    public readonly Dictionary<Compound, float> WritableOutputs = new Dictionary<Compound, float>();
    public readonly List<Compound> WritableLimitingCompounds = new List<Compound>();

    public readonly Dictionary<Compound, float> AvailableAmounts = new Dictionary<Compound, float>();

    // ReSharper disable once CollectionNeverQueried.Global
    public readonly Dictionary<Compound, float> AvailableRates = new Dictionary<Compound, float>();

    public ProcessSpeedInformation(BioProcess process)
    {
        Process = process;
    }

    public BioProcess Process { get; }

    public string Name => Process.Name;

    public IEnumerable<KeyValuePair<Compound, float>> Inputs =>
        WritableInputs.Where(p => !p.Key.IsEnvironmental);

    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs =>
        AvailableAmounts.Where(p => p.Key.IsEnvironmental);

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs { get; }

    public IEnumerable<KeyValuePair<Compound, float>> Outputs => WritableOutputs;

    public float CurrentSpeed { get; set; }

    public IReadOnlyList<Compound> LimitingCompounds => WritableLimitingCompounds;
}
