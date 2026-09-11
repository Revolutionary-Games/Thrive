/// <summary>
///   Read-only access to an energy balance, excluding consumption and production tracking.
/// </summary>
public interface IReadOnlyEnergyBalanceInfo
{
    /// <summary>
    ///   The cost of base movement (only when moving)
    /// </summary>
    public float BaseMovement { get; }

    /// <summary>
    ///   The cost of having all flagella working at the same time (only when moving)
    /// </summary>
    public float Flagella { get; }

    /// <summary>
    ///   The cost of all actomyosin working at the same time (only when a colony is moving)
    /// </summary>
    public float Actomyosin { get; }

    /// <summary>
    ///   The cost of having all cilia working at the same time at max rotation (only when rotating)
    /// </summary>
    public float Cilia { get; }

    /// <summary>
    ///   Sum of <see cref="BaseMovement"/>, <see cref="Flagella"/>, <see cref="Actomyosin"/>, and <see cref="Cilia"/>
    /// </summary>
    public float TotalMovement { get; }

    /// <summary>
    ///   The total osmoregulation cost for the microbe
    /// </summary>
    public float Osmoregulation { get; }

    /// <summary>
    ///   Total production of energy for all the microbe's processes (assumes there's enough resources to
    ///   run everything)
    /// </summary>
    public float TotalProduction { get; }

    /// <summary>
    ///   The total energy consumption of the microbe while it is moving and running all processes
    /// </summary>
    public float TotalConsumption { get; }

    /// <summary>
    ///   Total energy consumption while the microbe is stationary (so everything except movement)
    /// </summary>
    public float TotalConsumptionStationary { get; }

    /// <summary>
    ///   The absolutely final balance of ATP when a microbe is going all out and running everything and moving
    /// </summary>
    public float FinalBalance { get; }

    /// <summary>
    ///   Final balance of ATP when a microbe is stationary (running processes + osmoregulation)
    /// </summary>
    public float FinalBalanceStationary { get; }
}
