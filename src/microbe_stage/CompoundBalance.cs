using System.Collections.Generic;

/// <summary>
///   Balance of a given compound. Lists the organelles that contribute to the balance
/// </summary>
public class CompoundBalance
{
    public readonly Dictionary<string, float> Consumption = new();

    public readonly Dictionary<string, float> Production = new();

    /// <summary>
    ///   Total balance of this compound
    /// </summary>
    public float Balance;

    public void AddConsumption(string organelleName, float amount)
    {
        if (Consumption.ContainsKey(organelleName))
        {
            Consumption[organelleName] += amount;
        }
        else
        {
            Consumption[organelleName] = amount;
        }

        Balance -= amount;
    }

    public void AddProduction(string organelleName, float amount)
    {
        if (Production.ContainsKey(organelleName))
        {
            Production[organelleName] += amount;
        }
        else
        {
            Production[organelleName] = amount;
        }

        Balance += amount;
    }
}
