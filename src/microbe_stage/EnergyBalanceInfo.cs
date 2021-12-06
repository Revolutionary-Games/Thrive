using System.Collections.Generic;

/// <summary>
///   Info regarding a microbe's energy balance in a patch
/// </summary>
public class EnergyBalanceInfo
{
    public Dictionary<string, float> Consumption { get; } = new Dictionary<string, float>();
    public Dictionary<string, float> Production { get; } = new Dictionary<string, float>();

    public float BaseMovement { get; set; }
    public float Flagella { get; set; }

    public float TotalMovement { get; set; }

    public float Osmoregulation { get; set; }

    public float TotalProduction { get; set; }
    public float TotalConsumption { get; set; }
    public float TotalConsumptionStationary { get; set; }

    /// <summary>
    ///   The absolutely final balance of ATP when a cell is going all out and running everything and moving
    /// </summary>
    public float FinalBalance { get; set; }

    /// <summary>
    ///   Final balance of ATP when a cell is stationary (running processes + osmoregulation)
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
