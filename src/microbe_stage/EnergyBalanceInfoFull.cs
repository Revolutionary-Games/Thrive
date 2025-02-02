using System.Collections.Generic;

/// <summary>
///   Info regarding a microbe's energy balance in a patch, including consumption and production info
/// </summary>
/// <remarks>
///   <para>
///     This does not take special modes the microbe can be into account, for example engulfing or binding
///   </para>
///   <para>
///     This inherits from <see cref="EnergyBalanceInfoSimple"/> whilst also tracking the consumption and production
///   </para>
/// </remarks>
public class EnergyBalanceInfoFull : EnergyBalanceInfoSimple
{
    /// <summary>
    ///   The raw list of all energy consuming things related to the microbe. The key is the action name and the
    ///   value is the total consumption of that action.
    /// </summary>
    public Dictionary<string, float> Consumption { get; } = [];

    /// <summary>
    ///   The same as <see cref="Consumption"/> but for energy production
    /// </summary>
    public Dictionary<string, float> Production { get; } = [];

    /// <summary>
    ///   If setup to collect required compounds to run, then this collects compounds each <see cref="Production"/>
    ///   entry requires to produce the energy.
    /// </summary>
    public Dictionary<string, Dictionary<Compound, float>>? ProductionRequiresCompounds { get; private set; }

    public void AddConsumption(string groupName, float amount)
    {
        Consumption.TryGetValue(groupName, out var existing);

        Consumption[groupName] = existing + amount;
    }

    public void AddProduction(string groupName, float amount, Dictionary<Compound, float> requiredInputCompounds)
    {
        Production.TryGetValue(groupName, out var existing);

        Production[groupName] = existing + amount;

        if (ProductionRequiresCompounds != null)
        {
            if (!ProductionRequiresCompounds.TryGetValue(groupName, out var compoundData))
            {
                compoundData = new Dictionary<Compound, float>();
                ProductionRequiresCompounds[groupName] = compoundData;
            }

            foreach (var inputCompound in requiredInputCompounds)
            {
                compoundData.TryGetValue(inputCompound.Key, out var existingAmount);
                compoundData[inputCompound.Key] = existingAmount + amount;
            }
        }
    }

    public void SetupTrackingForRequiredCompounds()
    {
        if (ProductionRequiresCompounds == null)
        {
            ProductionRequiresCompounds = new Dictionary<string, Dictionary<Compound, float>>();
        }
        else
        {
            // TODO: should this just clear the second level dictionaries for more object reuse?
            ProductionRequiresCompounds.Clear();
        }
    }
}
