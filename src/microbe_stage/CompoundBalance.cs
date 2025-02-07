using System.Collections.Generic;

/// <summary>
///   Balance of a given compound. Lists the organelles that contribute to the balance
/// </summary>
public class CompoundBalance
{
    public Dictionary<string, float>? ConsumptionOrganelles;

    public Dictionary<string, float>? ProductionOrganelles;

    public float Consumption;

    public float Production;

    /// <summary>
    ///   Optionally calculated value for how long it takes for this compound to fill storage (0 when not calculated,
    ///   can be negative if the balance is not positive).
    /// </summary>
    public float FillTime;

    /// <summary>
    ///   Total balance of this compound
    /// </summary>
    public float Balance
    {
        get
        {
            return Production - Consumption;
        }
    }

    public void AddConsumption(string organelleName, float amount)
    {
        if (ConsumptionOrganelles != null)
        {
            ConsumptionOrganelles.TryGetValue(organelleName, out var existing);

            ConsumptionOrganelles[organelleName] = existing + amount;
        }

        Consumption += amount;
    }

    public void AddProduction(string organelleName, float amount)
    {
        if (ProductionOrganelles != null)
        {
            ProductionOrganelles.TryGetValue(organelleName, out var existing);

            ProductionOrganelles[organelleName] = existing + amount;
        }

        Production += amount;
    }
}
