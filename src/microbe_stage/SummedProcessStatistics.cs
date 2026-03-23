using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Summed statistics of multiple processes
/// </summary>
public class SummedProcessStatistics : IProcessDisplayInfo
{
    public int ProcessCount;

    public float RawSpeed;

    private readonly Dictionary<Compound, float> summedEnvironmentalInputs = new();

    private readonly Dictionary<Compound, float> summedFullSpeedRequiredEnvironmentalInputs = new();

    public SummedProcessStatistics(SingleProcessStatistics displayInfo)
    {
        if (displayInfo.LimitingCompounds != null)
            LimitingCompounds = displayInfo.LimitingCompounds.ToList();

        Process = displayInfo.Process;

        RawSpeed = displayInfo.RawSpeed();
        CurrentSpeed = displayInfo.CurrentSpeed;
    }

    /// <summary>
    ///   The process these statistics are for
    /// </summary>
    public TweakedProcess Process { get; set; }

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

    public IEnumerable<(Compound Compound, float Amount)> Inputs()
    {
        foreach (var input in Process.Process.Inputs)
        {
            if (input.Key.IsEnvironmental)
                continue;

            yield return (input.Key.ID, input.Value * CurrentSpeed);
        }
    }

    public IEnumerable<(Compound Compound, float Amount)> Outputs()
    {
        foreach (var output in Process.Process.Outputs)
        {
            yield return (output.Key.ID, output.Value * CurrentSpeed);
        }
    }

    public void Clear()
    {
        ProcessCount = 0;
        RawSpeed = 0.0f;
        CurrentSpeed = 0.0f;

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
