﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nito.Collections;

/// <summary>
///   Holds process running statistics information
/// </summary>
public class ProcessStatistics
{
    public Dictionary<TweakedProcess, SingleProcessStatistics> Processes { get; } = new();

    public void MarkAllUnused()
    {
        foreach (var entry in Processes)
        {
            entry.Value.Used = false;
        }
    }

    public void RemoveUnused()
    {
        foreach (var item in Processes.Where(p => !p.Value.Used).ToList())
        {
            Processes.Remove(item.Key);
        }
    }

    public SingleProcessStatistics GetAndMarkUsed(TweakedProcess forProcess)
    {
        if (Processes.TryGetValue(forProcess, out var entry))
        {
            entry.Used = true;
            return entry;
        }

        entry = new SingleProcessStatistics(forProcess.Process);
        Processes[forProcess] = entry;
        entry.Used = true;
        return entry;
    }
}

/// <summary>
///   Statistics for a single process
/// </summary>
public class SingleProcessStatistics : IProcessDisplayInfo
{
    private readonly float keepSnapshotTime;
    private readonly Deque<SingleProcessStatisticsSnapshot> snapshots = new();

    /// <summary>
    ///   Cached statistics object to not need to recreate this each time the average statistics are computed.
    /// </summary>
    private AverageProcessStatistics computedStatistics;

    private Dictionary<Compound, float>? precomputedEnvironmentInputs;

    public SingleProcessStatistics(BioProcess process,
        float keepSnapshotTime = Constants.DEFAULT_PROCESS_STATISTICS_AVERAGE_INTERVAL)
    {
        this.keepSnapshotTime = keepSnapshotTime;
        Process = process;
        computedStatistics = new AverageProcessStatistics(this);
    }

    /// <summary>
    ///   The process these statistics are for
    /// </summary>
    public BioProcess Process { get; }

    public bool Used { get; internal set; }

    public string Name => Process.Name;

    public IEnumerable<KeyValuePair<Compound, float>> Inputs =>
        LatestSnapshot?.Inputs.Where(p => !p.Key.IsEnvironmental) ??
        throw new InvalidOperationException("No snapshot set");

    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs =>
        LatestSnapshot?.Inputs.Where(p => p.Key.IsEnvironmental) ??
        throw new InvalidOperationException("No snapshot set");

    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs =>
        precomputedEnvironmentInputs ??= Process.Inputs
            .Where(p => p.Key.IsEnvironmental)
            .ToDictionary(p => p.Key, p => p.Value);

    public IEnumerable<KeyValuePair<Compound, float>> Outputs =>
        LatestSnapshot?.Outputs ?? throw new InvalidOperationException("No snapshot set");

    public float CurrentSpeed
    {
        get => LatestSnapshot?.CurrentSpeed ?? 0;
        set
        {
            if (LatestSnapshot == null)
                throw new InvalidOperationException("Snapshot needs to be set before recording current speed");

            LatestSnapshot.CurrentSpeed = value;
        }
    }

    public IReadOnlyList<Compound>? LimitingCompounds => LatestSnapshot?.LimitingCompounds;

    private SingleProcessStatisticsSnapshot? LatestSnapshot =>
        snapshots.Count > 0 ? snapshots[snapshots.Count - 1] : null;

