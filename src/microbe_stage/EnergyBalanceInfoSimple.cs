/// <summary>
///   Info regarding a microbe's energy balance in a patch, excluding consumption and production tracking
/// </summary>
/// <remarks>
///   <para>
///     This does not take special modes the microbe can be into account, for example, engulfing or binding
///   </para>
///   <para>
///     The consumption and production is not tracked, use <see cref="EnergyBalanceInfoFull"/> to store it
///   </para>
/// </remarks>
public class EnergyBalanceInfoSimple
{
    /// <summary>
    ///   The cost of base movement (only when moving)
    /// </summary>
    public float BaseMovement { get; set; }

    /// <summary>
    ///   The cost of having all flagella working at the same time (only when moving)
    /// </summary>
    public float Flagella { get; set; }

    /// <summary>
    ///   The cost of having all cilia working at the same time at max rotation (only when rotating)
    /// </summary>
    public float Cilia { get; set; }

    /// <summary>
    ///   Sum of <see cref="BaseMovement"/>, <see cref="Flagella"/>, and <see cref="Cilia"/>
    /// </summary>
    public float TotalMovement { get; set; }

    /// <summary>
    ///   The total osmoregulation cost for the microbe
    /// </summary>
    public float Osmoregulation { get; set; }

    /// <summary>
    ///   Total production of energy for all the microbe's processes (assumes there's enough resources to
    ///   run everything)
    /// </summary>
    public float TotalProduction { get; set; }

    /// <summary>
    ///   The total energy consumption of the microbe while it is moving and running all processes
    /// </summary>
    public float TotalConsumption { get; set; }

    /// <summary>
    ///   Total energy consumption while the microbe is stationary (so everything except movement)
    /// </summary>
    public float TotalConsumptionStationary { get; set; }

    /// <summary>
    ///   The absolutely final balance of ATP when a microbe is going all out and running everything and moving
    /// </summary>
    public float FinalBalance { get; set; }

    /// <summary>
    ///   Final balance of ATP when a microbe is stationary (running processes + osmoregulation)
    /// </summary>
    public float FinalBalanceStationary { get; set; }
}
