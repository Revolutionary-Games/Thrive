using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Speed information of a process in specific patch. Used in the editor to show info to the player.
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
        WritableInputs.Where(p => !IProcessDisplayInfo.IsEnvironmental(p.Key));

    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs =>
        AvailableAmounts.Where(p => IProcessDisplayInfo.IsEnvironmental(p.Key));

    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs =>
        WritableFullSpeedRequiredEnvironmentalInputs;

    public IReadOnlyDictionary<Compound, float> Outputs => WritableOutputs;

    public float CurrentSpeed { get; set; }

    /// <summary>
    ///   Efficiency is a measure of how well the environment is favorable to the process.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     It is computed as the product of the available amounts of environmental compounds
    ///     at instantiation and stored here.
    ///   </para>
    /// </remarks>
    public float Efficiency { get; set; }

    public IReadOnlyList<Compound> LimitingCompounds => WritableLimitingCompounds;

    // Direct access to ATP values as these need to be read a lot in auto-evo
    public float ATPProduction { get; set; }
    public float ATPConsumption { get; set; }

    public bool MatchesUnderlyingProcess(BioProcess process)
    {
        return Process == process;
    }

    /// <summary>
    ///   Scales all non-environmental inputs and outputs with the given modifier
    /// </summary>
    public void ScaleSpeed(float modifier, Dictionary<Compound, float>? workMemory)
    {
        workMemory ??= new Dictionary<Compound, float>();

        if (WritableInputs.Count > 0)
        {
            workMemory.Clear();

            foreach (var input in WritableInputs)
            {
                if (IProcessDisplayInfo.IsEnvironmental(input.Key))
                    continue;

                workMemory.Add(input.Key, input.Value * modifier);
            }

            foreach (var entry in workMemory)
            {
                WritableInputs[entry.Key] = entry.Value;
            }
        }

        if (WritableOutputs.Count > 0)
        {
            workMemory.Clear();

            foreach (var output in WritableOutputs)
            {
                if (IProcessDisplayInfo.IsEnvironmental(output.Key))
                    continue;

                workMemory.Add(output.Key, output.Value * modifier);
            }

            foreach (var entry in workMemory)
            {
                WritableOutputs[entry.Key] = entry.Value;
            }
        }
    }

    public bool Equals(IProcessDisplayInfo? other)
    {
        return Equals((object?)other);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ProcessSpeedInformation other && Equals(other));
    }

    public override int GetHashCode()
    {
        return 239 ^ Process.GetHashCode();
    }

    public override string ToString()
    {
        return $"Process speed {CurrentSpeed} for {Process}";
    }

    protected bool Equals(ProcessSpeedInformation other)
    {
        return Process.Equals(other.Process);
    }
}
