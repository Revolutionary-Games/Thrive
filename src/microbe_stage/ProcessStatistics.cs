using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using Nito.Collections;

/// <summary>
///   Holds process running statistics information
/// </summary>
public sealed class ProcessStatistics
{
    /// <summary>
    ///   Temporary memory to use for <see cref="RemoveUnused"/> to avoid small constant allocations. This used to be
    ///   thread local, but as we lock anyway, it doesn't really matter.
    /// </summary>
    private List<TweakedProcess>? temporaryRemovedItems;

    /// <summary>
    ///   The processes and their associated speed statistics
    /// </summary>
    public Dictionary<TweakedProcess, SingleProcessStatistics> Processes { get; } = new();

    public void MarkAllUnused()
    {
        lock (Processes)
        {
            foreach (var entry in Processes)
            {
                entry.Value.Used = false;
            }
        }
    }

    public void RemoveUnused()
    {
        lock (Processes)
        {
            temporaryRemovedItems ??= new List<TweakedProcess>();

            foreach (var entry in Processes)
            {
                if (!entry.Value.Used)
                    temporaryRemovedItems.Add(entry.Key);
            }

            int count = temporaryRemovedItems.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    if (!Processes.Remove(temporaryRemovedItems[i]))
                        GD.PrintErr("Failed to remove item from ProcessStatistics");
                }

                temporaryRemovedItems.Clear();
            }
        }
    }

    public SingleProcessStatistics GetAndMarkUsed(TweakedProcess forProcess)
    {
#if DEBUG
        if (forProcess.Process == null!)
            throw new ArgumentException("Invalid process marked used");
#endif

        lock (Processes)
        {
            if (Processes.TryGetValue(forProcess, out var entry))
            {
                entry.Used = true;
                return entry;
            }

            entry = new SingleProcessStatistics(forProcess);
            Processes[forProcess] = entry;
            entry.Used = true;
            return entry;
        }
    }
}

/// <summary>
///   Statistics for a single process
/// </summary>
public class SingleProcessStatistics : IProcessDisplayInfo
{
    private readonly float keepSnapshotTime;
    private readonly Deque<SingleProcessStatisticsSnapshot> snapshots = new();

    private List<Compound>? limitingCompounds;

    private Dictionary<Compound, float> environmentalInputs = new();

    private Dictionary<Compound, float>? precomputedEnvironmentInputs;

    public SingleProcessStatistics(TweakedProcess process,
        float keepSnapshotTime = Constants.DEFAULT_PROCESS_STATISTICS_AVERAGE_INTERVAL)
    {
        this.keepSnapshotTime = keepSnapshotTime;
        Process = process;
    }

    /// <summary>
    ///   The process these statistics are for
    /// </summary>
    public TweakedProcess Process { get; private set; }

    public bool Used { get; internal set; }

    public string Name => Process.Process.Name;

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

    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs => environmentalInputs
        ?? throw new InvalidOperationException("No snapshot set");

    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs =>
        precomputedEnvironmentInputs ??= Process.Process.Inputs
            .Where(p => IProcessDisplayInfo.IsEnvironmental(p.Key.ID))
            .ToDictionary(p => p.Key.ID, p => p.Value);

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

    public float CurrentSpeed
    {
        get => CalculateAverageSpeed();
        set
        {
            if (LatestSnapshot == null)
                throw new InvalidOperationException("Snapshot needs to be set before recording current speed");

            LatestSnapshot.CurrentSpeed = value;
        }
    }

    public bool Enabled => Process.SpeedMultiplier > 0;

    public IReadOnlyList<Compound>? LimitingCompounds => limitingCompounds;

    private SingleProcessStatisticsSnapshot? LatestSnapshot =>
        snapshots.Count > 0 ? snapshots[snapshots.Count - 1] : null;

    public void BeginFrame(float delta)
    {
        // Prepare the next snapshot object to fill
        SingleProcessStatisticsSnapshot? existing = null;

        float seenTime = 0;

        // Remove statistics that are outside the keep time
        for (int i = snapshots.Count - 1; i >= 0; --i)
        {
            var current = snapshots[i];

            if (seenTime > keepSnapshotTime)
            {
                existing = current;
                existing.Clear();

                while (i >= 0)
                {
                    snapshots.RemoveFromFront();
                    --i;
                }

                break;
            }

            seenTime += current.Delta;
        }

        existing ??= new SingleProcessStatisticsSnapshot();

        existing.Delta = delta;
        snapshots.AddToBack(existing);

        // TODO: does this need to be cleared this often?
        precomputedEnvironmentInputs = null;
        limitingCompounds?.Clear();
        environmentalInputs.Clear();
    }

    public void AddLimitingFactor(Compound compound)
    {
        limitingCompounds ??= new List<Compound>();

        limitingCompounds.Add(compound);
    }

    public void AddCapacityProblem(Compound compound)
    {
        limitingCompounds ??= new List<Compound>();

        // For now this is shown to the user the same way as limit problems
        limitingCompounds.Add(compound);
    }

    public void AddEnvironmentInput(Compound compound, float amount)
    {
        environmentalInputs[compound] = amount;
    }

    /// <summary>
    ///   Adds all environmental inputs to the target dictionary without allocating an enumerator. Will throw if the
    ///   target already has something, so it should be cleared by the caller first.
    /// </summary>
    /// <param name="target">Where to copy the data</param>
    public void CopyEnvironmentalInputs(Dictionary<Compound, float> target)
    {
        foreach (var input in environmentalInputs)
        {
            target.Add(input.Key, input.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateProcessDataIfNeeded(in TweakedProcess process)
    {
        if (!MatchesUnderlyingProcess(process.Process))
        {
            GD.PrintErr("Wrong process info passed to SingleProcessStatistics for data update");
            return;
        }

        // Copying the whole struct is needed here, even though really we'd just need to copy the speed multiplier as
        // that's the only thing that is allowed to change
        Process = process;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MatchesUnderlyingProcess(BioProcess process)
    {
        return Process.Process == process;
    }

    public bool Equals(IProcessDisplayInfo? other)
    {
        return Equals((object?)other);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        // This also checks for obj being null
        if (obj is SingleProcessStatistics statistics)
        {
            return Process.Equals(statistics.Process);
        }

        return false;
    }

    public bool Equals(SingleProcessStatistics? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Process.Equals(other.Process);
    }

    public override int GetHashCode()
    {
        return 233 ^ Process.GetHashCode();
    }

    public override string ToString()
    {
        return $"Single process speed {CurrentSpeed} for {Process}";
    }

    private float CalculateAverageSpeed()
    {
        float average = 0.0f;

        for (int i = 0; i < snapshots.Count; ++i)
        {
            average += snapshots[i].CurrentSpeed;
        }

        average /= snapshots.Count;

        return average;
    }

    /// <summary>
    ///   Single point in time when statistics were collected
    /// </summary>
    private class SingleProcessStatisticsSnapshot
    {
        public float CurrentSpeed;
        public float Delta;

        /// <summary>
        ///   Prepares this for reuse
        /// </summary>
        public void Clear()
        {
            CurrentSpeed = 0;
            Delta = 0;
        }
    }
}
