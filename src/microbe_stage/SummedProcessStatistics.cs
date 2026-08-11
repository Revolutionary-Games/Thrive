using System;
using System.Collections.Generic;

/// <summary>
///   Summed statistics of multiple processes
/// </summary>
public class SummedProcessStatistics : IProcessDisplayInfo
{
    private readonly Dictionary<Compound, float> summedEnvironmentalInputs = new();

    private readonly Dictionary<Compound, float> summedFullSpeedRequiredEnvironmentalInputs = new();

    public SummedProcessStatistics(TweakedProcess process)
    {
        Process = process;
    }

    /// <summary>
    ///   The process these statistics are for
    /// </summary>
    public TweakedProcess Process { get; private set; }

    public string Name => Process.Process.Name;

    public float CurrentSpeed { get; set; }

    public IEnumerable<KeyValuePair<Compound, float>> Inputs
    {
        get
        {
            foreach (var input in Process.Process.Inputs)
            {
                if (input.Key.IsEnvironmental)
                    continue;

                yield return new KeyValuePair<Compound, float>(input.Key.ID, input.Value * CurrentSpeed);
            }
        }
    }

    /// <summary>
    ///   Current environmental input values
    /// </summary>
    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs => summedEnvironmentalInputs;

    /// <summary>
    ///   Environment inputs that result in the process running at maximum speed
    /// </summary>
    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs =>
        summedFullSpeedRequiredEnvironmentalInputs;

    /// <summary>
    ///   All the output compounds
    /// </summary>
    public IEnumerable<KeyValuePair<Compound, float>> Outputs
    {
        get
        {
            foreach (var output in Process.Process.Outputs)
            {
                yield return new KeyValuePair<Compound, float>(output.Key.ID, output.Value * CurrentSpeed);
            }
        }
    }

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

    /// <summary>
    ///   Adds the <paramref name="stats"/>' speed to the these stats. Also updates this class' secondary info.
    /// </summary>
    public void SumWithStatistics(SingleProcessStatistics stats)
    {
        if (stats.Process.Process != Process.Process)
        {
            throw new ArgumentException(
                "The statistics provided to SummedProcessStatistics have a different bioprocess");
        }

        CurrentSpeed += stats.CurrentSpeed;

        // Refresh the process' manual activation status
        var newProcess = Process;
        newProcess.SpeedMultiplier = Math.Max(newProcess.SpeedMultiplier, stats.Process.SpeedMultiplier);
        Process = newProcess;

        // The next three stats can't vary between cells in a colony, so they are only set once per frame.
        // Importantly, they reset each frame by Clear(), so e.g. moving patches can still change these.
        // So as a result, we assume it is safe to latch these values on the first time we see them and then wait
        // until the next clear.

        if (summedEnvironmentalInputs.Count == 0)
        {
            stats.CopyEnvironmentalInputs(summedEnvironmentalInputs);
        }

        LimitingCompounds ??= stats.LimitingCompounds;

        if (summedFullSpeedRequiredEnvironmentalInputs.Count == 0)
        {
            foreach (var input in stats.FullSpeedRequiredEnvironmentalInputs)
            {
                summedFullSpeedRequiredEnvironmentalInputs.Add(input.Key, input.Value);
            }
        }
    }

    public void Clear()
    {
        CurrentSpeed = 0.0f;

        var newProcess = Process;
        newProcess.SpeedMultiplier = 0.0f;
        Process = newProcess;

        summedEnvironmentalInputs.Clear();
        summedFullSpeedRequiredEnvironmentalInputs.Clear();
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
