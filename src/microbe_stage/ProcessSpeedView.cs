using System.Collections.Generic;

/// <summary>
///   Read access to process calculations without exposing their mutable builder or input dictionary.
///   The owner must not modify the builder while a view is in use. SimulationCache keeps entries frozen
///   after insertion; ProcessSystem also uses this view for its local, completed calculations.
/// </summary>
internal readonly struct ProcessSpeedView
{
    private readonly ProcessSpeedInformation speed;

    public ProcessSpeedView(ProcessSpeedInformation speed)
    {
        this.speed = speed;
    }

    public float CurrentSpeed => speed.CurrentSpeed;

    public float ATPProduction => speed.ATPProduction;

    public float ATPConsumption => speed.ATPConsumption;

    public InputAmounts Inputs => new(speed.WritableInputs);

    public ProcessSpeedInformation ToMutableCopy()
    {
        var copy = new ProcessSpeedInformation(speed.Process)
        {
            CurrentSpeed = speed.CurrentSpeed,
            Efficiency = speed.Efficiency,
            ATPProduction = speed.ATPProduction,
            ATPConsumption = speed.ATPConsumption,
        };

        foreach (var entry in speed.WritableInputs)
            copy.WritableInputs.Add(entry.Key, entry.Value);

        foreach (var entry in speed.WritableOutputs)
            copy.WritableOutputs.Add(entry.Key, entry.Value);

        foreach (var entry in speed.WritableFullSpeedRequiredEnvironmentalInputs)
            copy.WritableFullSpeedRequiredEnvironmentalInputs.Add(entry.Key, entry.Value);

        foreach (var entry in speed.AvailableAmounts)
            copy.AvailableAmounts.Add(entry.Key, entry.Value);

        foreach (var entry in speed.AvailableRates)
            copy.AvailableRates.Add(entry.Key, entry.Value);

        copy.WritableLimitingCompounds.AddRange(speed.WritableLimitingCompounds);
        return copy;
    }

    /// <summary>
    ///   Enumerates compound amounts by value with a struct enumerator. Does not implement collection
    ///   interfaces, so callers cannot downcast to the backing dictionary or box its enumerator implicitly.
    /// </summary>
    public readonly struct InputAmounts
    {
        private readonly Dictionary<Compound, float> inputs;

        public InputAmounts(Dictionary<Compound, float> inputs)
        {
            this.inputs = inputs;
        }

        public Dictionary<Compound, float>.Enumerator GetEnumerator()
        {
            return inputs.GetEnumerator();
        }
    }
}
