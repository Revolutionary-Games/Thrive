using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Summed statistics of multiple processes
/// </summary>
public class SummedProcessStatistics : IProcessDisplayInfo
{
    public int ProcessCount;

    private readonly Dictionary<Compound, float> summedInputs = new();

    private readonly Dictionary<Compound, float> summedEnvironmentalInputs = new();

    private readonly Dictionary<Compound, float> summedFullSpeedRequiredEnvironmentalInputs = new();

    private readonly Dictionary<Compound, float> summedOutputs = new();

    private float summedSpeed;

    public SummedProcessStatistics(IProcessDisplayInfo displayInfo)
    {
        if (displayInfo.LimitingCompounds != null)
            LimitingCompounds = displayInfo.LimitingCompounds.ToList();

        Enabled = displayInfo.Enabled;

        if (displayInfo is AverageProcessStatistics averageProcessStatistics)
        {
            Process = averageProcessStatistics.Process;
        }
        else if (displayInfo is SingleProcessStatistics singleProcessStatistics)
        {
            Process = singleProcessStatistics.Process;
        }

        AddProcess(displayInfo);
    }

    /// <summary>
    ///   The process these statistics are for
    /// </summary>
    public TweakedProcess Process { get; private set; }

    public string Name => Process.Process.Name;

    public float CurrentSpeed => summedSpeed;

    public IEnumerable<KeyValuePair<Compound, float>> Inputs => summedInputs;

    /// <summary>
    ///   Current environmental input values
    /// </summary>
    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs => summedEnvironmentalInputs;

    /// <summary>
    ///   Environment inputs that result in process running at maximum speed
    /// </summary>
    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs =>
        summedFullSpeedRequiredEnvironmentalInputs;

    /// <summary>
    ///   All the output compounds
    /// </summary>
    public IReadOnlyDictionary<Compound, float> Outputs => summedOutputs;

    public IReadOnlyList<Compound>? LimitingCompounds { get; set; }

    public bool Enabled { get; set; }

    /// <summary>
    ///   Used for algorithms that need to know what they have processed already
    /// </summary>
    public bool Marked { get; set; }

    public bool MatchesUnderlyingProcess(BioProcess process)
    {
        return process == Process.Process;
    }

    public void AddProcess(IProcessDisplayInfo displayInfo)
    {
        foreach (var input in displayInfo.Inputs)
        {
            summedInputs.TryGetValue(input.Key, out var value);
            summedInputs[input.Key] = value + input.Value;
        }

        foreach (var output in displayInfo.Outputs)
        {
            summedOutputs.TryGetValue(output.Key, out var value);
            summedOutputs[output.Key] = value + output.Value;
        }

        foreach (var output in displayInfo.EnvironmentalInputs)
        {
            summedEnvironmentalInputs.TryGetValue(output.Key, out var value);
            summedEnvironmentalInputs[output.Key] = value + output.Value;
        }

        foreach (var output in displayInfo.FullSpeedRequiredEnvironmentalInputs)
        {
            summedFullSpeedRequiredEnvironmentalInputs.TryGetValue(output.Key, out var value);
            summedFullSpeedRequiredEnvironmentalInputs[output.Key] = value + output.Value;
        }

        summedSpeed += displayInfo.CurrentSpeed;
        ++ProcessCount;

        Enabled = displayInfo.Enabled;
    }

    public void Clear()
    {
        summedSpeed = 0.0f;
        ProcessCount = 0;

        summedInputs.Clear();
        summedOutputs.Clear();
        summedEnvironmentalInputs.Clear();
        summedFullSpeedRequiredEnvironmentalInputs.Clear();
    }

    public bool Equals(IProcessDisplayInfo? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        if (!obj.MatchesUnderlyingProcess(Process.Process))
            return false;

        return MathF.Abs(summedSpeed - obj.CurrentSpeed) < MathUtils.EPSILON;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is SummedProcessStatistics statistics)
        {
            return Equals(this, statistics);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return 3079 ^ Process.GetHashCode();
    }
}
