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

    /// <summary>
    ///   Optionally calculated value for how long it takes for this compound to fill storage (0 when not calculated,
    ///   can be negative if the balance is not positive).
    /// </summary>
    public float FillTime;

    public void AddConsumption(string organelleName, float amount)
    {
        Consumption.TryGetValue(organelleName, out var existing);

        Consumption[organelleName] = existing + amount;

        Balance -= amount;
    }

    public void AddProduction(string organelleName, float amount)
    {
        Production.TryGetValue(organelleName, out var existing);

        Production[organelleName] = existing + amount;

        Balance += amount;
    }
}
