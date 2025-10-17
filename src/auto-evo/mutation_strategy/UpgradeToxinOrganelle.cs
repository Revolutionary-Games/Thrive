using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

/// <summary>
///   Specific mutationstrategy for toxin-launching organelles
///   applies a fixed toxin type based on given upgradename
///   and randomly adjusts toxicity upward, downward, or in both directions
/// </summary>
public class UpgradeToxinOrganelle : IMutationStrategy<MicrobeSpecies>
{
    private readonly Func<OrganelleDefinition, bool> criteria;
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;
    private readonly string direction;
    private readonly string? upgradeName;
    private ToxinType toxinType;

    /// <summary>
    /// Updates a toxin-launching organelle with a given toxin type,
    /// and randomly moves toxicity in a given direction.
    /// </summary>
    ///   <param name="criteria">Organelle requirement to apply</param>
    ///   <param name="upgradeName">The name of the upgrade to apply</param>
    ///   <param name="direction">"increase", "decrease", or "both" to decide what to do with toxicity
    /// </param>
    public UpgradeToxinOrganelle(Func<OrganelleDefinition, bool> criteria, string upgradeName, string direction)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        this.criteria = criteria;
        this.upgradeName = upgradeName;
        this.direction = direction.ToLowerInvariant();
    }

    // We don't want this to repeat, since changing toxicity does not cost MP
    public bool Repeatable => true;

    public List<Tuple<MicrobeSpecies, double>>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (allOrganelles.Count == 0)
        {
            return null;
        }

        // If a cheaper organelle upgrade gets added, this will need to be updated
        if (mp < 10)
            return null;

        double mpcost = 0;

        bool validMutations = false;

        // Manual looping to avoid one enumerator allocation per call
        var organelleList = baseSpecies.Organelles.Organelles;
        var count = organelleList.Count;
        for (var i = 0; i < count; ++i)
        {
            var organelle = organelleList[i];

            if (allOrganelles.Contains(organelle.Definition))
            {
                validMutations = true;
                break;
            }
        }

        if (!validMutations)
            return null;

        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();
        organelleList = newSpecies.Organelles.Organelles;

        for (var i = 0; i < count; ++i)
        {
            var organelle = organelleList[i];
            if (!criteria(organelle.Definition))
                continue;

            organelle.Upgrades ??= new OrganelleUpgrades();

            if (organelle.Upgrades.UnlockedFeatures.Contains(upgradeName!))
                continue;

            foreach (var availableUpgrade in organelle.Definition.AvailableUpgrades)
            {
                var availableUpgradeName = availableUpgrade.Key;
                if (availableUpgradeName == upgradeName)
                {
                    toxinType = ToxinUpgradeNames.ToxinTypeFromName(availableUpgrade.Key);
                    mpcost = availableUpgrade.Value.MPCost;
                    break;
                }
            }

            if (mpcost > mp)
                break;

            // Start applying changes
            if (organelle.Upgrades.CustomUpgradeData is not ToxinUpgrades toxinData)
            {
                toxinData = new ToxinUpgrades(toxinType, 0.0f);
                organelle.Upgrades.CustomUpgradeData = toxinData;
            }
            else
            {
                toxinData.BaseType = toxinType;
            }

            organelle.Upgrades.UnlockedFeatures.Add(upgradeName!);
            mp -= mpcost;

            // Adjust toxicity
            var change = (float)(random.NextDouble() * Constants.AUTO_EVO_MUTATION_TOXICITY_STEP);

            switch (direction)
            {
                case "increase":
                    toxinData.Toxicity = Math.Clamp(toxinData.Toxicity + change, -1.0f, 1.0f);
                    break;

                case "decrease":
                    toxinData.Toxicity = Math.Clamp(toxinData.Toxicity - change, -1.0f, 1.0f);
                    break;

                case "both":
                    change *= random.NextDouble() < 0.5 ? -1.0f : 1.0f;
                    toxinData.Toxicity = Math.Clamp(toxinData.Toxicity + change, -1.0f, 1.0f);
                    break;
            }

            // returning here so that only one organelle gets mutated per run of UpgradeToxinOrganelle
            return [Tuple.Create(newSpecies, mp - mpcost)];
        }

        return null;
    }
}