    /// <summary>
    ///   Computes the average values reported in this statistics object. Used to provide smoother display for the user
    /// </summary>
    /// <returns>The average statistics result. This may not be modified.</returns>
    public IProcessDisplayInfo ComputeAverageValues()
    {
        // Statistics can't be computed if we have nothing to compute them from
        // TODO: should the statistics be cleared in this case?
        if (snapshots.Count < 1)
            return computedStatistics;

        int entriesProcessed = 0;

        float totalSpeed = 0;
        computedStatistics.WritableInputs.Clear();
        computedStatistics.WritableOutputs.Clear();

        var seenLimiters = new Dictionary<Compound, int>();

        foreach (var entry in snapshots)
        {
            totalSpeed += entry.CurrentSpeed;

            computedStatistics.WritableInputs.Merge(entry.Inputs);
            computedStatistics.WritableOutputs.Merge(entry.Outputs);

            foreach (var limit in entry.LimitingCompounds)
            {
                seenLimiters.TryGetValue(limit, out var existing);

                seenLimiters[limit] = existing + 1;
            }

            ++entriesProcessed;
        }

        // TODO: probably want to come up with a better way for averaging this
        int limitorCutoff = entriesProcessed / 2;

        // It is assumed that the updates have happened at pretty consistent intervals. So we do a rough average
        // based on the entry count. If we wanted to get fancy we could take the delta in each snapshot into account
        computedStatistics.CurrentSpeed = totalSpeed / entriesProcessed;
        computedStatistics.WritableInputs.DivideBy(entriesProcessed);
        computedStatistics.WritableOutputs.DivideBy(entriesProcessed);

        computedStatistics.WritableLimitingCompounds.Clear();

        foreach (var entry in seenLimiters)
        {
            if (entry.Value > limitorCutoff)
            {
                computedStatistics.WritableLimitingCompounds.Add(entry.Key);
            }
        }

        return computedStatistics;
    }

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
    }

    public void AddLimitingFactor(Compound compound)
    {
        if (LatestSnapshot == null)
            throw new InvalidOperationException("Snapshot needs to be set before recording data into it");

        LatestSnapshot.LimitingCompounds.Add(compound);
    }

    public void AddCapacityProblem(Compound compound)
    {
        if (LatestSnapshot == null)
            throw new InvalidOperationException("Snapshot needs to be set before recording data into it");

        // For now this is shown to the user the same way as limit problems
        LatestSnapshot.LimitingCompounds.Add(compound);
    }

    public void AddInputAmount(Compound compound, float amount)
    {
        if (LatestSnapshot == null)
            throw new InvalidOperationException("Snapshot needs to be set before recording data into it");

        LatestSnapshot.Inputs[compound] = amount;
    }

    public void AddOutputAmount(Compound compound, float amount)
    {
        if (LatestSnapshot == null)
            throw new InvalidOperationException("Snapshot needs to be set before recording data into it");

        LatestSnapshot.Outputs[compound] = amount;
    }

    public void Clear()
    {
        snapshots.Clear();
        precomputedEnvironmentInputs = null;
    }

    public override bool Equals(object obj)
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

    public override int GetHashCode()
    {
        return 37 ^ Process.GetHashCode();
    }

    /// <summary>
    ///   Single point in time when statistics were collected
    /// </summary>
    private class SingleProcessStatisticsSnapshot
    {
        public readonly Dictionary<Compound, float> Inputs = new();
        public readonly Dictionary<Compound, float> Outputs = new();
        public readonly List<Compound> LimitingCompounds = new();
        public float CurrentSpeed;
        public float Delta;

        /// <summary>
        ///   Prepares this for reuse
        /// </summary>
        public void Clear()
        {
            CurrentSpeed = 0;
            Delta = 0;
            Inputs.Clear();
            Outputs.Clear();
            LimitingCompounds.Clear();
        }
    }
}

/// <summary>
///   Computed average statistics for a single process
/// </summary>
public class AverageProcessStatistics : IProcessDisplayInfo
{
    public readonly Dictionary<Compound, float> WritableInputs = new();
    public readonly Dictionary<Compound, float> WritableOutputs = new();
    public readonly List<Compound> WritableLimitingCompounds = new();

    private readonly SingleProcessStatistics owner;

    public AverageProcessStatistics(SingleProcessStatistics owner)
    {
        this.owner = owner;
    }

    public string Name => owner.Name;

    public IEnumerable<KeyValuePair<Compound, float>> Inputs =>
        WritableInputs.Where(p => !p.Key.IsEnvironmental);

    public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs =>
        WritableInputs.Where(p => p.Key.IsEnvironmental);

    public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs =>
        owner.FullSpeedRequiredEnvironmentalInputs;

    public IEnumerable<KeyValuePair<Compound, float>> Outputs => WritableOutputs;
    public float CurrentSpeed { get; set; }
    public IReadOnlyList<Compound> LimitingCompounds => WritableLimitingCompounds;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        // This also checks for obj being null
        if (obj is AverageProcessStatistics statistics)
        {
            return owner.Equals(statistics.owner);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return 211 ^ owner.GetHashCode();
    }
}
