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

    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, IComponentSpecificUpgrades customUpgrade)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        shouldRepeat = false;
        this.customUpgrade = customUpgrade;
    }

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

            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            organelleList = newSpecies.Organelles.Organelles;
            count = organelleList.Count;
            for (var i = 0; i < count; ++i)
            {
                var organelle = organelleList[i];

                if (allOrganelles.Contains(organelle.Definition))
                {
                    foreach (var availableUpgrade in organelle.Definition.AvailableUpgrades)
                    {
                        var availableUpgradeName = availableUpgrade.Key;
                        if (availableUpgradeName == upgradeName)
                        {
                            mpcost = availableUpgrade.Value.MPCost;
                        }
                    }

                    // returning here in the loop to make sure that only one organelle gets upgraded
                    organelle.Upgrades ??= new OrganelleUpgrades();
                    if (customUpgrade != null)
                    {
                        organelle.Upgrades.CustomUpgradeData = customUpgrade;
                        mutated.Add(new Tuple<MicrobeSpecies, double>(newSpecies, mp));
                        return mutated;
                    }

                    if (upgradeName != null && !organelle.Upgrades.UnlockedFeatures.Contains(upgradeName) &&
                        mpcost <= mp)
                    {
                        organelle.Upgrades.UnlockedFeatures.Add(upgradeName);
                        mp -= mpcost;
                        mutated.Add(new Tuple<MicrobeSpecies, double>(newSpecies, mp));
                        return mutated;
                    }
                }
            }
        }

        return null;
    }
}
