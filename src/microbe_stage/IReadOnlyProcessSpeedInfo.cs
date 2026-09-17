/// <summary>
///   Read-only access to the speed, ATP balance, and complete inputs of a process calculation.
/// </summary>
public interface IReadOnlyProcessSpeedInfo
{
    public float CurrentSpeed { get; }

    public float ATPProduction { get; }

    public float ATPConsumption { get; }

    /// <summary>
    ///   All inputs, including environmental compounds, in calculation order.
    ///   Unlike display inputs, these are not filtered.
    /// </summary>
    public ProcessInputAmounts AllInputs { get; }
}
