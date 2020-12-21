using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Holds process running statistics information
/// </summary>
public class ProcessStatistics
{
    public Dictionary<TweakedProcess, SingleProcessStatistics> Processes { get; } =
        new Dictionary<TweakedProcess, SingleProcessStatistics>();

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
        if (Processes.ContainsKey(forProcess))
        {
            var result = Processes[forProcess];
            result.Used = true;
            return result;
        }

        var newEntry = new SingleProcessStatistics(forProcess.Process);
        Processes[forProcess] = newEntry;
        newEntry.Used = true;
        return newEntry;
    }

    public class SingleProcessStatistics : IProcessDisplayInfo
    {
        private readonly Dictionary<Compound, float> inputs = new Dictionary<Compound, float>();
        private readonly Dictionary<Compound, float> outputs = new Dictionary<Compound, float>();
        private readonly List<Compound> limitingCompounds = new List<Compound>();

        private Dictionary<Compound, float> precomputedEnvironmentInputs = new Dictionary<Compound, float>();

        public SingleProcessStatistics(BioProcess process)
        {
            Process = process;
        }

        /// <summary>
        ///   The process these statistics are for
        /// </summary>
        public BioProcess Process { get; }

        public bool Used { get; internal set; }

        public string Name => Process.Name;
        public IEnumerable<KeyValuePair<Compound, float>> Inputs => inputs.Where(p => !p.Key.IsEnvironmental);

        public IEnumerable<KeyValuePair<Compound, float>> EnvironmentalInputs =>
            inputs.Where(p => p.Key.IsEnvironmental);

        public IReadOnlyDictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs
        {
            get
            {
                if (precomputedEnvironmentInputs == null)
                {
                    precomputedEnvironmentInputs = Process.Inputs.Where(p => p.Key.IsEnvironmental)
                        .ToDictionary(p => p.Key, p => p.Value);
                }

                return precomputedEnvironmentInputs;
            }
        }

        public IEnumerable<KeyValuePair<Compound, float>> Outputs => outputs;

        public float CurrentSpeed { get; set; }
        public IReadOnlyList<Compound> LimitingCompounds => limitingCompounds;

        public void Clear()
        {
            CurrentSpeed = 0;
            inputs.Clear();
            outputs.Clear();
            limitingCompounds.Clear();

            precomputedEnvironmentInputs = null;
        }

        public void AddLimitingFactor(Compound compound)
        {
            limitingCompounds.Add(compound);
        }

        public void AddCapacityProblem(Compound compound)
        {
            // For now this is shown to the user the same way as limit problems
            limitingCompounds.Add(compound);
        }

        public void AddInputAmount(Compound compound, float amount)
        {
            inputs[compound] = amount;
        }

        public void AddOutputAmount(Compound compound, float amount)
        {
            outputs[compound] = amount;
        }
    }
}
