using System.Collections.Generic;

/// <summary>
///   Info regarding a microbe's energy balance in a patch
/// </summary>
/// <remarks>
///   <para>
///     This does not take special modes the microbe can be into account, for example engulfing or binding
///   </para>
/// </remarks>
public class EnergyBalanceInfo
{
    /// <summary>
    ///   The raw list of all energy consuming things related to the microbe. The key is the action name and the
    ///   value is the total consumption of that action.
    /// </summary>
    public Dictionary<string, float> Consumption { get; } = new();

    /// <summary>
    ///   The same as <see cref="Consumption"/> but for energy production
    /// </summary>
    public Dictionary<string, float> Production { get; } = new();

    /// <summary>
    ///   The cost of base movement (only when moving)
    /// </summary>
    public float BaseMovement { get; set; }

    /// <summary>
    ///   The cost of having all flagella working at the same time (only when moving)
    /// </summary>
    public float Flagella { get; set; }

    /// <summary>
    ///   Sum of <see cref="BaseMovement"/> and <see cref="Flagella"/>
    /// </summary>
    public float TotalMovement { get; set; }

    /// <summary>
    ///   The total osmoregulation cost for the microbe
    /// </summary>
    public float Osmoregulation { get; set; }

    /// <summary>
    ///   Total production of energy for all of the microbe's processes (assumes there's enough resources to
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

    public void AddConsumption(string groupName, float amount)
    {
        if (!Consumption.ContainsKey(groupName))
        {
            Consumption[groupName] = amount;
        }
        else
        {
            Consumption[groupName] += amount;
        }
    }

    public void AddProduction(string groupName, float amount)
    {
        if (!Production.ContainsKey(groupName))
        {
            Production[groupName] = amount;
        }
        else
        {
            Production[groupName] += amount;
        }
    }
}
