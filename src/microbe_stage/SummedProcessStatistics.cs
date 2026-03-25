using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Summed statistics of multiple processes
/// </summary>
public class SummedProcessStatistics : IProcessDisplayInfo
{
    public int ProcessCount;

    private readonly Dictionary<Compound, float> summedEnvironmentalInputs = new();

    private readonly Dictionary<Compound, float> summedFullSpeedRequiredEnvironmentalInputs = new();

    public SummedProcessStatistics(SingleProcessStatistics displayInfo)
    {
        if (displayInfo.LimitingCompounds != null)
            LimitingCompounds = displayInfo.LimitingCompounds.ToList();

        Process = displayInfo.Process;

        CurrentSpeed = displayInfo.CurrentSpeed;

        foreach (var input in displayInfo.FullSpeedRequiredEnvironmentalInputs)
        {
            summedFullSpeedRequiredEnvironmentalInputs.Add(input.Key, input.Value);
        }

        UpdateSecondaryInfo(displayInfo);
    }

    /// <summary>
    ///   The process these statistics are for
    /// </summary>
    public TweakedProcess Process { get; private set; }

    public string Name => Process.Process.Name;

    public float CurrentSpeed { get; set; }

    /// <summary>
    ///   Current environmental input values
    /// </summary>
    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs => summedEnvironmentalInputs;

    /// <summary>
    ///   Environment inputs that result in process running at maximum speed
    /// </summary>
    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs =>
        summedFullSpeedRequiredEnvironmentalInputs;

    public IReadOnlyList<Compound>? LimitingCompounds { get; set; }

    public bool Enabled => Process.SpeedMultiplier > 0;

    /// <summary>
    ///   Used for algorithms that need to know what they have processed already
    /// </summary>
    public bool Marked { get; set; }

    public bool MatchesUnderlyingProcess(BioProcess process)
    {
        return process == Process.Process;
    }

    public void UpdateSecondaryInfo(SingleProcessStatistics stats)
    {
        if (stats.Process.Process != Process.Process)
        {
            throw new ArgumentException(
                "The statistics provided to SummedProcessStatistics have a different bioprocess");
        }

        Process = stats.Process;

        summedEnvironmentalInputs.Clear();

        foreach (var input in stats.EnvironmentalInputs)
        {
            summedEnvironmentalInputs.Add(input.Key, input.Value);
        }

        LimitingCompounds = stats.LimitingCompounds;
    }

    public IEnumerable<KeyValuePair<Compound, float>> Inputs()
    {
        foreach (var input in Process.Process.Inputs)
        {
            if (input.Key.IsEnvironmental)
                continue;

            yield return new KeyValuePair<Compound, float>(input.Key.ID, input.Value * CurrentSpeed);
        }
    }

    public IEnumerable<KeyValuePair<Compound, float>> Outputs()
    {
        foreach (var output in Process.Process.Outputs)
        {
            yield return new KeyValuePair<Compound, float>(output.Key.ID, output.Value * CurrentSpeed);
        }
    }

    public void Clear()
    {
        ProcessCount = 0;
        CurrentSpeed = 0.0f;

        summedEnvironmentalInputs.Clear();
        LimitingCompounds = null;
    }

    public bool Equals(IProcessDisplayInfo? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        if (!obj.MatchesUnderlyingProcess(Process.Process))
            return false;

        return MathF.Abs(CurrentSpeed - obj.CurrentSpeed) < MathUtils.EPSILON;
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
