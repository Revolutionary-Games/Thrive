using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

public class UpgradeOrganelle : IMutationStrategy<MicrobeSpecies>
{
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;
    private readonly IComponentSpecificUpgrades? customUpgrade;
    private readonly string? upgradeName;
    private readonly bool shouldRepeat;

    /// <summary>
    ///   Updates an organelle type with the given upgrade custom data
    /// </summary>
    /// <param name="criteria">Organelle requirement to apply</param>
    /// <param name="customUpgrade">The custom data to be applied as an upgrade to the organelle</param>
    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, IComponentSpecificUpgrades customUpgrade)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        shouldRepeat = false;
        this.customUpgrade = customUpgrade;
    }

    /// <summary>
    ///   Updates an organelle type with the given upgrade, creating a new species to test for every additional
    ///   organelle upgraded.
    /// </summary>
    /// <param name="criteria">Organelle requirement to apply</param>
    /// <param name="upgradeName">The name of the upgrade (from organelles.json) to apply</param>
    /// <param name="shouldRepeat">
    ///   Determines whether this mutation strategy can be used multiple times
    /// should be false for any upgrade that does not cost MP</param>
    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, string upgradeName, bool shouldRepeat)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        foreach (var organelle in allOrganelles)
        {
            if (!organelle.AvailableUpgrades.ContainsKey(upgradeName))
            {
                throw new ArgumentException(organelle.Name + " does not have upgrade: " + upgradeName);
            }
        }

        this.shouldRepeat = shouldRepeat;
        this.upgradeName = upgradeName;
    }

    public bool Repeatable => shouldRepeat;

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

        if (validMutations)
        {
            var mutated = new List<Tuple<MicrobeSpecies, double>>();

            var eligibleOrganelles = new List<OrganelleTemplate>();

            foreach (var organelle in baseSpecies.Organelles.Organelles)
            {
                if (allOrganelles.Contains(organelle.Definition))
                    eligibleOrganelles.Add(organelle);
            }

            var eligibleOrganelleSample = eligibleOrganelles
                .OrderBy(_ => random.Next())
                .Take(Constants.AUTO_EVO_ORGANELLE_UPGRADE_ATTEMPTS);

            foreach (var baseSpeciesOrganelle in eligibleOrganelleSample)
            {
                var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

                var organelle = newSpecies.Organelles.Organelles
                    .First(organelle => organelle.Position == baseSpeciesOrganelle.Position);

                foreach (var availableUpgrade in organelle.Definition.AvailableUpgrades)
                {
                    var availableUpgradeName = availableUpgrade.Key;
                    if (availableUpgradeName == upgradeName)
                    {
                        mpcost = availableUpgrade.Value.MPCost;
                        break;
                    }
                }

                if (mpcost > mp)
                    break;

                organelle.Upgrades ??= new OrganelleUpgrades();

                if (customUpgrade != null)
                {
                    organelle.Upgrades.CustomUpgradeData = customUpgrade;
                    mutated.Add(new Tuple<MicrobeSpecies, double>(newSpecies, mp - mpcost));
                }

                if (upgradeName != null &&
                    !organelle.Upgrades.UnlockedFeatures.Contains(upgradeName))
                {
                    organelle.Upgrades.UnlockedFeatures.Add(upgradeName);
                    mutated.Add(new Tuple<MicrobeSpecies, double>(newSpecies, mp - mpcost));
                }
            }

            return mutated;
        }

        return null;
    }
}
